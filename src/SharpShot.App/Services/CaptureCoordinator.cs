using Microsoft.Extensions.Logging;
using SharpShot.Core.Enums;
using SharpShot.Core.Exceptions;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;
using SharpShot.Core.Services;

namespace SharpShot.App.Services;

public enum CaptureStatusKind
{
    Ready,
    Working,
    Success,
    Cancelled,
    Failure,
}

public sealed record CaptureStatus(CaptureStatusKind Kind, string Message, CaptureResult? Result = null);

public interface ICaptureCoordinator
{
    CaptureStatus Status { get; }

    /// <summary>Raised on the UI thread whenever the status changes.</summary>
    event EventHandler<CaptureStatus>? StatusChanged;

    Task CaptureFullScreenAsync();

    Task CaptureRegionAsync();
}

/// <summary>
/// Orchestrates a capture end to end: hide our windows → wait for composition → grab frame(s) →
/// (select region) → crop → encode PNG → save → notify. Re-entrant requests are ignored while one runs.
/// </summary>
internal sealed class CaptureCoordinator : ICaptureCoordinator
{
    private readonly IMonitorService _monitors;
    private readonly IScreenCaptureService _capture;
    private readonly IImageEncoder _encoder;
    private readonly IScreenshotStorage _storage;
    private readonly ISettingsService _settings;
    private readonly INotificationService _notifications;
    private readonly IWindowHost _windowHost;
    private readonly RegionSelectionSession.Factory _selectionFactory;
    private readonly ILogger<CaptureCoordinator> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CaptureCoordinator(
        IMonitorService monitors,
        IScreenCaptureService capture,
        IImageEncoder encoder,
        IScreenshotStorage storage,
        ISettingsService settings,
        INotificationService notifications,
        IWindowHost windowHost,
        RegionSelectionSession.Factory selectionFactory,
        ILogger<CaptureCoordinator> logger)
    {
        _monitors = monitors;
        _capture = capture;
        _encoder = encoder;
        _storage = storage;
        _settings = settings;
        _notifications = notifications;
        _windowHost = windowHost;
        _selectionFactory = selectionFactory;
        _logger = logger;
    }

    public CaptureStatus Status { get; private set; } = new(CaptureStatusKind.Ready, "Pronto para capturar");

    public event EventHandler<CaptureStatus>? StatusChanged;

    public Task CaptureFullScreenAsync() => RunAsync(CaptureMode.FullScreen, async () =>
    {
        var monitor = _monitors.GetPrimaryMonitor();
        var frame = await _capture.CaptureMonitorAsync(monitor);
        return (frame.Pixels, frame.Timestamp);
    });

    public Task CaptureRegionAsync() => RunAsync(CaptureMode.Region, async () =>
    {
        var monitors = _monitors.GetMonitors();
        var frames = new List<CapturedFrame>(monitors.Count);
        foreach (var monitor in monitors)
        {
            frames.Add(await _capture.CaptureMonitorAsync(monitor));
        }

        var session = _selectionFactory(frames);
        var selection = await session.RunAsync();
        if (selection is null)
        {
            return null;
        }

        var bounds = new PixelRectangle(0, 0, selection.Frame.Pixels.Width, selection.Frame.Pixels.Height);
        var region = RegionMath.Clamp(selection.Region, bounds);
        if (region.IsEmpty)
        {
            return null;
        }

        return (selection.Frame.Pixels.Crop(region), selection.Frame.Timestamp);
    });

    private async Task RunAsync(CaptureMode mode, Func<Task<(PixelBuffer Pixels, DateTimeOffset Timestamp)?>> produce)
    {
        if (!await _gate.WaitAsync(0))
        {
            _logger.LogDebug("Capture request ignored: another capture is in progress.");
            return;
        }

        var wasVisible = _windowHost.IsMainWindowVisible;
        try
        {
            SetStatus(new CaptureStatus(CaptureStatusKind.Working, mode == CaptureMode.Region ? "Selecione uma região…" : "Capturando…"));
            var restore = await _windowHost.HideForCaptureAsync();
            (PixelBuffer Pixels, DateTimeOffset Timestamp)? produced;
            try
            {
                produced = await produce();
            }
            finally
            {
                await restore();
            }

            if (produced is null)
            {
                SetStatus(new CaptureStatus(CaptureStatusKind.Cancelled, "Captura cancelada"));
                return;
            }

            var (pixels, timestamp) = produced.Value;
            var result = await EncodeAndSaveAsync(pixels, mode, timestamp);
            SetStatus(new CaptureStatus(CaptureStatusKind.Success, $"Captura salva: {result.FileName}", result));

            if (_settings.Current.ShowNotificationAfterCapture)
            {
                _notifications.ShowCaptureSaved(result);
            }
        }
        catch (Exception ex) when (ex is CaptureException or ScreenshotSaveException or OperationCanceledException)
        {
            var message = UserMessages.ForException(ex);
            _logger.LogError(ex, "Capture failed ({Mode}): {Message}", mode, message);
            SetStatus(new CaptureStatus(CaptureStatusKind.Failure, message));
            if (!wasVisible)
            {
                _notifications.ShowCaptureFailed(message);
            }
        }
        catch (Exception ex)
        {
            var message = UserMessages.ForException(ex);
            _logger.LogError(ex, "Unexpected capture failure ({Mode}).", mode);
            SetStatus(new CaptureStatus(CaptureStatusKind.Failure, message));
            if (!wasVisible)
            {
                _notifications.ShowCaptureFailed(message);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CaptureResult> EncodeAndSaveAsync(PixelBuffer pixels, CaptureMode mode, DateTimeOffset timestamp)
    {
        var data = await _encoder.EncodeAsync(pixels);
        var directory = _storage.ResolveDirectory(_settings.Current.SaveDirectory);
        var fileName = FileNaming.CreateFileName(timestamp, _encoder.FileExtension);
        var saved = await _storage.SaveAsync(data, directory, fileName);
        return new CaptureResult(saved.FilePath, pixels.Width, pixels.Height, mode, timestamp, saved.SizeBytes);
    }

    private void SetStatus(CaptureStatus status)
    {
        Status = status;
        if (_windowHost.DispatcherQueue.HasThreadAccess)
        {
            StatusChanged?.Invoke(this, status);
        }
        else
        {
            _windowHost.DispatcherQueue.TryEnqueue(() => StatusChanged?.Invoke(this, status));
        }
    }
}
