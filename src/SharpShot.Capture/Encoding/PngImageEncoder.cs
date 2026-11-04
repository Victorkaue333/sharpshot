using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpShot.Core.Enums;
using SharpShot.Core.Exceptions;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace SharpShot.Capture.Encoding;

/// <summary>
/// Lossless PNG encoding through the Windows Imaging Component (Windows.Graphics.Imaging.BitmapEncoder).
/// Pixels are handed to the encoder exactly as captured (BGRA8, alpha ignored); no scaling, filtering or
/// colour conversion happens here. The PNG is written as 8-bit RGB (alpha dropped: screenshots are opaque).
/// </summary>
public sealed class PngImageEncoder : IImageEncoder
{
    /// <summary>pHYs metadata only; it never changes pixels. 96 DPI keeps 1:1 viewing in most viewers.</summary>
    public const double MetadataDpi = 96.0;

    private readonly ILogger<PngImageEncoder> _logger;

    public PngImageEncoder(ILogger<PngImageEncoder> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public string FileExtension => ".png";

    public string FormatName => "PNG";

    public async Task<byte[]> EncodeAsync(PixelBuffer pixels, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream).AsTask(cancellationToken);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                (uint)pixels.Width,
                (uint)pixels.Height,
                MetadataDpi,
                MetadataDpi,
                pixels.Data);
            await encoder.FlushAsync().AsTask(cancellationToken);

            var bytes = new byte[stream.Size];
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size).AsTask(cancellationToken);
            reader.ReadBytes(bytes);

            _logger.LogDebug("Encoded {Width}x{Height} px to PNG ({Bytes} bytes).", pixels.Width, pixels.Height, bytes.Length);
            return bytes;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or ArgumentException or OutOfMemoryException)
        {
            throw new ScreenshotSaveException(SaveErrorKind.EncodingFailed, $"PNG encoding failed: {ex.Message}", ex);
        }
    }
}
