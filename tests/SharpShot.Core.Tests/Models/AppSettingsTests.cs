using SharpShot.Core.Enums;
using SharpShot.Core.Models;

namespace SharpShot.Core.Tests.Models;

public sealed class AppSettingsTests
{
    [Fact]
    public void CreateDefault_HasExpectedValues()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.False(settings.StartWithWindows);
        Assert.True(settings.MinimizeToTray);
        Assert.True(settings.ShowNotificationAfterCapture);
        Assert.Equal(HotkeyBinding.DefaultFullScreen, settings.FullScreenHotkey);
        Assert.Equal(HotkeyBinding.DefaultRegion, settings.RegionHotkey);
        Assert.Null(settings.SaveDirectory);
        Assert.Equal(ThemeMode.System, settings.Theme);
    }

    [Fact]
    public void Clone_IsIndependentCopy()
    {
        var original = AppSettings.CreateDefault();
        original.SaveDirectory = @"C:\Shots";

        var clone = original.Clone();
        clone.StartWithWindows = true;
        clone.MinimizeToTray = false;
        clone.Theme = ThemeMode.Dark;
        clone.SaveDirectory = @"D:\Other";
        clone.FullScreenHotkey = new HotkeyBinding(HotkeyModifiers.Alt, 0x41);

        Assert.NotSame(original, clone);
        Assert.False(original.StartWithWindows);
        Assert.True(original.MinimizeToTray);
        Assert.Equal(ThemeMode.System, original.Theme);
        Assert.Equal(@"C:\Shots", original.SaveDirectory);
        Assert.Equal(HotkeyBinding.DefaultFullScreen, original.FullScreenHotkey);
    }

    [Fact]
    public void Clone_CopiesAllValues()
    {
        var original = new AppSettings
        {
            StartWithWindows = true,
            MinimizeToTray = false,
            ShowNotificationAfterCapture = false,
            FullScreenHotkey = new HotkeyBinding(HotkeyModifiers.Alt, 0x70),
            RegionHotkey = new HotkeyBinding(HotkeyModifiers.Windows | HotkeyModifiers.Shift, 0x53),
            SaveDirectory = @"C:\Shots",
            Theme = ThemeMode.Light,
        };

        var clone = original.Clone();

        Assert.Equal(original.SchemaVersion, clone.SchemaVersion);
        Assert.Equal(original.StartWithWindows, clone.StartWithWindows);
        Assert.Equal(original.MinimizeToTray, clone.MinimizeToTray);
        Assert.Equal(original.ShowNotificationAfterCapture, clone.ShowNotificationAfterCapture);
        Assert.Equal(original.FullScreenHotkey, clone.FullScreenHotkey);
        Assert.Equal(original.RegionHotkey, clone.RegionHotkey);
        Assert.Equal(original.SaveDirectory, clone.SaveDirectory);
        Assert.Equal(original.Theme, clone.Theme);
    }
}
