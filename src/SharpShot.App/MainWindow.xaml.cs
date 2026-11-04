using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using SharpShot.App.Services;
using SharpShot.App.Views;
using SharpShot.Core.Models;
using Windows.Graphics;

namespace SharpShot.App;

public sealed partial class MainWindow : Window
{
    private const int DefaultWidth = 980;
    private const int DefaultHeight = 700;
    private const int MinimumWidth = 760;
    private const int MinimumHeight = 560;

    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarArea);

        var appWindow = AppWindow;
        appWindow.Title = "SharpShot";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "SharpShot.ico");
        if (File.Exists(iconPath))
        {
            appWindow.SetIcon(iconPath);
        }

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = MinimumWidth;
            presenter.PreferredMinimumHeight = MinimumHeight;
        }

        appWindow.Closing += OnClosing;

        var settings = App.Current.Settings;
        ThemeService.Apply(this, settings.Current.Theme);
        settings.Changed += OnSettingsChanged;
        Root.ActualThemeChanged += (_, _) => ThemeService.ApplyTitleBar(AppWindow, Root.ActualTheme);

        Root.Loaded += (_, _) =>
        {
            var scale = Root.XamlRoot?.RasterizationScale ?? 1.0;
            appWindow.Resize(new SizeInt32((int)(DefaultWidth * scale), (int)(DefaultHeight * scale)));
            CenterOnScreen(appWindow);
        };

        ContentFrame.Navigate(typeof(HomePage), null, new SuppressNavigationTransitionInfo());
    }

    /// <summary>Raised when the window hid itself instead of closing (minimize to tray).</summary>
    public event EventHandler? HideRequested;

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_forceClose)
        {
            return;
        }

        if (App.Current.ShouldHideOnClose)
        {
            args.Cancel = true;
            sender.Hide();
            HideRequested?.Invoke(this, EventArgs.Empty);
            App.Current.OnHiddenToTray();
            return;
        }

        args.Cancel = true;
        App.Current.RequestExit();
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
        => DispatcherQueue.TryEnqueue(() => ThemeService.Apply(this, settings.Theme));

    private void Navigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        var target = (item.Tag as string) switch
        {
            "settings" => typeof(SettingsPage),
            "about" => typeof(AboutPage),
            _ => typeof(HomePage),
        };

        if (ContentFrame.CurrentSourcePageType != target)
        {
            ContentFrame.Navigate(target, null, new EntranceNavigationTransitionInfo());
        }
    }

    private static void CenterOnScreen(AppWindow appWindow)
    {
        var area = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary)?.WorkArea;
        if (area is null)
        {
            return;
        }

        var x = area.Value.X + Math.Max(0, (area.Value.Width - appWindow.Size.Width) / 2);
        var y = area.Value.Y + Math.Max(0, (area.Value.Height - appWindow.Size.Height) / 2);
        appWindow.Move(new PointInt32(x, y));
    }
}
