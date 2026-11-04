using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpShot.Capture.Interop;
using SharpShot.Core.Enums;
using SharpShot.Core.Exceptions;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;

namespace SharpShot.Capture.Services;

/// <summary>Enumerates monitors through EnumDisplayMonitors / GetMonitorInfo / GetDpiForMonitor.</summary>
public sealed class MonitorService : IMonitorService
{
    private readonly ILogger<MonitorService> _logger;

    public MonitorService(ILogger<MonitorService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var handles = new List<nint>();
        NativeMethods.MonitorEnumProc callback = (nint hMonitor, nint _, ref NativeMethods.RECT _, nint _) =>
        {
            handles.Add(hMonitor);
            return true;
        };

        if (!NativeMethods.EnumDisplayMonitors(0, 0, callback, 0))
        {
            throw new CaptureException(CaptureErrorKind.NoMonitor, "EnumDisplayMonitors failed.", new Win32Exception(Marshal.GetLastWin32Error()));
        }

        GC.KeepAlive(callback);

        var monitors = new List<MonitorInfo>(handles.Count);
        foreach (var handle in handles)
        {
            var info = new NativeMethods.MONITORINFOEXW { cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEXW>() };
            if (!NativeMethods.GetMonitorInfoW(handle, ref info))
            {
                _logger.LogWarning("GetMonitorInfo failed for monitor 0x{Handle:X}.", handle);
                continue;
            }

            uint dpi = 96;
            if (NativeMethods.GetDpiForMonitor(handle, NativeMethods.MdtEffectiveDpi, out var dpiX, out _) >= 0 && dpiX > 0)
            {
                dpi = dpiX;
            }

            monitors.Add(new MonitorInfo(
                handle,
                info.szDevice,
                ToRectangle(info.rcMonitor),
                ToRectangle(info.rcWork),
                (info.dwFlags & NativeMethods.MonitorInfoPrimary) != 0,
                dpi));
        }

        if (monitors.Count == 0)
        {
            throw new CaptureException(CaptureErrorKind.NoMonitor, "No active monitors were found.");
        }

        // Primary first, then left-to-right / top-to-bottom for a stable order.
        return monitors
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.Bounds.X)
            .ThenBy(m => m.Bounds.Y)
            .ToList();
    }

    public MonitorInfo GetPrimaryMonitor()
    {
        var monitors = GetMonitors();
        return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
    }

    public MonitorInfo GetMonitorContaining(int x, int y)
    {
        var handle = NativeMethods.MonitorFromPoint(new NativeMethods.POINT { X = x, Y = y }, NativeMethods.MonitorDefaultToNearest);
        var monitors = GetMonitors();
        return monitors.FirstOrDefault(m => m.Handle == handle)
               ?? monitors.FirstOrDefault(m => m.Bounds.Contains(x, y))
               ?? GetPrimaryMonitor();
    }

    public MonitorInfo GetMonitorUnderCursor()
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return GetPrimaryMonitor();
        }

        return GetMonitorContaining(point.X, point.Y);
    }

    public PixelRectangle GetVirtualDesktopBounds()
    {
        var monitors = GetMonitors();
        var left = monitors.Min(m => m.Bounds.Left);
        var top = monitors.Min(m => m.Bounds.Top);
        var right = monitors.Max(m => m.Bounds.Right);
        var bottom = monitors.Max(m => m.Bounds.Bottom);
        return PixelRectangle.FromLTRB(left, top, right, bottom);
    }

    private static PixelRectangle ToRectangle(NativeMethods.RECT rect)
        => PixelRectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
}
