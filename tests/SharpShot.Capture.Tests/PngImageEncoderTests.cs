using System.Buffers.Binary;
using Microsoft.Extensions.Logging.Abstractions;
using SharpShot.Capture.Encoding;
using SharpShot.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace SharpShot.Capture.Tests;

public sealed class PngImageEncoderTests
{
    private const int Width = 64;
    private const int Height = 48;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly PngImageEncoder _encoder = new(NullLogger<PngImageEncoder>.Instance);

    /// <summary>
    /// A 64x48 gradient with deliberately non-opaque, varying alpha (which the encoder must ignore)
    /// plus a few exact, hand-picked pixels.
    /// </summary>
    private static PixelBuffer CreateTestImage()
    {
        var buffer = PixelBuffer.Create(Width, Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                buffer.SetPixel(
                    x,
                    y,
                    b: (byte)(x * 4),
                    g: (byte)(y * 5),
                    r: (byte)((x * 7) + (y * 3)),
                    a: (byte)((x * 31) + (y * 17)));
            }
        }

        buffer.SetPixel(0, 0, 0x00, 0x00, 0x00, 0x00);
        buffer.SetPixel(Width - 1, Height - 1, 0xFF, 0xFF, 0xFF, 0x01);
        buffer.SetPixel(10, 20, 0x01, 0x02, 0x03, 0x80);
        buffer.SetPixel(33, 7, 0xFE, 0x01, 0x80, 0xFF);
        buffer.SetPixel(Width - 1, 0, 0x12, 0x34, 0x56, 0x00);
        buffer.SetPixel(0, Height - 1, 0xAB, 0xCD, 0xEF, 0x7F);
        return buffer;
    }

    private static async Task<(BitmapDecoder Decoder, byte[] Pixels)> DecodeAsync(byte[] png)
    {
        var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(png);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var provider = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            new BitmapTransform(),
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);
        return (decoder, provider.DetachPixelData());
    }

    [Fact]
    public void Metadata()
    {
        Assert.Equal(".png", _encoder.FileExtension);
        Assert.Equal("PNG", _encoder.FormatName);
    }

    [Fact]
    public void Constructor_RejectsNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() => new PngImageEncoder(null!));
    }

    [Fact]
    public async Task EncodeAsync_RejectsNullBuffer()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _encoder.EncodeAsync(null!));
    }

    [Fact]
    public async Task EncodeAsync_WritesPngSignatureAndHeader()
    {
        var png = await _encoder.EncodeAsync(CreateTestImage());

        Assert.Equal(PngSignature, png[..8]);
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(png, 12, 4));
        Assert.Equal(Width, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal(Height, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
        Assert.Equal(8, png[24]); // bit depth
        Assert.Equal(2, png[25]); // colour type 2 = truecolour RGB, no alpha channel
    }

    [Fact]
    public async Task EncodeAsync_IsLossless_ByteExactRgbAfterDecode()
    {
        var source = CreateTestImage();
        var expected = (byte[])source.Data.Clone();

        var png = await _encoder.EncodeAsync(source);
        var (decoder, decoded) = await DecodeAsync(png);

        Assert.Equal(BitmapDecoder.PngDecoderId, decoder.DecoderInformation.CodecId);
        Assert.Equal((uint)Width, decoder.PixelWidth);
        Assert.Equal((uint)Height, decoder.PixelHeight);
        Assert.Equal(expected.Length, decoded.Length);

        var mismatches = 0;
        for (var i = 0; i < expected.Length; i += 4)
        {
            if (decoded[i] != expected[i] || decoded[i + 1] != expected[i + 1] || decoded[i + 2] != expected[i + 2])
            {
                mismatches++;
            }
        }

        Assert.Equal(0, mismatches);
        Assert.Equal(expected, source.Data); // the encoder must not touch the caller's buffer
    }

    [Theory]
    [InlineData(0, 0, 0x00, 0x00, 0x00)]
    [InlineData(Width - 1, Height - 1, 0xFF, 0xFF, 0xFF)]
    [InlineData(10, 20, 0x01, 0x02, 0x03)]
    [InlineData(33, 7, 0xFE, 0x01, 0x80)]
    [InlineData(Width - 1, 0, 0x12, 0x34, 0x56)]
    [InlineData(0, Height - 1, 0xAB, 0xCD, 0xEF)]
    public async Task EncodeAsync_PreservesKnownPixels(int x, int y, byte b, byte g, byte r)
    {
        var png = await _encoder.EncodeAsync(CreateTestImage());
        var (_, decoded) = await DecodeAsync(png);

        var offset = ((y * Width) + x) * 4;
        Assert.Equal(b, decoded[offset]);
        Assert.Equal(g, decoded[offset + 1]);
        Assert.Equal(r, decoded[offset + 2]);
    }

    [Fact]
    public async Task EncodeAsync_DropsAlpha_DecodesOpaque()
    {
        var png = await _encoder.EncodeAsync(CreateTestImage());
        var (_, decoded) = await DecodeAsync(png);

        for (var i = 3; i < decoded.Length; i += 4)
        {
            Assert.Equal(0xFF, decoded[i]);
        }
    }

    [Fact]
    public async Task EncodeAsync_SinglePixel()
    {
        var buffer = PixelBuffer.Create(1, 1);
        buffer.SetPixel(0, 0, 0x10, 0x20, 0x30);

        var (decoder, decoded) = await DecodeAsync(await _encoder.EncodeAsync(buffer));

        Assert.Equal(1u, decoder.PixelWidth);
        Assert.Equal(1u, decoder.PixelHeight);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 0xFF }, decoded);
    }

    [Fact]
    public async Task EncodeAsync_IsDeterministic()
    {
        var first = await _encoder.EncodeAsync(CreateTestImage());
        var second = await _encoder.EncodeAsync(CreateTestImage());

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task EncodeAsync_HonoursCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _encoder.EncodeAsync(CreateTestImage(), cts.Token));
    }
}
