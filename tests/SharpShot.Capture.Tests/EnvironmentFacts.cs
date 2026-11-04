using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace SharpShot.Capture.Tests;

/// <summary>A fact that is reported as skipped when the process has no interactive desktop.</summary>
public sealed class DisplayFactAttribute : FactAttribute
{
    public DisplayFactAttribute()
    {
        if (!Environment.UserInteractive)
        {
            Skip = "Requires an interactive desktop session (Environment.UserInteractive is false).";
        }
    }
}

/// <summary>
/// A fact that is reported as skipped unless Windows Graphics Capture is usable: an interactive desktop
/// and <see cref="GraphicsCaptureSession.IsSupported"/> returning true.
/// </summary>
public sealed class CaptureFactAttribute : FactAttribute
{
    public CaptureFactAttribute()
    {
        if (!Environment.UserInteractive)
        {
            Skip = "Requires an interactive desktop session (Environment.UserInteractive is false).";
            return;
        }

        bool supported;
        try
        {
            supported = GraphicsCaptureSession.IsSupported();
        }
        catch (Exception ex) when (ex is COMException or TypeLoadException or InvalidOperationException)
        {
            supported = false;
        }

        if (!supported)
        {
            Skip = "Windows Graphics Capture is not supported in this session.";
        }
    }
}
