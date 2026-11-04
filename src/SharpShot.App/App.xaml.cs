using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SharpShot.App.Infrastructure;
using SharpShot.App.Services;
using SharpShot.App.ViewModels;
using SharpShot.Capture.Encoding;
using SharpShot.Capture.Graphics;
using SharpShot.Capture.Services;
using SharpShot.Core.Enums;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;
using SharpShot.Storage.Configuration;
using SharpShot.Storage.Services;

namespace SharpShot.App;

public partial class App : Application
{
    private readonly AppInstance _instance;
    private readonly string[] _args;
    private ServiceProvider? _services;
    private ILogger<App>? _logger;
    private WindowHost? _windowHost;
    private TrayIconService? _tray;
    private HotkeyService? _hotkeys;
    private NotificationService? _notifications;
    private bool _exiting;

    public App(AppInstance instance, string[] args)
    {
        _instance = instance;
        _args = args;
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            _logger?.LogCritical(e.ExceptionObject as Exception, "Fatal unhandled exception (AppDomain).");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _logger?.LogError(e.Exception, "Unobserved task exception.");
            e.SetObserved();
        };
    }

    public static new App Current => (App)Application.Current;

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("Services are not initialized yet.");

    public ISettingsService Settings => Services.GetRequiredService<ISettingsService>();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var dispatcher = DispatcherQueue.GetForCurrentThread();
        _services = BuildServices(dispatcher);
        _logger = _services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("SharpShot {Version} starting (args: {Args}).", AboutViewModel.Version, string.Join(' ', _args));

        var settings = _services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();

        _windowHost = (WindowHost)_services.GetRequiredService<IWindowHost>();
        _notifications = _services.GetRequiredService<NotificationService>();
        _notifications.Initialize();

        _hotkeys = _services.GetRequiredService<HotkeyService>();
        _hotkeys.HotkeyPressed += OnHotkeyPressed;
        var registrations = _hotkeys.ApplySettings(settings.Current);
        foreach (var (mode, result) in registrations)
        {
            if (!result.Success)
            {
                _logger.LogWarning("Hotkey for {Mode} not registered: {Error}", mode, result.Error);
            }
        }

        _tray = _services.GetRequiredService<TrayIconService>();
        _tray.ActionRequested += OnTrayAction;
        _tray.Show();

        _instance.Activated += OnInstanceActivated;

        var startMinimized = _args.Contains(StartupService.MinimizedArgument, StringComparer.OrdinalIgnoreCase);
        if (!startMinimized)
        {
            _windowHost.ShowMainWindow();
        }
        else
        {
            _logger.LogInformation("Started minimized to the tray.");
        }
    }

    /// <summary>Hotkey registration results, exposed so the settings page can show conflicts found at startup.</summary>
    public IHotkeyService Hotkeys => _hotkeys ?? throw new InvalidOperationException();

    public void RequestExit()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        _logger?.LogInformation("Exit requested; releasing resources.");
        _instance.Activated -= OnInstanceActivated;
        _tray?.Dispose();
        _hotkeys?.Dispose();
        _notifications?.Dispose();
        _windowHost?.CloseAndExit();
        _services?.Dispose();
        Exit();
    }

    /// <summary>True while the user chose to keep SharpShot in the tray when the window closes.</summary>
    public bool ShouldHideOnClose => Settings.Current.MinimizeToTray && !_exiting;

    /// <summary>Called when the main window hid itself to the tray; shows the one-time tray notice.</summary>
    public async void OnHiddenToTray()
    {
        try
        {
            if (Settings.Current.TrayHintShown)
            {
                return;
            }

            _notifications?.ShowTrayHint();
            var snapshot = Settings.Current.Clone();
            snapshot.TrayHintShown = true;
            await Settings.SaveAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not record the tray hint flag.");
        }
    }

    private ServiceProvider BuildServices(DispatcherQueue dispatcher)
    {
        var services = new ServiceCollection();

        var logDirectory = Path.Combine(SettingsService.GetApplicationDataDirectory(), "logs");
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddDebug();
            builder.AddProvider(new FileLoggerProvider(logDirectory, LogLevel.Information));
        });

        services.AddSingleton(dispatcher);
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IScreenshotStorage, ScreenshotStorageService>();
        services.AddSingleton<IMonitorService, MonitorService>();
        services.AddSingleton<Direct3DDeviceFactory>();
        services.AddSingleton<IScreenCaptureService, WindowsGraphicsCaptureService>();
        services.AddSingleton<IImageEncoder, PngImageEncoder>();

        services.AddSingleton<MessageWindow>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<IHotkeyService>(sp => sp.GetRequiredService<HotkeyService>());
        services.AddSingleton<TrayIconService>();
        services.AddSingleton<ShellService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IWindowHost>(sp => new WindowHost(() => new MainWindow(), sp.GetRequiredService<DispatcherQueue>()));
        services.AddSingleton<RegionSelectionSession.Factory>(sp => frames =>
            new RegionSelectionSession(frames, sp.GetRequiredService<ILogger<RegionSelectionSession>>()));
        services.AddSingleton<ICaptureCoordinator, CaptureCoordinator>();

        services.AddTransient<HomeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private void OnHotkeyPressed(object? sender, CaptureMode mode)
    {
        var coordinator = Services.GetRequiredService<ICaptureCoordinator>();
        _ = mode == CaptureMode.Region ? coordinator.CaptureRegionAsync() : coordinator.CaptureFullScreenAsync();
    }

    private void OnTrayAction(object? sender, TrayAction action)
    {
        var coordinator = Services.GetRequiredService<ICaptureCoordinator>();
        switch (action)
        {
            case TrayAction.CaptureFullScreen:
                _ = coordinator.CaptureFullScreenAsync();
                break;
            case TrayAction.CaptureRegion:
                _ = coordinator.CaptureRegionAsync();
                break;
            case TrayAction.OpenWindow:
                _windowHost?.ShowMainWindow();
                break;
            case TrayAction.OpenFolder:
                var storage = Services.GetRequiredService<IScreenshotStorage>();
                Services.GetRequiredService<ShellService>().OpenFolder(storage.ResolveDirectory(Settings.Current.SaveDirectory));
                break;
            case TrayAction.Exit:
                RequestExit();
                break;
        }
    }

    private void OnInstanceActivated(object? sender, AppActivationArguments e)
    {
        // A second launch was redirected here: bring the window to front.
        _windowHost?.DispatcherQueue.TryEnqueue(() => _windowHost.ShowMainWindow());
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _logger?.LogCritical(e.Exception, "Unhandled exception: {Message}", e.Message);
        e.Handled = true;
    }
}
