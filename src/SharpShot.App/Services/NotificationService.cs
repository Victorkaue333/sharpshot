using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using SharpShot.Core.Models;

namespace SharpShot.App.Services;

public interface INotificationService
{
    bool IsAvailable { get; }

    void ShowCaptureSaved(CaptureResult result);

    void ShowCaptureFailed(string message);
}

/// <summary>
/// Toast notifications through the Windows App SDK AppNotificationManager (works unpackaged).
/// Buttons carry arguments that <see cref="ShellService"/> executes when the toast is activated,
/// both while the app runs and when Windows relaunches it with "----AppNotificationActivated:".
/// </summary>
internal sealed class NotificationService : INotificationService, IDisposable
{
    public const string ActionKey = "action";
    public const string PathKey = "path";
    public const string OpenFolderAction = "openFolder";
    public const string OpenFileAction = "openFile";

    private readonly ShellService _shell;
    private readonly ILogger<NotificationService> _logger;
    private readonly string _logoPath;
    private bool _registered;

    public NotificationService(ShellService shell, ILogger<NotificationService> logger)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(logger);
        _shell = shell;
        _logger = logger;
        _logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "NotificationLogo.png");
    }

    public bool IsAvailable => _registered;

    /// <summary>Registers the app for notifications. Safe to call when notifications are unsupported.</summary>
    public void Initialize()
    {
        try
        {
            if (!AppNotificationManager.IsSupported())
            {
                _logger.LogWarning("App notifications are not supported on this system; toasts disabled.");
                return;
            }

            var manager = AppNotificationManager.Default;
            manager.NotificationInvoked += OnNotificationInvoked;
            manager.Register();
            _registered = true;
            _logger.LogInformation("Notification manager registered (setting: {Setting}).", manager.Setting);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification registration failed; toasts disabled.");
            _registered = false;
        }
    }

    public void ShowCaptureSaved(CaptureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!_registered)
        {
            return;
        }

        try
        {
            var builder = new AppNotificationBuilder()
                .AddText("Captura salva com sucesso!")
                .AddText(result.FileName)
                .AddArgument(ActionKey, OpenFileAction)
                .AddArgument(PathKey, result.FilePath)
                .AddButton(new AppNotificationButton("Abrir pasta")
                    .AddArgument(ActionKey, OpenFolderAction)
                    .AddArgument(PathKey, result.FilePath))
                .AddButton(new AppNotificationButton("Ver imagem")
                    .AddArgument(ActionKey, OpenFileAction)
                    .AddArgument(PathKey, result.FilePath));

            if (File.Exists(_logoPath))
            {
                builder.SetAppLogoOverride(new Uri(_logoPath), AppNotificationImageCrop.Default);
            }

            Show(builder.BuildNotification());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to show the capture notification.");
        }
    }

    public void ShowCaptureFailed(string message)
    {
        if (!_registered)
        {
            return;
        }

        try
        {
            var builder = new AppNotificationBuilder()
                .AddText("Não foi possível capturar a tela")
                .AddText(message);
            Show(builder.BuildNotification());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to show the failure notification.");
        }
    }

    /// <summary>Executes the action carried by a toast activation (foreground or launched by the toast).</summary>
    public static void HandleActivation(IDictionary<string, string> arguments, ShellService shell)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(shell);
        if (!arguments.TryGetValue(PathKey, out var path) || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        arguments.TryGetValue(ActionKey, out var action);
        switch (action)
        {
            case OpenFolderAction:
                shell.RevealInExplorer(path);
                break;
            case OpenFileAction:
                shell.OpenFile(path);
                break;
        }
    }

    public static IDictionary<string, string> ParseArguments(string arguments)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in arguments.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            result[Uri.UnescapeDataString(pair[..separator])] = Uri.UnescapeDataString(pair[(separator + 1)..]);
        }

        return result;
    }

    public void Dispose()
    {
        if (!_registered)
        {
            return;
        }

        try
        {
            AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
            AppNotificationManager.Default.Unregister();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Notification manager unregister failed.");
        }

        _registered = false;
    }

    private void Show(AppNotification notification)
    {
        var setting = AppNotificationManager.Default.Setting;
        if (setting != AppNotificationSetting.Enabled)
        {
            _logger.LogInformation("Notifications are disabled ({Setting}); skipping toast.", setting);
            return;
        }

        AppNotificationManager.Default.Show(notification);
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        try
        {
            HandleActivation(args.Arguments, _shell);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification activation failed.");
        }
    }
}
