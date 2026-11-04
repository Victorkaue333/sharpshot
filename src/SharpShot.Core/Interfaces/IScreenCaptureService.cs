using SharpShot.Core.Models;

namespace SharpShot.Core.Interfaces;

/// <summary>Grabs the current content of a monitor at native resolution.</summary>
public interface IScreenCaptureService
{
    /// <summary>False when the OS does not support Windows Graphics Capture (or the session cannot use it).</summary>
    bool IsSupported { get; }

    /// <summary>Captures one frame of <paramref name="monitor"/> as an opaque BGRA8 buffer.</summary>
    Task<CapturedFrame> CaptureMonitorAsync(MonitorInfo monitor, CancellationToken cancellationToken = default);
}
