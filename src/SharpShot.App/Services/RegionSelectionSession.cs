using Microsoft.Extensions.Logging;
using SharpShot.App.Views;
using SharpShot.Core.Models;

namespace SharpShot.App.Services;

/// <summary>The user's choice: a rectangle (monitor-local physical pixels) inside one frozen monitor frame.</summary>
public sealed record RegionSelection(CapturedFrame Frame, PixelRectangle Region);

/// <summary>
/// Shows one <see cref="RegionOverlayWindow"/> per monitor over the frozen frames and resolves when the user
/// confirms or cancels. A selection lives on a single monitor at a time; starting a drag on another monitor
/// clears the previous one.
/// </summary>
public sealed class RegionSelectionSession
{
    public delegate RegionSelectionSession Factory(IReadOnlyList<CapturedFrame> frames);

    private readonly IReadOnlyList<CapturedFrame> _frames;
    private readonly ILogger<RegionSelectionSession> _logger;
    private readonly List<RegionOverlayWindow> _overlays = new();
    private readonly TaskCompletionSource<RegionSelection?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _finished;

    public RegionSelectionSession(IReadOnlyList<CapturedFrame> frames, ILogger<RegionSelectionSession> logger)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(logger);
        if (frames.Count == 0)
        {
            throw new ArgumentException("At least one frame is required.", nameof(frames));
        }

        _frames = frames;
        _logger = logger;
    }

    public async Task<RegionSelection?> RunAsync()
    {
        foreach (var frame in _frames)
        {
            var overlay = new RegionOverlayWindow(frame, this);
            _overlays.Add(overlay);
        }

        foreach (var overlay in _overlays)
        {
            overlay.ShowOverlay();
        }

        // Keyboard focus goes to the overlay under the cursor (or the primary one).
        var focused = _overlays.FirstOrDefault(o => o.ContainsCursor()) ?? _overlays.FirstOrDefault(o => o.Frame.Monitor.IsPrimary) ?? _overlays[0];
        focused.Activate();

        try
        {
            return await _completion.Task;
        }
        finally
        {
            foreach (var overlay in _overlays)
            {
                overlay.CloseOverlay();
            }

            _overlays.Clear();
        }
    }

    /// <summary>Called by an overlay when a drag begins there; other overlays drop their selection.</summary>
    internal void SelectionStarted(RegionOverlayWindow source)
    {
        foreach (var overlay in _overlays)
        {
            if (!ReferenceEquals(overlay, source))
            {
                overlay.ClearSelection();
            }
        }
    }

    internal void Confirm(RegionOverlayWindow source, PixelRectangle region)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        _logger.LogInformation("Region confirmed on {Monitor}: {Region}.", source.Frame.Monitor.DeviceName, region);
        _completion.TrySetResult(new RegionSelection(source.Frame, region));
    }

    internal void Cancel()
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        _logger.LogInformation("Region selection cancelled.");
        _completion.TrySetResult(null);
    }
}
