using SharpShot.Core.Models;

namespace SharpShot.Core.Tests.Models;

public sealed class PixelBufferTests
{
    /// <summary>A 5x4 buffer whose every byte is distinct (byte i = i).</summary>
    private static PixelBuffer CreateDistinct5x4()
    {
        var data = new byte[5 * 4 * PixelBuffer.BytesPerPixel];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)i;
        }

        return new PixelBuffer(5, 4, data);
    }

    [Fact]
    public void Constructor_SetsDimensionsAndStride()
    {
        var buffer = new PixelBuffer(3, 2, new byte[24]);

        Assert.Equal(3, buffer.Width);
        Assert.Equal(2, buffer.Height);
        Assert.Equal(12, buffer.Stride);
        Assert.Equal(24, buffer.SizeInBytes);
    }

    [Theory]
    [InlineData(23)]
    [InlineData(25)]
    [InlineData(0)]
    public void Constructor_RejectsLengthMismatch(int length)
    {
        Assert.Throws<ArgumentException>(() => new PixelBuffer(3, 2, new byte[length]));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void Constructor_RejectsNonPositiveSize(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(width, height, []));
    }

    [Fact]
    public void Constructor_RejectsNullData()
    {
        Assert.Throws<ArgumentNullException>(() => new PixelBuffer(1, 1, null!));
    }

    [Fact]
    public void Create_AllocatesZeroedBuffer()
    {
        var buffer = PixelBuffer.Create(4, 3);

        Assert.Equal(48, buffer.Data.Length);
        Assert.All(buffer.Data, b => Assert.Equal(0, b));
    }

    [Fact]
    public void Crop_CopiesExactBytes()
    {
        var source = CreateDistinct5x4();

        var cropped = source.Crop(new PixelRectangle(1, 2, 2, 2));

        Assert.Equal(2, cropped.Width);
        Assert.Equal(2, cropped.Height);
        // Row 2 starts at pixel (1,2) = byte (2*5+1)*4 = 44; row 3 at (3*5+1)*4 = 64.
        byte[] expected =
        [
            44, 45, 46, 47, 48, 49, 50, 51,
            64, 65, 66, 67, 68, 69, 70, 71,
        ];
        Assert.Equal(expected, cropped.Data);
    }

    [Fact]
    public void Crop_BottomRightPixel_CopiesExactBytes()
    {
        var source = CreateDistinct5x4();

        var cropped = source.Crop(new PixelRectangle(4, 3, 1, 1));

        Assert.Equal(new byte[] { 76, 77, 78, 79 }, cropped.Data);
    }

    [Fact]
    public void Crop_DoesNotAliasSource()
    {
        var source = CreateDistinct5x4();
        var cropped = source.Crop(new PixelRectangle(0, 0, 2, 2));

        cropped.Data[0] = 0xEE;

        Assert.Equal(0, source.Data[0]);
    }

    [Fact]
    public void Crop_FullSize_ReturnsEqualCopyNotSameArray()
    {
        var source = CreateDistinct5x4();

        var cropped = source.Crop(new PixelRectangle(0, 0, 5, 4));

        Assert.NotSame(source, cropped);
        Assert.NotSame(source.Data, cropped.Data);
        Assert.Equal(source.Data, cropped.Data);
    }

    [Theory]
    [InlineData(4, 0, 2, 1)] // past the right edge
    [InlineData(0, 3, 1, 2)] // past the bottom edge
    [InlineData(-1, 0, 1, 1)]
    [InlineData(0, -1, 1, 1)]
    [InlineData(0, 0, 6, 4)] // larger than the buffer
    public void Crop_OutsideBounds_Throws(int x, int y, int w, int h)
    {
        var source = CreateDistinct5x4();

        Assert.Throws<ArgumentOutOfRangeException>(() => source.Crop(new PixelRectangle(x, y, w, h)));
    }

    [Theory]
    [InlineData(0, 0, 0, 1)]
    [InlineData(0, 0, 1, 0)]
    [InlineData(1, 1, -1, 2)]
    public void Crop_Empty_Throws(int x, int y, int w, int h)
    {
        var source = CreateDistinct5x4();

        Assert.Throws<ArgumentException>(() => source.Crop(new PixelRectangle(x, y, w, h)));
    }

    [Fact]
    public void ForceOpaque_SetsOnlyAlphaBytes()
    {
        var buffer = CreateDistinct5x4();
        Assert.False(buffer.IsOpaque());

        buffer.ForceOpaque();

        Assert.True(buffer.IsOpaque());
        for (var i = 0; i < buffer.Data.Length; i++)
        {
            Assert.Equal(i % 4 == 3 ? (byte)0xFF : (byte)i, buffer.Data[i]);
        }
    }

    [Fact]
    public void IsOpaque_FalseWhenSinglePixelTranslucent()
    {
        var buffer = PixelBuffer.Create(3, 3);
        buffer.ForceOpaque();
        buffer.SetPixel(2, 2, 1, 2, 3, 0xFE);

        Assert.False(buffer.IsOpaque());
    }

    [Fact]
    public void SetPixel_GetPixel_RoundTripInBgraOrder()
    {
        var buffer = PixelBuffer.Create(5, 4);

        buffer.SetPixel(3, 2, b: 10, g: 20, r: 30, a: 40);
        buffer.SetPixel(0, 0, b: 1, g: 2, r: 3);

        Assert.Equal(((byte)10, (byte)20, (byte)30, (byte)40), buffer.GetPixel(3, 2));
        Assert.Equal(((byte)1, (byte)2, (byte)3, (byte)0xFF), buffer.GetPixel(0, 0));
        var offset = ((2 * 5) + 3) * 4;
        Assert.Equal(new byte[] { 10, 20, 30, 40 }, buffer.Data[offset..(offset + 4)]);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(0, 4)]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void GetPixel_SetPixel_OutsideBounds_Throw(int x, int y)
    {
        var buffer = PixelBuffer.Create(5, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(x, y));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(x, y, 0, 0, 0));
    }
}
