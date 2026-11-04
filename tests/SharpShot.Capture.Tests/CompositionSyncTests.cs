using System.Diagnostics;
using SharpShot.Capture.Services;

namespace SharpShot.Capture.Tests;

public sealed class CompositionSyncTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(2);

    [Fact]
    public void WaitForComposition_DefaultFrames_ReturnsPromptly()
    {
        var stopwatch = Stopwatch.StartNew();

        CompositionSync.WaitForComposition();

        Assert.True(stopwatch.Elapsed < Limit, $"WaitForComposition took {stopwatch.Elapsed}.");
    }

    [Theory]
    [InlineData(0)] // clamped to one frame
    [InlineData(-3)]
    [InlineData(1)]
    public void WaitForComposition_AnyFrameCount_ReturnsPromptly(int frames)
    {
        var stopwatch = Stopwatch.StartNew();

        CompositionSync.WaitForComposition(frames);

        Assert.True(stopwatch.Elapsed < Limit, $"WaitForComposition({frames}) took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task WaitForCompositionAsync_ReturnsPromptly()
    {
        var stopwatch = Stopwatch.StartNew();

        await CompositionSync.WaitForCompositionAsync().WaitAsync(Limit);

        Assert.True(stopwatch.Elapsed < Limit, $"WaitForCompositionAsync took {stopwatch.Elapsed}.");
    }
}
