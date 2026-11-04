using SharpShot.Core.Models;

namespace SharpShot.Core.Tests.Models;

public sealed class PixelRectangleTests
{
    [Theory]
    [InlineData(10, 20, 30, 50)]
    [InlineData(30, 50, 10, 20)]
    [InlineData(30, 20, 10, 50)]
    [InlineData(10, 50, 30, 20)]
    public void FromPoints_NormalizesAnyCornerOrder(int x1, int y1, int x2, int y2)
    {
        var rect = PixelRectangle.FromPoints(x1, y1, x2, y2);

        Assert.Equal(new PixelRectangle(10, 20, 20, 30), rect);
    }

    [Fact]
    public void FromPoints_HandlesNegativeVirtualDesktopCoordinates()
    {
        var rect = PixelRectangle.FromPoints(5, 5, -15, -25);

        Assert.Equal(new PixelRectangle(-15, -25, 20, 30), rect);
        Assert.Equal(5, rect.Right);
        Assert.Equal(5, rect.Bottom);
    }

    [Fact]
    public void FromLTRB_ComputesSize()
    {
        Assert.Equal(new PixelRectangle(1, 2, 9, 18), PixelRectangle.FromLTRB(1, 2, 10, 20));
    }

    [Fact]
    public void Intersect_OverlappingRectangles_ReturnsOverlap()
    {
        var a = new PixelRectangle(0, 0, 100, 100);
        var b = new PixelRectangle(50, 60, 100, 100);

        Assert.Equal(new PixelRectangle(50, 60, 50, 40), a.Intersect(b));
        Assert.Equal(a.Intersect(b), b.Intersect(a));
    }

    [Fact]
    public void Intersect_ContainedRectangle_ReturnsInner()
    {
        var outer = new PixelRectangle(-100, -100, 400, 400);
        var inner = new PixelRectangle(10, 10, 5, 5);

        Assert.Equal(inner, outer.Intersect(inner));
    }

    [Theory]
    [InlineData(100, 0, 10, 10)] // touching the (exclusive) right edge
    [InlineData(0, 100, 10, 10)] // touching the (exclusive) bottom edge
    [InlineData(200, 200, 10, 10)] // disjoint
    public void Intersect_NoOverlap_ReturnsEmpty(int x, int y, int w, int h)
    {
        var result = new PixelRectangle(0, 0, 100, 100).Intersect(new PixelRectangle(x, y, w, h));

        Assert.Equal(PixelRectangle.Empty, result);
        Assert.True(result.IsEmpty);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(9, 19, true)]
    [InlineData(10, 0, false)]
    [InlineData(0, 20, false)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    public void ContainsPoint_UsesExclusiveRightAndBottom(int x, int y, bool expected)
    {
        Assert.Equal(expected, new PixelRectangle(0, 0, 10, 20).Contains(x, y));
    }

    [Fact]
    public void ContainsRectangle()
    {
        var outer = new PixelRectangle(0, 0, 100, 100);

        Assert.True(outer.Contains(outer));
        Assert.True(outer.Contains(new PixelRectangle(10, 10, 90, 90)));
        Assert.False(outer.Contains(new PixelRectangle(10, 10, 91, 90)));
        Assert.False(outer.Contains(new PixelRectangle(-1, 0, 10, 10)));
    }

    [Fact]
    public void Offset_MovesWithoutResizing()
    {
        var rect = new PixelRectangle(10, 20, 30, 40);

        Assert.Equal(new PixelRectangle(15, 13, 30, 40), rect.Offset(5, -7));
    }

    [Fact]
    public void RelativeTo_TranslatesIntoOriginSpace()
    {
        var monitor = new PixelRectangle(-1920, 0, 1920, 1080);
        var onMonitor = new PixelRectangle(-1900, 100, 50, 60);

        Assert.Equal(new PixelRectangle(20, 100, 50, 60), onMonitor.RelativeTo(monitor));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(0, 5, true)]
    [InlineData(5, 0, true)]
    [InlineData(-5, 5, true)]
    [InlineData(5, -5, true)]
    [InlineData(1, 1, false)]
    public void IsEmpty_WhenWidthOrHeightNotPositive(int width, int height, bool expected)
    {
        Assert.Equal(expected, new PixelRectangle(3, 3, width, height).IsEmpty);
    }

    [Fact]
    public void Area_IsZeroForNegativeSizes()
    {
        Assert.Equal(0, new PixelRectangle(0, 0, -5, 10).Area);
        Assert.Equal(200L, new PixelRectangle(0, 0, 10, 20).Area);
    }
}
