namespace SharpShot.Core.Models;

/// <summary>
/// A tightly packed 32-bit BGRA8 bitmap (stride = Width * 4), the interchange format between
/// the capture engine, the region cropper and the PNG encoder. Pixels are never resampled.
/// </summary>
public sealed class PixelBuffer
{
    public const int BytesPerPixel = 4;

    public PixelBuffer(int width, int height, byte[] data)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(data);

        var expected = checked((long)width * height * BytesPerPixel);
        if (data.LongLength != expected)
        {
            throw new ArgumentException(
                $"Pixel data length {data.LongLength} does not match {width}x{height} BGRA8 ({expected} bytes).",
                nameof(data));
        }

        Width = width;
        Height = height;
        Data = data;
    }

    public int Width { get; }
    public int Height { get; }
    public int Stride => Width * BytesPerPixel;
    public byte[] Data { get; }
    public long SizeInBytes => Data.LongLength;

    public static PixelBuffer Create(int width, int height)
        => new(width, height, new byte[checked(width * height * BytesPerPixel)]);

    /// <summary>
    /// Returns an exact copy of <paramref name="region"/> (coordinates relative to this buffer).
    /// Throws if the region is empty or not fully inside the buffer: callers must clamp first.
    /// </summary>
    public PixelBuffer Crop(PixelRectangle region)
    {
        if (region.IsEmpty)
        {
            throw new ArgumentException("Crop region must have a positive size.", nameof(region));
        }

        var bounds = new PixelRectangle(0, 0, Width, Height);
        if (!bounds.Contains(region))
        {
            throw new ArgumentOutOfRangeException(nameof(region), $"Region {region} is outside the {Width}x{Height} buffer.");
        }

        if (region.X == 0 && region.Y == 0 && region.Width == Width && region.Height == Height)
        {
            return new PixelBuffer(Width, Height, (byte[])Data.Clone());
        }

        var result = Create(region.Width, region.Height);
        var rowBytes = region.Width * BytesPerPixel;
        for (var row = 0; row < region.Height; row++)
        {
            var sourceOffset = ((region.Y + row) * Width + region.X) * BytesPerPixel;
            var targetOffset = row * rowBytes;
            Buffer.BlockCopy(Data, sourceOffset, result.Data, targetOffset, rowBytes);
        }

        return result;
    }

    /// <summary>Sets every alpha byte to 255. Desktop captures are opaque by definition.</summary>
    public void ForceOpaque()
    {
        for (var i = 3; i < Data.Length; i += BytesPerPixel)
        {
            Data[i] = 0xFF;
        }
    }

    public bool IsOpaque()
    {
        for (var i = 3; i < Data.Length; i += BytesPerPixel)
        {
            if (Data[i] != 0xFF)
            {
                return false;
            }
        }

        return true;
    }

    public (byte B, byte G, byte R, byte A) GetPixel(int x, int y)
    {
        var offset = Offset(x, y);
        return (Data[offset], Data[offset + 1], Data[offset + 2], Data[offset + 3]);
    }

    public void SetPixel(int x, int y, byte b, byte g, byte r, byte a = 0xFF)
    {
        var offset = Offset(x, y);
        Data[offset] = b;
        Data[offset + 1] = g;
        Data[offset + 2] = r;
        Data[offset + 3] = a;
    }

    private int Offset(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException($"({x}, {y}) is outside the {Width}x{Height} buffer.");
        }

        return (y * Width + x) * BytesPerPixel;
    }
}
