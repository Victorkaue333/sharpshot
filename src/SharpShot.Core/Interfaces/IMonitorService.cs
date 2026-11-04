using SharpShot.Core.Models;

namespace SharpShot.Core.Interfaces;

/// <summary>Enumerates physical displays with their physical-pixel bounds and DPI.</summary>
public interface IMonitorService
{
    IReadOnlyList<MonitorInfo> GetMonitors();

    MonitorInfo GetPrimaryMonitor();

    /// <summary>Monitor containing the physical point, or the nearest one.</summary>
    MonitorInfo GetMonitorContaining(int x, int y);

    MonitorInfo GetMonitorUnderCursor();

    /// <summary>Bounding box of all monitors (the virtual desktop), in physical pixels.</summary>
    PixelRectangle GetVirtualDesktopBounds();
}
