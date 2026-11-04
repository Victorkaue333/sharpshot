using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SharpShot.App.Services;

public interface IStartupService
{
    bool IsEnabled();

    /// <summary>Enables or disables launch at sign-in. Returns false when the registry could not be written.</summary>
    bool SetEnabled(bool enabled);
}

/// <summary>
/// "Start with Windows" for the unpackaged app: a value under HKCU\...\CurrentVersion\Run pointing at
/// SharpShot.exe with <c>--minimized</c>, so the app starts in the tray. (A packaged build would use a StartupTask.)
/// </summary>
internal sealed class StartupService : IStartupService
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "SharpShot";
    public const string MinimizedArgument = "--minimized";

    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public static string ExecutablePath => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "SharpShot.exe");

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(ValueName) as string;
            return !string.IsNullOrWhiteSpace(value)
                   && value.Contains(ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not read the Run registry key.");
            return false;
        }
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return false;
            }

            if (enabled)
            {
                key.SetValue(ValueName, $"\"{ExecutablePath}\" {MinimizedArgument}", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            _logger.LogInformation("Start with Windows {State}.", enabled ? "enabled" : "disabled");
            return true;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not update the Run registry key.");
            return false;
        }
    }
}
