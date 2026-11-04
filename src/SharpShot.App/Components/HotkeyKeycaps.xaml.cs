using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SharpShot.Core.Models;

namespace SharpShot.App.Components;

/// <summary>Renders a <see cref="HotkeyBinding"/> as keycaps: [Ctrl] + [Shift] + [1].</summary>
public sealed partial class HotkeyKeycaps : UserControl
{
    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey), typeof(HotkeyBinding), typeof(HotkeyKeycaps), new PropertyMetadata(null, OnBindingChanged));

    public HotkeyKeycaps()
    {
        InitializeComponent();
    }

    public HotkeyBinding? Hotkey
    {
        get => (HotkeyBinding?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    private static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((HotkeyKeycaps)d).Rebuild();

    private void Rebuild()
    {
        Container.Children.Clear();
        if (Hotkey is null)
        {
            return;
        }

        var keycapStyle = (Style)Application.Current.Resources["KeycapStyle"];
        var keycapTextStyle = (Style)Application.Current.Resources["KeycapTextStyle"];
        var separatorStyle = (Style)Application.Current.Resources["SecondaryTextStyle"];

        var first = true;
        foreach (var part in Hotkey.Parts)
        {
            if (!first)
            {
                Container.Children.Add(new TextBlock { Text = "+", Style = separatorStyle, VerticalAlignment = VerticalAlignment.Center });
            }

            Container.Children.Add(new Border
            {
                Style = keycapStyle,
                Child = new TextBlock { Text = part, Style = keycapTextStyle },
            });
            first = false;
        }

        AutomationPropertiesHelper.SetName(this, Hotkey.ToDisplayString());
    }
}

internal static class AutomationPropertiesHelper
{
    public static void SetName(UIElement element, string name)
        => Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(element, name);
}
