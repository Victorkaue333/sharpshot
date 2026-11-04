using SharpShot.Core.Enums;
using SharpShot.Core.Models;
using SharpShot.Core.Services;

namespace SharpShot.Core.Tests.Services;

public sealed class RegionMathTests
{
    private static readonly PixelRectangle Bounds = new(0, 0, 100, 100);

    // L=10 T=10 R=30 B=30
    private static readonly PixelRectangle Rect = new(10, 10, 20, 20);

    [Theory]
    [InlineData(0.5, 1.0, 1)] // banker's rounding would give 0
    [InlineData(2.5, 1.0, 3)] // banker's rounding would give 2
    [InlineData(-2.5, 1.0, -3)]
    [InlineData(1.25, 2.0, 3)]
    [InlineData(100, 1.25, 125)]
    [InlineData(100, 1.5, 150)]
    [InlineData(10.2, 1.0, 10)]
    [InlineData(0, 1.75, 0)]
    public void DipToPixel_RoundsAwayFromZero(double dip, double scale, int expected)
    {
        Assert.Equal(expected, RegionMath.DipToPixel(dip, scale));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void DipToPixel_RejectsNonPositiveScale(double scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RegionMath.DipToPixel(10, scale));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegionMath.PixelToDip(10, scale));
    }

    [Fact]
    public void PixelToDip_DividesByScale()
    {
        Assert.Equal(100.0, RegionMath.PixelToDip(150, 1.5), 10);
    }

    [Theory]
    [InlineData(50, 50, 50, 50)]
    [InlineData(-5, 60, 0, 50)]
    [InlineData(150, -1, 100, 0)]
    [InlineData(100, 50, 100, 50)] // far edge is inclusive
    public void ClampPoint_KeepsPointInsideBounds(int x, int y, int expectedX, int expectedY)
    {
        var bounds = new PixelRectangle(0, 0, 100, 50);

        Assert.Equal((expectedX, expectedY), RegionMath.ClampPoint(x, y, bounds));
    }

    [Fact]
    public void ClampPoint_WorksWithNegativeBounds()
    {
        var bounds = new PixelRectangle(-1920, -200, 1920, 1080);

        Assert.Equal((-1920, 880), RegionMath.ClampPoint(-5000, 5000, bounds));
    }

    [Fact]
    public void FromDrag_ForwardDrag()
    {
        Assert.Equal(new PixelRectangle(20, 10, 60, 30), RegionMath.FromDrag(20, 10, 80, 40, Bounds));
    }

    [Fact]
    public void FromDrag_ReversedDrag_Normalizes()
    {
        Assert.Equal(new PixelRectangle(20, 10, 60, 30), RegionMath.FromDrag(80, 40, 20, 10, Bounds));
    }

    [Fact]
    public void FromDrag_ClampsToBounds()
    {
        var bounds = new PixelRectangle(0, 0, 100, 50);

        Assert.Equal(bounds, RegionMath.FromDrag(-10, -10, 150, 80, bounds));
        Assert.Equal(new PixelRectangle(0, 30, 40, 20), RegionMath.FromDrag(40, 30, -999, 999, bounds));
    }

    [Fact]
    public void Clamp_IntersectsWithBounds()
    {
        Assert.Equal(new PixelRectangle(90, 90, 10, 10), RegionMath.Clamp(new PixelRectangle(90, 90, 50, 50), Bounds));
    }

    [Theory]
    [InlineData(5, 5, 15, 15)]
    [InlineData(200, 200, 80, 80)] // clamped to the right/bottom edges
    [InlineData(-50, -50, 0, 0)] // clamped to the left/top edges
    [InlineData(200, -50, 80, 0)]
    public void Move_ClampsInsideBounds(int dx, int dy, int expectedX, int expectedY)
    {
        var moved = RegionMath.Move(Rect, dx, dy, Bounds);

        Assert.Equal(new PixelRectangle(expectedX, expectedY, 20, 20), moved);
        Assert.True(Bounds.Contains(moved));
    }

    [Fact]
    public void Move_RectLargerThanBounds_PinsToTopLeft()
    {
        var moved = RegionMath.Move(new PixelRectangle(0, 0, 150, 150), 30, 30, Bounds);

        Assert.Equal(new PixelRectangle(0, 0, 150, 150), moved);
    }

    [Theory]
    [InlineData(100, 100, ResizeHandle.TopLeft)]
    [InlineData(125, 100, ResizeHandle.Top)]
    [InlineData(150, 100, ResizeHandle.TopRight)]
    [InlineData(150, 120, ResizeHandle.Right)]
    [InlineData(150, 140, ResizeHandle.BottomRight)]
    [InlineData(125, 140, ResizeHandle.Bottom)]
    [InlineData(100, 140, ResizeHandle.BottomLeft)]
    [InlineData(100, 120, ResizeHandle.Left)]
    [InlineData(125, 120, ResizeHandle.Move)]
    [InlineData(10, 10, ResizeHandle.None)]
    [InlineData(300, 120, ResizeHandle.None)]
    public void HitTest_ExactPositions(int x, int y, ResizeHandle expected)
    {
        var rect = new PixelRectangle(100, 100, 50, 40);

        Assert.Equal(expected, RegionMath.HitTest(rect, x, y, tolerance: 5));
    }

    [Theory]
    [InlineData(95, 95, ResizeHandle.TopLeft)] // at the edge of tolerance (outside)
    [InlineData(105, 105, ResizeHandle.TopLeft)] // at the edge of tolerance (inside)
    [InlineData(155, 96, ResizeHandle.TopRight)]
    [InlineData(146, 144, ResizeHandle.BottomRight)]
    [InlineData(97, 143, ResizeHandle.BottomLeft)]
    [InlineData(125, 96, ResizeHandle.Top)]
    [InlineData(125, 145, ResizeHandle.Bottom)]
    [InlineData(95, 120, ResizeHandle.Left)]
    [InlineData(154, 120, ResizeHandle.Right)]
    [InlineData(106, 106, ResizeHandle.Move)] // just past tolerance, inside the body
    [InlineData(94, 120, ResizeHandle.None)] // just past tolerance, outside
    [InlineData(125, 94, ResizeHandle.None)]
    [InlineData(100, 94, ResizeHandle.None)] // near left edge but above the tolerance band
    public void HitTest_RespectsTolerance(int x, int y, ResizeHandle expected)
    {
        var rect = new PixelRectangle(100, 100, 50, 40);

        Assert.Equal(expected, RegionMath.HitTest(rect, x, y, tolerance: 5));
    }

    [Fact]
    public void HitTest_EmptyRectangle_ReturnsNone()
    {
        Assert.Equal(ResizeHandle.None, RegionMath.HitTest(new PixelRectangle(10, 10, 0, 0), 10, 10, 5));
    }

    [Theory]
    [InlineData(ResizeHandle.TopLeft, 5, 6, 5, 6, 25, 24)]
    [InlineData(ResizeHandle.Top, 99, 5, 10, 5, 20, 25)]
    [InlineData(ResizeHandle.TopRight, 40, 0, 10, 0, 30, 30)]
    [InlineData(ResizeHandle.Right, 50, 99, 10, 10, 40, 20)]
    [InlineData(ResizeHandle.BottomRight, 40, 45, 10, 10, 30, 35)]
    [InlineData(ResizeHandle.Bottom, 0, 50, 10, 10, 20, 40)]
    [InlineData(ResizeHandle.BottomLeft, 0, 50, 0, 10, 30, 40)]
    [InlineData(ResizeHandle.Left, 2, 77, 2, 10, 28, 20)]
    public void Resize_ByEachHandle_AnchorsOppositeEdges(ResizeHandle handle, int x, int y, int ex, int ey, int ew, int eh)
    {
        Assert.Equal(new PixelRectangle(ex, ey, ew, eh), RegionMath.Resize(Rect, handle, x, y, Bounds));
    }

    [Theory]
    [InlineData(ResizeHandle.Move)]
    [InlineData(ResizeHandle.None)]
    public void Resize_NonResizeHandle_ReturnsUnchanged(ResizeHandle handle)
    {
        Assert.Equal(Rect, RegionMath.Resize(Rect, handle, 70, 70, Bounds));
    }

    [Theory]
    [InlineData(ResizeHandle.Left, 50, 20, 30, 10, 20, 20)]
    [InlineData(ResizeHandle.Right, 0, 20, 0, 10, 10, 20)]
    [InlineData(ResizeHandle.Top, 20, 60, 10, 30, 20, 30)]
    [InlineData(ResizeHandle.Bottom, 20, 4, 10, 4, 20, 6)]
    [InlineData(ResizeHandle.TopLeft, 60, 70, 30, 30, 30, 40)]
    [InlineData(ResizeHandle.BottomRight, 0, 0, 0, 0, 10, 10)]
    public void Resize_CrossingOver_FlipsWithoutNegativeSize(ResizeHandle handle, int x, int y, int ex, int ey, int ew, int eh)
    {
        var result = RegionMath.Resize(Rect, handle, x, y, Bounds);

        Assert.Equal(new PixelRectangle(ex, ey, ew, eh), result);
        Assert.True(result.Width >= 0);
        Assert.True(result.Height >= 0);
    }

    [Fact]
    public void Resize_ClampsPointerToBounds()
    {
        Assert.Equal(new PixelRectangle(10, 10, 90, 20), RegionMath.Resize(Rect, ResizeHandle.Right, 500, 0, Bounds));
        Assert.Equal(new PixelRectangle(0, 0, 30, 30), RegionMath.Resize(Rect, ResizeHandle.TopLeft, -500, -500, Bounds));
    }

    [Theory]
    [InlineData(ResizeHandle.TopLeft, true, false, ResizeHandle.TopRight)]
    [InlineData(ResizeHandle.TopLeft, false, true, ResizeHandle.BottomLeft)]
    [InlineData(ResizeHandle.TopLeft, true, true, ResizeHandle.BottomRight)]
    [InlineData(ResizeHandle.TopRight, true, false, ResizeHandle.TopLeft)]
    [InlineData(ResizeHandle.TopRight, false, true, ResizeHandle.BottomRight)]
    [InlineData(ResizeHandle.BottomLeft, true, false, ResizeHandle.BottomRight)]
    [InlineData(ResizeHandle.BottomLeft, false, true, ResizeHandle.TopLeft)]
    [InlineData(ResizeHandle.BottomRight, true, true, ResizeHandle.TopLeft)]
    [InlineData(ResizeHandle.Left, true, false, ResizeHandle.Right)]
    [InlineData(ResizeHandle.Right, true, false, ResizeHandle.Left)]
    [InlineData(ResizeHandle.Left, false, true, ResizeHandle.Left)]
    [InlineData(ResizeHandle.Top, false, true, ResizeHandle.Bottom)]
    [InlineData(ResizeHandle.Bottom, false, true, ResizeHandle.Top)]
    [InlineData(ResizeHandle.Top, true, false, ResizeHandle.Top)]
    [InlineData(ResizeHandle.Right, false, false, ResizeHandle.Right)]
    [InlineData(ResizeHandle.Move, true, true, ResizeHandle.Move)]
    [InlineData(ResizeHandle.None, true, true, ResizeHandle.None)]
    public void Flip_ReturnsOppositeHandle(ResizeHandle handle, bool horizontally, bool vertically, ResizeHandle expected)
    {
        Assert.Equal(expected, RegionMath.Flip(handle, horizontally, vertically));
    }
}
