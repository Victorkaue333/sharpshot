using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpShot.Core.Enums;

namespace SharpShot.Core.Models;

/// <summary>
/// A global keyboard shortcut: one or more modifiers plus a main key.
/// Serialized as a compact string such as "Ctrl+Shift+1".
/// </summary>
[JsonConverter(typeof(HotkeyBindingJsonConverter))]
public sealed record HotkeyBinding(HotkeyModifiers Modifiers, uint VirtualKey)
{
    public const uint VkDigit1 = 0x31;
    public const uint VkDigit2 = 0x32;

    public static HotkeyBinding DefaultFullScreen { get; } = new(HotkeyModifiers.Control | HotkeyModifiers.Shift, VkDigit1);
    public static HotkeyBinding DefaultRegion { get; } = new(HotkeyModifiers.Control | HotkeyModifiers.Shift, VkDigit2);

    /// <summary>A binding is usable when it has at least one modifier and an assignable main key.</summary>
    public bool IsValid => Modifiers != HotkeyModifiers.None && VirtualKeyNames.IsAssignable(VirtualKey);

    /// <summary>Display parts in Windows order: Ctrl, Shift, Alt, Win, then the key.</summary>
    public IReadOnlyList<string> Parts
    {
        get
        {
            var parts = new List<string>(5);
            if (Modifiers.HasFlag(HotkeyModifiers.Control))
            {
                parts.Add("Ctrl");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Shift))
            {
                parts.Add("Shift");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Alt))
            {
                parts.Add("Alt");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Windows))
            {
                parts.Add("Win");
            }

            parts.Add(VirtualKeyNames.GetName(VirtualKey));
            return parts;
        }
    }

    /// <summary>"Ctrl + Shift + 1" — for UI text.</summary>
    public string ToDisplayString() => string.Join(" + ", Parts);

    /// <summary>"Ctrl+Shift+1" — for settings files.</summary>
    public override string ToString() => string.Join("+", Parts);

    public static bool TryParse(string? text, [NotNullWhen(true)] out HotkeyBinding? binding)
    {
        binding = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Consume leading "<modifier>+" tokens; whatever remains is the key name, which may itself
        // contain '+' (e.g. "Num+"), so a plain Split('+') would be wrong.
        var modifiers = HotkeyModifiers.None;
        var remaining = text.Trim();
        while (true)
        {
            var plus = remaining.IndexOf('+');
            if (plus <= 0)
            {
                break;
            }

            var modifier = ParseModifier(remaining[..plus].Trim());
            if (modifier is null)
            {
                break;
            }

            modifiers |= modifier.Value;
            remaining = remaining[(plus + 1)..].Trim();
        }

        if (remaining.Length == 0 || !VirtualKeyNames.TryGetVirtualKey(remaining, out var key))
        {
            return false;
        }

        binding = new HotkeyBinding(modifiers, key);
        return binding.IsValid;
    }

    private static HotkeyModifiers? ParseModifier(string token) => token.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => HotkeyModifiers.Control,
        "SHIFT" => HotkeyModifiers.Shift,
        "ALT" => HotkeyModifiers.Alt,
        "WIN" or "WINDOWS" => HotkeyModifiers.Windows,
        _ => null,
    };
}

public sealed class HotkeyBindingJsonConverter : JsonConverter<HotkeyBinding>
{
    public override HotkeyBinding? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // An unparseable hotkey must not invalidate the whole settings file: return null and let the
        // settings service fall back to the default binding for that action.
        return HotkeyBinding.TryParse(reader.GetString(), out var binding) ? binding : null;
    }

    public override void Write(Utf8JsonWriter writer, HotkeyBinding value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
