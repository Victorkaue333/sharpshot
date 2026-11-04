using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SharpShot.Core.Enums;
using SharpShot.Core.Models;
using Windows.System;
using Windows.UI.Core;

namespace SharpShot.App.Components;

/// <summary>
/// Shows a hotkey as keycaps and lets the user record a new one: toggle the pencil, press a key combination.
/// The new binding is raised through <see cref="BindingCommitted"/>; the host decides whether to accept it.
/// </summary>
public sealed partial class HotkeyEditor : UserControl
{
    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey), typeof(HotkeyBinding), typeof(HotkeyEditor), new PropertyMetadata(null, OnBindingChanged));

    public HotkeyEditor()
    {
        InitializeComponent();
    }

    public HotkeyBinding? Hotkey
    {
        get => (HotkeyBinding?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    /// <summary>Raised when the user pressed a valid combination while recording.</summary>
    public event EventHandler<HotkeyBinding>? BindingCommitted;

    private static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (HotkeyEditor)d;
        editor.Keycaps.Hotkey = editor.Hotkey;
    }

    private void EditToggle_Checked(object sender, RoutedEventArgs e)
    {
        RecordingText.Text = "Pressione as teclas…";
        RecordingBox.Visibility = Visibility.Visible;
        Keycaps.Visibility = Visibility.Collapsed;
        EditToggle.Focus(FocusState.Programmatic);
    }

    private void EditToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        RecordingBox.Visibility = Visibility.Collapsed;
        Keycaps.Visibility = Visibility.Visible;
    }

    private void EditToggle_LostFocus(object sender, RoutedEventArgs e)
    {
        if (EditToggle.IsChecked == true)
        {
            EditToggle.IsChecked = false;
        }
    }

    private void EditToggle_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (EditToggle.IsChecked != true)
        {
            return;
        }

        e.Handled = true;
        var key = (uint)e.Key;

        if (e.Key == VirtualKey.Escape)
        {
            EditToggle.IsChecked = false;
            return;
        }

        if (VirtualKeyNames.IsModifierKey(key))
        {
            RecordingText.Text = DescribeModifiers(ReadModifiers()) + " …";
            return;
        }

        var modifiers = ReadModifiers();
        var candidate = new HotkeyBinding(modifiers, key);
        if (!candidate.IsValid)
        {
            RecordingText.Text = modifiers == HotkeyModifiers.None
                ? "Inclua Ctrl, Shift, Alt ou Win"
                : "Tecla não suportada";
            return;
        }

        EditToggle.IsChecked = false;
        BindingCommitted?.Invoke(this, candidate);
    }

    private static HotkeyModifiers ReadModifiers()
    {
        var modifiers = HotkeyModifiers.None;
        if (IsDown(VirtualKey.Control))
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (IsDown(VirtualKey.Shift))
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (IsDown(VirtualKey.Menu))
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
        {
            modifiers |= HotkeyModifiers.Windows;
        }

        return modifiers;
    }

    private static bool IsDown(VirtualKey key)
        => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

    private static string DescribeModifiers(HotkeyModifiers modifiers)
    {
        if (modifiers == HotkeyModifiers.None)
        {
            return "Pressione as teclas";
        }

        var probe = new HotkeyBinding(modifiers, HotkeyBinding.VkDigit1);
        var parts = probe.Parts.Take(probe.Parts.Count - 1);
        return string.Join(" + ", parts);
    }
}
