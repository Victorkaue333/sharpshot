using Microsoft.Extensions.Logging.Abstractions;
using SharpShot.Capture.Graphics;
using SharpShot.Capture.Services;
using SharpShot.Core.Models;

namespace SharpShot.Capture.Tests;

public sealed class WindowsGraphicsCaptureServiceTests : IDisposable
{
    private readonly Direct3DDeviceFactory _deviceFactory = new(NullLogger<Direct3DDeviceFactory>.Instance);

    public void Dispose() => _deviceFactory.Dispose();

    private WindowsGraphicsCaptureService CreateService()
        => new(_deviceFactory, NullLogger<WindowsGraphicsCaptureService>.Instance);

    [Fact]
    public void Constructor_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new WindowsGraphicsCaptureService(null!, NullLogger<WindowsGraphicsCaptureService>.Instance));
        Assert.Throws<ArgumentNullException>(() => new WindowsGraphicsCaptureService(_deviceFactory, null!));
    }

    [Fact]
    public async Task CaptureMonitorAsync_RejectsNullMonitor()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateService().CaptureMonitorAsync(null!));
    }

    [Fact]
    public void Direct3DDeviceFactory_ThrowsAfterDispose()
    {
        var factory = new Direct3DDeviceFactory(NullLogger<Direct3DDeviceFactory>.Instance);
        factory.Dispose();

        Assert.Throws<ObjectDisposedException>(() => factory.GetOrCreate());
    }

    [CaptureFact]
    [Trait("Category", "Integration")]
    public async Task CaptureMonitorAsync_PrimaryMonitor_ReturnsNativeResolutionOpaqueFrame()
    {
        var service = CreateService();
        Assert.True(service.IsSupported);
        var monitor = new MonitorService(NullLogger<MonitorService>.Instance).GetPrimaryMonitor();
        var before = DateTimeOffset.Now;

        var frame = await service.CaptureMonitorAsync(monitor);

        Assert.Same(monitor, frame.Monitor);
        Assert.Equal(monitor.Bounds.Width, frame.Pixels.Width);
        Assert.Equal(monitor.Bounds.Height, frame.Pixels.Height);
        Assert.Equal((long)monitor.Bounds.Width * monitor.Bounds.Height * PixelBuffer.BytesPerPixel, frame.Pixels.SizeInBytes);
        Assert.True(frame.Pixels.IsOpaque(), "Captured frame is not opaque.");
        Assert.True(HasNonBlackPixel(frame.Pixels), "Captured frame is entirely black.");
        Assert.InRange(frame.Timestamp, before.AddSeconds(-1), DateTimeOffset.Now.AddSeconds(1));
    }

    [CaptureFact]
    [Trait("Category", "Integration")]
    public async Task CaptureMonitorAsync_TwiceWithSameService_ReusesDevice()
    {
        var service = CreateService();
        var monitor = new MonitorService(NullLogger<MonitorService>.Instance).GetPrimaryMonitor();

        var first = await service.CaptureMonitorAsync(monitor);
        var device = _deviceFactory.GetOrCreate();
        var second = await service.CaptureMonitorAsync(monitor);

        Assert.Same(device, _deviceFactory.GetOrCreate());
        Assert.Equal(first.Pixels.Width, second.Pixels.Width);
        Assert.Equal(first.Pixels.Height, second.Pixels.Height);
    }

    private static bool HasNonBlackPixel(PixelBuffer pixels)
    {
        var data = pixels.Data;
        for (var i = 0; i < data.Length; i += PixelBuffer.BytesPerPixel)
        {
            if (data[i] != 0 || data[i + 1] != 0 || data[i + 2] != 0)
            {
                return true;
            }
        }

        return false;
    }
}
