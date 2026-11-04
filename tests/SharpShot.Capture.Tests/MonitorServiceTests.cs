using Microsoft.Extensions.Logging.Abstractions;
using SharpShot.Capture.Services;

namespace SharpShot.Capture.Tests;

[Trait("Category", "Display")]
public sealed class MonitorServiceTests
{
    private readonly MonitorService _service = new(NullLogger<MonitorService>.Instance);

    [Fact]
    public void Constructor_RejectsNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() => new MonitorService(null!));
    }

    [DisplayFact]
    public void GetMonitors_ReturnsAtLeastOneMonitorWithPositiveSize()
    {
        var monitors = _service.GetMonitors();

        Assert.NotEmpty(monitors);
        Assert.All(monitors, m =>
        {
            Assert.NotEqual(0, m.Handle);
            Assert.True(m.Bounds.Width > 0, $"{m} has a non-positive width.");
            Assert.True(m.Bounds.Height > 0, $"{m} has a non-positive height.");
            Assert.False(m.WorkArea.IsEmpty, $"{m} has an empty work area.");
            Assert.True(m.Bounds.Contains(m.WorkArea), $"{m}: work area {m.WorkArea} is outside the bounds.");
            Assert.True(m.Dpi >= 96, $"{m} reports DPI {m.Dpi}.");
            Assert.False(string.IsNullOrWhiteSpace(m.DeviceName));
        });
    }

    [DisplayFact]
    public void GetMonitors_HasExactlyOnePrimary_ListedFirst()
    {
        var monitors = _service.GetMonitors();

        Assert.Single(monitors, m => m.IsPrimary);
        Assert.True(monitors[0].IsPrimary);
    }

    [DisplayFact]
    public void GetMonitors_PrimaryIsAtVirtualDesktopOrigin()
    {
        var primary = _service.GetMonitors().Single(m => m.IsPrimary);

        Assert.Equal(0, primary.Bounds.X);
        Assert.Equal(0, primary.Bounds.Y);
    }

    [DisplayFact]
    public void GetPrimaryMonitor_ReturnsThePrimary()
    {
        var primary = _service.GetPrimaryMonitor();

        Assert.True(primary.IsPrimary);
        Assert.Equal(_service.GetMonitors().Single(m => m.IsPrimary), primary);
    }

    [DisplayFact]
    public void GetVirtualDesktopBounds_ContainsEveryMonitor()
    {
        var desktop = _service.GetVirtualDesktopBounds();
        var monitors = _service.GetMonitors();

        Assert.False(desktop.IsEmpty);
        Assert.All(monitors, m => Assert.True(desktop.Contains(m.Bounds), $"{m} is outside {desktop}."));
        Assert.Equal(monitors.Min(m => m.Bounds.Left), desktop.Left);
        Assert.Equal(monitors.Min(m => m.Bounds.Top), desktop.Top);
        Assert.Equal(monitors.Max(m => m.Bounds.Right), desktop.Right);
        Assert.Equal(monitors.Max(m => m.Bounds.Bottom), desktop.Bottom);
    }

    [DisplayFact]
    public void GetMonitorContaining_PrimaryCentre_ReturnsPrimary()
    {
        var primary = _service.GetPrimaryMonitor();
        var centreX = primary.Bounds.X + (primary.Bounds.Width / 2);
        var centreY = primary.Bounds.Y + (primary.Bounds.Height / 2);

        var found = _service.GetMonitorContaining(centreX, centreY);

        Assert.Equal(primary, found);
    }

    [DisplayFact]
    public void GetMonitorContaining_EveryMonitorCentre_ReturnsThatMonitor()
    {
        foreach (var monitor in _service.GetMonitors())
        {
            var found = _service.GetMonitorContaining(
                monitor.Bounds.X + (monitor.Bounds.Width / 2),
                monitor.Bounds.Y + (monitor.Bounds.Height / 2));

            Assert.Equal(monitor.Handle, found.Handle);
        }
    }

    [DisplayFact]
    public void GetMonitorContaining_FarOutsidePoint_ReturnsNearestMonitor()
    {
        var monitors = _service.GetMonitors();

        var found = _service.GetMonitorContaining(-1_000_000, -1_000_000);

        Assert.Contains(found, monitors);
    }

    [DisplayFact]
    public void GetMonitorUnderCursor_ReturnsAKnownMonitor()
    {
        Assert.Contains(_service.GetMonitorUnderCursor(), _service.GetMonitors());
    }
}
