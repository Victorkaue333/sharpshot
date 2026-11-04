using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using SharpShot.Capture.Graphics;
using SharpShot.Capture.Interop;
using SharpShot.Core.Enums;
using SharpShot.Core.Exceptions;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;

namespace SharpShot.Capture.Services;

/// <summary>
/// One-shot monitor capture built on Windows Graphics Capture (Windows.Graphics.Capture).
/// The frame pool is free-threaded, so the first frame is awaited without a dispatcher.
/// Frames are requested as B8G8R8A8 UNORM: on HDR displays DWM delivers the SDR-mapped composition,
/// so the PNG is always 8-bit SDR (see docs/capture-engine/hdr.md).
/// </summary>
public sealed class WindowsGraphicsCaptureService : IScreenCaptureService
{
    private const string SessionTypeName = "Windows.Graphics.Capture.GraphicsCaptureSession";
    private static readonly TimeSpan FirstFrameTimeout = TimeSpan.FromSeconds(5);

    private readonly Direct3DDeviceFactory _deviceFactory;
    private readonly ILogger<WindowsGraphicsCaptureService> _logger;
    private readonly TimeProvider _timeProvider;
    private bool? _borderlessAccessGranted;

    public WindowsGraphicsCaptureService(Direct3DDeviceFactory deviceFactory, ILogger<WindowsGraphicsCaptureService> logger, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(deviceFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _deviceFactory = deviceFactory;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool IsSupported
    {
        get
        {
            try
            {
                return GraphicsCaptureSession.IsSupported();
            }
            catch (Exception ex) when (ex is COMException or TypeLoadException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "GraphicsCaptureSession.IsSupported() threw; treating capture as unsupported.");
                return false;
            }
        }
    }

    public async Task<CapturedFrame> CaptureMonitorAsync(MonitorInfo monitor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        if (!IsSupported)
        {
            throw new CaptureException(CaptureErrorKind.NotSupported, "Windows Graphics Capture is not supported in this session (Windows 10 1903+ with a desktop session is required).");
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CaptureOnceAsync(monitor, cancellationToken);
            }
            catch (COMException ex) when (attempt == 1 && IsDeviceLost(ex.HResult))
            {
                _logger.LogWarning(ex, "Direct3D device lost during capture; recreating device and retrying once.");
                _deviceFactory.Invalidate();
            }
        }
    }

    private async Task<CapturedFrame> CaptureOnceAsync(MonitorInfo monitor, CancellationToken cancellationToken)
    {
        var device = _deviceFactory.GetOrCreate();
        GraphicsCaptureItem item;
        try
        {
            item = GraphicsCaptureInterop.CreateItemForMonitor(monitor.Handle);
        }
        catch (COMException ex)
        {
            throw new CaptureException(CaptureErrorKind.NoMonitor, $"Could not create a capture item for monitor {monitor.DeviceName}.", ex);
        }

        var size = item.Size;
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new CaptureException(CaptureErrorKind.NoMonitor, $"Monitor {monitor.DeviceName} reported an empty size.");
        }

        var frameSource = new TaskCompletionSource<Direct3D11CaptureFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, size);
        using var session = framePool.CreateCaptureSession(item);

        void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            var frame = sender.TryGetNextFrame();
            if (frame is null)
            {
                return;
            }

            if (!frameSource.TrySetResult(frame))
            {
                frame.Dispose();
            }
        }

        framePool.FrameArrived += OnFrameArrived;
        try
        {
            ConfigureSession(session);
            var timestamp = _timeProvider.GetLocalNow();
            session.StartCapture();

            Direct3D11CaptureFrame captured;
            try
            {
                captured = await frameSource.Task.WaitAsync(FirstFrameTimeout, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                throw new CaptureException(CaptureErrorKind.Timeout, "No frame arrived from Windows Graphics Capture within 5 seconds.", ex);
            }
            catch (OperationCanceledException ex)
            {
                throw new CaptureException(CaptureErrorKind.Cancelled, "Capture was cancelled.", ex);
            }

            using (captured)
            {
                var pixels = await ReadPixelsAsync(captured, cancellationToken);
                _logger.LogInformation("Captured {Monitor}: {Width}x{Height} px.", monitor.DeviceName, pixels.Width, pixels.Height);
                return new CapturedFrame(pixels, monitor, timestamp);
            }
        }
        finally
        {
            framePool.FrameArrived -= OnFrameArrived;
            // Dispose any frame that raced in after the first one was taken.
            if (frameSource.Task.IsCompletedSuccessfully)
            {
                // already disposed by the using block above
            }
        }
    }

    private void ConfigureSession(GraphicsCaptureSession session)
    {
        if (ApiInformation.IsPropertyPresent(SessionTypeName, nameof(GraphicsCaptureSession.IsCursorCaptureEnabled)))
        {
            session.IsCursorCaptureEnabled = false;
        }

        if (ApiInformation.IsPropertyPresent(SessionTypeName, nameof(GraphicsCaptureSession.IsBorderRequired)))
        {
            if (_borderlessAccessGranted is null)
            {
                _borderlessAccessGranted = TryRequestBorderlessAccess();
            }

            if (_borderlessAccessGranted == true)
            {
                try
                {
                    session.IsBorderRequired = false;
                }
                catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
                {
                    _logger.LogDebug(ex, "Could not disable the capture border; continuing with it.");
                }
            }
        }
    }

    private bool TryRequestBorderlessAccess()
    {
        try
        {
            if (!ApiInformation.IsTypePresent("Windows.Graphics.Capture.GraphicsCaptureAccess"))
            {
                return false;
            }

            var status = GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Borderless).AsTask().GetAwaiter().GetResult();
            _logger.LogDebug("Borderless capture access: {Status}.", status);
            return status == Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus.Allowed;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Borderless capture access request failed.");
            return false;
        }
    }

    private static async Task<PixelBuffer> ReadPixelsAsync(Direct3D11CaptureFrame frame, CancellationToken cancellationToken)
    {
        var contentSize = frame.ContentSize;
        using var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, BitmapAlphaMode.Ignore).AsTask(cancellationToken);
        var source = bitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8
            ? bitmap
            : SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);

        try
        {
            var width = source.PixelWidth;
            var height = source.PixelHeight;
            var buffer = PixelBuffer.Create(width, height);
            source.CopyToBuffer(buffer.Data.AsBuffer());

            // The surface is the frame-pool size; the content may be smaller when the display mode just changed.
            if (contentSize.Width > 0 && contentSize.Height > 0 && (contentSize.Width < width || contentSize.Height < height))
            {
                buffer = buffer.Crop(new PixelRectangle(0, 0, Math.Min(contentSize.Width, width), Math.Min(contentSize.Height, height)));
            }

            buffer.ForceOpaque();
            return buffer;
        }
        finally
        {
            if (!ReferenceEquals(source, bitmap))
            {
                source.Dispose();
            }
        }
    }

    private static bool IsDeviceLost(int hresult)
        => hresult is NativeMethods.DxgiErrorDeviceRemoved or NativeMethods.DxgiErrorDeviceReset;
}
