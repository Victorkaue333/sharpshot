using System.Text.Json;
using SharpShot.Core.Enums;
using SharpShot.Core.Models;

namespace SharpShot.Core.Tests.Models;

public sealed class HotkeyBindingTests
{
    private const uint VkA = 0x41;
    private const uint VkF5 = 0x74;
    private const uint VkNumPlus = 0x6B;
    private const uint VkShift = 0x10;
    private const uint VkControl = 0x11;
    private const uint VkLWin = 0x5B;

    private const HotkeyModifiers CtrlShift = HotkeyModifiers.Control | HotkeyModifiers.Shift;
    private const HotkeyModifiers All = HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Windows;

    private sealed record Holder(HotkeyBinding? Hotkey);

    [Fact]
    public void Defaults_AreCtrlShift1And2()
    {
        Assert.Equal(new HotkeyBinding(CtrlShift, 0x31), HotkeyBinding.DefaultFullScreen);
        Assert.Equal(new HotkeyBinding(CtrlShift, 0x32), HotkeyBinding.DefaultRegion);
        Assert.True(HotkeyBinding.DefaultFullScreen.IsValid);
        Assert.True(HotkeyBinding.DefaultRegion.IsValid);
        Assert.Equal("Ctrl+Shift+1", HotkeyBinding.DefaultFullScreen.ToString());
        Assert.Equal("Ctrl+Shift+2", HotkeyBinding.DefaultRegion.ToString());
    }

    [Fact]
    public void IsValid_FalseWithoutModifier()
    {
        Assert.False(new HotkeyBinding(HotkeyModifiers.None, VkA).IsValid);
    }

    [Theory]
    [InlineData(VkShift)]
    [InlineData(VkControl)]
    [InlineData(VkLWin)]
    [InlineData(0xA0u)]
    [InlineData(0xA5u)]
    public void IsValid_FalseForModifierOnlyMainKey(uint virtualKey)
    {
        Assert.True(VirtualKeyNames.IsModifierKey(virtualKey));
        Assert.False(VirtualKeyNames.IsAssignable(virtualKey));
        Assert.False(new HotkeyBinding(HotkeyModifiers.Control, virtualKey).IsValid);
    }

    [Fact]
    public void IsValid_FalseForUnknownKey()
    {
        Assert.False(new HotkeyBinding(HotkeyModifiers.Control, 0xFF).IsValid);
    }

    [Fact]
    public void ToString_And_ToDisplayString()
    {
        var binding = HotkeyBinding.DefaultFullScreen;

        Assert.Equal("Ctrl+Shift+1", binding.ToString());
        Assert.Equal("Ctrl + Shift + 1", binding.ToDisplayString());
    }

    [Fact]
    public void Parts_FollowWindowsModifierOrder()
    {
        var binding = new HotkeyBinding(All, VkF5);

        Assert.Equal(["Ctrl", "Shift", "Alt", "Win", "F5"], binding.Parts);
        Assert.Equal("Ctrl+Shift+Alt+Win+F5", binding.ToString());
    }

    [Fact]
    public void Parts_UnknownKeyUsesHexName()
    {
        Assert.Equal(["Alt", "0xFF"], new HotkeyBinding(HotkeyModifiers.Alt, 0xFF).Parts);
    }

    [Theory]
    [InlineData("Ctrl+Shift+1", CtrlShift, 0x31u)]
    [InlineData("ctrl+shift+1", CtrlShift, 0x31u)]
    [InlineData("Control+Shift+A", CtrlShift, VkA)]
    [InlineData("Windows+Shift+S", HotkeyModifiers.Windows | HotkeyModifiers.Shift, 0x53u)]
    [InlineData("Win+F5", HotkeyModifiers.Windows, VkF5)]
    [InlineData(" Alt + PrintScreen ", HotkeyModifiers.Alt, 0x2Cu)]
    [InlineData("Shift+Ctrl+Alt+Win+Esc", All, 0x1Bu)]
    [InlineData("Ctrl+-", HotkeyModifiers.Control, 0xBDu)]
    public void TryParse_AcceptsValidTextAndAliases(string text, HotkeyModifiers modifiers, uint virtualKey)
    {
        Assert.True(HotkeyBinding.TryParse(text, out var binding));
        Assert.Equal(new HotkeyBinding(modifiers, virtualKey), binding);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("garbage")]
    [InlineData("Ctrl+Banana")]
    [InlineData("Ctrl+Shift")] // modifiers only, no main key
    [InlineData("Ctrl+A+B")] // two main keys
    [InlineData("Ctrl+1+2")]
    [InlineData("A")] // no modifier
    [InlineData("Hyper+A")]
    public void TryParse_RejectsInvalidText(string? text)
    {
        Assert.False(HotkeyBinding.TryParse(text, out _));
    }

    [Fact]
    public void TryParse_RoundTripsEveryAssignableKey()
    {
        var keys = Enumerable.Range(0, 256).Select(i => (uint)i)
            .Where(VirtualKeyNames.IsAssignable)
            .Where(vk => vk != VkNumPlus) // see TryParse_RoundTripsNumPlus (known issue)
            .ToList();
        Assert.NotEmpty(keys);

        foreach (var vk in keys)
        {
            var original = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Alt, vk);
            Assert.True(HotkeyBinding.TryParse(original.ToString(), out var parsed), $"'{original}' did not parse.");
            Assert.Equal(original, parsed);
        }
    }

    [Fact]
    public void TryParse_RoundTripsNumPlus()
    {
        var original = new HotkeyBinding(HotkeyModifiers.Control, VkNumPlus);

        Assert.True(HotkeyBinding.TryParse(original.ToString(), out var parsed));
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void VirtualKeyNames_MapsNamesCaseInsensitively()
    {
        Assert.True(VirtualKeyNames.TryGetVirtualKey("pageup", out var vk));
        Assert.Equal(0x21u, vk);
        Assert.Equal("PageUp", VirtualKeyNames.GetName(0x21));
        Assert.Equal("F24", VirtualKeyNames.GetName(0x87));
        Assert.Equal("Num0", VirtualKeyNames.GetName(0x60));
        Assert.False(VirtualKeyNames.TryGetVirtualKey("NotAKey", out _));
    }

    [Fact]
    public void Json_SerializesAsStringValue()
    {
        var json = JsonSerializer.Serialize(HotkeyBinding.DefaultRegion);

        // The default encoder may escape '+' as +, so compare the decoded value.
        using var document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.String, document.RootElement.ValueKind);
        Assert.Equal("Ctrl+Shift+2", document.RootElement.GetString());
    }

    [Fact]
    public void Json_RoundTripsAsPropertyValue()
    {
        var holder = new Holder(new HotkeyBinding(HotkeyModifiers.Alt | HotkeyModifiers.Windows, VkF5));

        var json = JsonSerializer.Serialize(holder);
        var restored = JsonSerializer.Deserialize<Holder>(json);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("Alt+Win+F5", document.RootElement.GetProperty("Hotkey").GetString());
        Assert.Equal(holder, restored);
    }

    [Fact]
    public void Json_NullTokenDeserializesToNull()
    {
        var restored = JsonSerializer.Deserialize<Holder>("{\"Hotkey\":null}");

        Assert.NotNull(restored);
        Assert.Null(restored.Hotkey);
    }

    [Theory]
    [InlineData("\"Ctrl+Banana\"")]
    [InlineData("\"A\"")]
    [InlineData("\"\"")]
    public void Json_InvalidStringDeserializesToNull(string json)
    {
        // An unparseable hotkey must not invalidate the whole settings file (settings fall back to defaults).
        Assert.Null(JsonSerializer.Deserialize<HotkeyBinding>(json));
    }
}
