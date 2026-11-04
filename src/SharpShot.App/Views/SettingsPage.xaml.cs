using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SharpShot.App.ViewModels;
using SharpShot.Core.Enums;
using SharpShot.Core.Models;

namespace SharpShot.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    private async void FullScreenEditor_BindingCommitted(object? sender, HotkeyBinding binding)
        => await ViewModel.SetHotkeyAsync(CaptureMode.FullScreen, binding);

    private async void RegionEditor_BindingCommitted(object? sender, HotkeyBinding binding)
        => await ViewModel.SetHotkeyAsync(CaptureMode.Region, binding);
}
