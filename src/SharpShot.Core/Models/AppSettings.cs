using SharpShot.Core.Enums;

namespace SharpShot.Core.Models;

/// <summary>User preferences persisted as JSON in the local application data folder.</summary>
public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public bool StartWithWindows { get; set; }

    public bool MinimizeToTray { get; set; } = true;

    public bool ShowNotificationAfterCapture { get; set; } = true;

    public HotkeyBinding FullScreenHotkey { get; set; } = HotkeyBinding.DefaultFullScreen;

    public HotkeyBinding RegionHotkey { get; set; } = HotkeyBinding.DefaultRegion;

    /// <summary>Custom save directory. Null or empty means "Pictures\SharpShot".</summary>
    public string? SaveDirectory { get; set; }

    public ThemeMode Theme { get; set; } = ThemeMode.System;

    /// <summary>True after the one-time "SharpShot keeps running in the tray" notice has been shown.</summary>
    public bool TrayHintShown { get; set; }

    public static AppSettings CreateDefault() => new();

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
