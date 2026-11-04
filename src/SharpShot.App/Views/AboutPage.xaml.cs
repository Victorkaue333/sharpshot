using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SharpShot.App.ViewModels;

namespace SharpShot.App.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<AboutViewModel>();
        InitializeComponent();
    }

    public AboutViewModel ViewModel { get; }
}
