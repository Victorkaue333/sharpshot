using SharpShot.Capture.Interop;

namespace SharpShot.Capture.Services;

/// <summary>
/// Synchronises with the Desktop Window Manager so that a window hidden just before a capture is
/// guaranteed to be gone from the composed desktop. <see cref="NativeMethods.DwmFlush"/> blocks until
/// the next composition frame is presented; two flushes cover the hide → compose → present pipeline
/// without relying on arbitrary sleeps.
/// </summary>
public static class CompositionSync
{
    public const int DefaultFrames = 2;

    /// <summary>Waits for <paramref name="frames"/> DWM composition frames (no-op when DWM is unavailable).</summary>
    public static void WaitForComposition(int frames = DefaultFrames)
    {
        for (var i = 0; i < Math.Max(frames, 1); i++)
        {
            if (NativeMethods.DwmFlush() < 0)
            {
                // DWM not running (legacy session); fall back to a single vertical-blank-sized pause.
                Thread.Sleep(16);
            }
        }
    }

    public static Task WaitForCompositionAsync(int frames = DefaultFrames, CancellationToken cancellationToken = default)
        => Task.Run(() => WaitForComposition(frames), cancellationToken);
}
