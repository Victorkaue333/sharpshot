using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SharpShot.Core.Enums;
using Windows.UI;

namespace SharpShot.App.Services;

/// <summary>Applies the configured theme (system / light / dark) to a window and its title bar buttons.</summary>
internal static class ThemeService
{
    public static ElementTheme ToElementTheme(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => ElementTheme.Light,
        ThemeMode.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    public static void Apply(Window window, ThemeMode mode)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = ToElementTheme(mode);
            ApplyTitleBar(window.AppWindow, root.ActualTheme);
        }
    }

    public static void ApplyTitleBar(AppWindow appWindow, ElementTheme actualTheme)
    {
        if (appWindow?.TitleBar is not { } titleBar || !AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var dark = actualTheme == ElementTheme.Dark;
        var foreground = dark ? Color.FromArgb(255, 0xF5, 0xF7, 0xFA) : Color.FromArgb(255, 0x0D, 0x10, 0x17);
        var hover = dark ? Color.FromArgb(255, 0x2A, 0x34, 0x47) : Color.FromArgb(255, 0xE2, 0xE8, 0xF0);
        var pressed = dark ? Color.FromArgb(255, 0x36, 0x42, 0x58) : Color.FromArgb(255, 0xCB, 0xD5, 0xE1);

        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 0x94, 0xA3, 0xB8);
        titleBar.ButtonHoverBackgroundColor = hover;
        titleBar.ButtonHoverForegroundColor = foreground;
        titleBar.ButtonPressedBackgroundColor = pressed;
        titleBar.ButtonPressedForegroundColor = foreground;
    }
}
