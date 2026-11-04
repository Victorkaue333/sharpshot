namespace SharpShot.Core.Enums;

/// <summary>Modifier flags for a global hotkey. Values match the Win32 MOD_* constants used by RegisterHotKey.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008,
}
