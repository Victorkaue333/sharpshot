using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpShot.Capture.Interop;
using SharpShot.Core.Enums;
using SharpShot.Core.Exceptions;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace SharpShot.Capture.Graphics;

/// <summary>
/// Creates and caches the WinRT <see cref="IDirect3DDevice"/> that Windows Graphics Capture renders into.
/// Hardware first, WARP (software) as fallback. The device is recreated after a device-removed error.
/// </summary>
public sealed class Direct3DDeviceFactory : IDisposable
{
    private readonly ILogger<Direct3DDeviceFactory> _logger;
    private readonly object _gate = new();
    private IDirect3DDevice? _device;
    private bool _disposed;

    public Direct3DDeviceFactory(ILogger<Direct3DDeviceFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public IDirect3DDevice GetOrCreate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_gate)
        {
            return _device ??= Create();
        }
    }

    /// <summary>Drops the cached device so the next call creates a fresh one (after DXGI_ERROR_DEVICE_REMOVED).</summary>
    public void Invalidate()
    {
        lock (_gate)
        {
            _device?.Dispose();
            _device = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Invalidate();
    }

    private IDirect3DDevice Create()
    {
        var hr = TryCreate(NativeMethods.D3DDriverTypeHardware, out var device);
        if (hr < 0 || device is null)
        {
            _logger.LogWarning("Hardware D3D11 device creation failed (0x{HResult:X8}); falling back to WARP.", hr);
            hr = TryCreate(NativeMethods.D3DDriverTypeWarp, out device);
        }

        if (hr < 0 || device is null)
        {
            throw new CaptureException(CaptureErrorKind.DeviceFailure, $"D3D11CreateDevice failed with HRESULT 0x{hr:X8}.", Marshal.GetExceptionForHR(hr));
        }

        return device;
    }

    private static int TryCreate(int driverType, out IDirect3DDevice? device)
    {
        device = null;
        var hr = NativeMethods.D3D11CreateDevice(
            0, driverType, 0, NativeMethods.D3D11CreateDeviceBgraSupport, 0, 0, NativeMethods.D3D11SdkVersion,
            out var d3dDevice, out _, out var context);
        if (hr < 0)
        {
            return hr;
        }

        try
        {
            var iid = NativeMethods.IidIDxgiDevice;
            hr = Marshal.QueryInterface(d3dDevice, in iid, out var dxgiDevice);
            if (hr < 0)
            {
                return hr;
            }

            try
            {
                hr = NativeMethods.CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out var inspectable);
                if (hr < 0)
                {
                    return hr;
                }

                try
                {
                    device = MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
                    return 0;
                }
                finally
                {
                    Marshal.Release(inspectable);
                }
            }
            finally
            {
                Marshal.Release(dxgiDevice);
            }
        }
        finally
        {
            if (context != 0)
            {
                Marshal.Release(context);
            }

            Marshal.Release(d3dDevice);
        }
    }
}
