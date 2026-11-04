namespace SharpShot.Core.Models;

/// <summary>
/// A physical display as reported by Windows. Bounds and WorkArea are in physical pixels on the
/// virtual desktop (origin at the primary monitor's top-left; other monitors may be negative).
/// </summary>
public sealed record MonitorInfo(
    nint Handle,
    string DeviceName,
    PixelRectangle Bounds,
    PixelRectangle WorkArea,
    bool IsPrimary,
    uint Dpi)
{
    /// <summary>Effective scale factor (1.0 = 100 %, 1.5 = 150 %).</summary>
    public double ScaleFactor => Dpi / 96.0;

    public override string ToString()
        => $"{DeviceName} {Bounds} {ScaleFactor:P0}{(IsPrimary ? " (primary)" : string.Empty)}";
}
