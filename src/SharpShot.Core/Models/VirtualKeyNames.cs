using System.Collections.Frozen;

namespace SharpShot.Core.Models;

/// <summary>
/// Maps Win32 virtual-key codes to the short names used in settings files and in the UI
/// ("Ctrl+Shift+1"). Only keys that make sense as a hotkey are listed.
/// </summary>
public static class VirtualKeyNames
{
    private static readonly FrozenDictionary<uint, string> NamesByKey = BuildNames();
    private static readonly FrozenDictionary<string, uint> KeysByName =
        NamesByKey.ToDictionary(p => p.Value, p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<uint> ModifierKeys =
    [
        0x10, 0x11, 0x12, // VK_SHIFT, VK_CONTROL, VK_MENU
        0x5B, 0x5C,       // VK_LWIN, VK_RWIN
        0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, // VK_LSHIFT..VK_RMENU
    ];

    public static bool IsModifierKey(uint virtualKey) => ModifierKeys.Contains(virtualKey);

    /// <summary>True when the key can be the main key of a hotkey binding.</summary>
    public static bool IsAssignable(uint virtualKey) => NamesByKey.ContainsKey(virtualKey);

    public static string GetName(uint virtualKey)
        => NamesByKey.TryGetValue(virtualKey, out var name) ? name : $"0x{virtualKey:X2}";

    public static bool TryGetVirtualKey(string name, out uint virtualKey)
    {
        ArgumentNullException.ThrowIfNull(name);
        return KeysByName.TryGetValue(name.Trim(), out virtualKey);
    }

    private static FrozenDictionary<uint, string> BuildNames()
    {
        var map = new Dictionary<uint, string>();

        for (uint vk = 0x30; vk <= 0x39; vk++)
        {
            map[vk] = ((char)vk).ToString(); // 0-9
        }

        for (uint vk = 0x41; vk <= 0x5A; vk++)
        {
            map[vk] = ((char)vk).ToString(); // A-Z
        }

        for (uint i = 0; i < 24; i++)
        {
            map[0x70 + i] = $"F{i + 1}";
        }

        for (uint i = 0; i < 10; i++)
        {
            map[0x60 + i] = $"Num{i}";
        }

        map[0x6A] = "Num*";
        map[0x6B] = "Num+";
        map[0x6D] = "Num-";
        map[0x6E] = "Num.";
        map[0x6F] = "Num/";

        map[0x08] = "Backspace";
        map[0x09] = "Tab";
        map[0x0D] = "Enter";
        map[0x13] = "Pause";
        map[0x14] = "CapsLock";
        map[0x1B] = "Esc";
        map[0x20] = "Space";
        map[0x21] = "PageUp";
        map[0x22] = "PageDown";
        map[0x23] = "End";
        map[0x24] = "Home";
        map[0x25] = "Left";
        map[0x26] = "Up";
        map[0x27] = "Right";
        map[0x28] = "Down";
        map[0x2C] = "PrintScreen";
        map[0x2D] = "Insert";
        map[0x2E] = "Delete";
        map[0x90] = "NumLock";
        map[0x91] = "ScrollLock";

        map[0xBA] = ";";
        map[0xBB] = "=";
        map[0xBC] = ",";
        map[0xBD] = "-";
        map[0xBE] = ".";
        map[0xBF] = "/";
        map[0xC0] = "`";
        map[0xDB] = "[";
        map[0xDC] = "\\";
        map[0xDD] = "]";
        map[0xDE] = "'";

        return map.ToFrozenDictionary();
    }
}
