using SharpShot.Core.Models;

namespace SharpShot.Core.Interfaces;

/// <summary>Encodes a pixel buffer into an image file format without changing its pixels.</summary>
public interface IImageEncoder
{
    /// <summary>File extension including the dot, e.g. ".png".</summary>
    string FileExtension { get; }

    /// <summary>Human readable format name, e.g. "PNG".</summary>
    string FormatName { get; }

    Task<byte[]> EncodeAsync(PixelBuffer pixels, CancellationToken cancellationToken = default);
}
