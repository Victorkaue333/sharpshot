using SharpShot.Core.Enums;

namespace SharpShot.Core.Models;

/// <summary>Outcome of a completed capture that has been written to disk.</summary>
public sealed record CaptureResult(
    string FilePath,
    int Width,
    int Height,
    CaptureMode Mode,
    DateTimeOffset Timestamp,
    long FileSizeBytes)
{
    public string FileName => Path.GetFileName(FilePath);
    public string Directory => Path.GetDirectoryName(FilePath) ?? string.Empty;
}

/// <summary>A raw frame grabbed from one monitor, before cropping or encoding.</summary>
public sealed record CapturedFrame(PixelBuffer Pixels, MonitorInfo Monitor, DateTimeOffset Timestamp);

/// <summary>A file written by the storage layer.</summary>
public sealed record SavedScreenshot(string FilePath, long SizeBytes);

/// <summary>Result of validating a user supplied directory path.</summary>
public sealed record PathValidationResult(bool IsValid, string? Error)
{
    public static PathValidationResult Valid { get; } = new(true, null);
    public static PathValidationResult Invalid(string error) => new(false, error);
}
