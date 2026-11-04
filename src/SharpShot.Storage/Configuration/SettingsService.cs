using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;

namespace SharpShot.Storage.Configuration;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON in <c>%LOCALAPPDATA%\SharpShot\settings.json</c>.
/// Writes are atomic (temp file + replace); a corrupted file is backed up and replaced by defaults.
/// </summary>
public sealed class SettingsService : ISettingsService, IDisposable
{
    public const string ApplicationFolderName = "SharpShot";
    public const string SettingsFileName = "settings.json";

    // Relaxed escaping keeps "Ctrl+Shift+1" readable in the file instead of "Ctrl+Shift+1".
    private static readonly SettingsJsonContext Json = new(new JsonSerializerOptions(SettingsJsonContext.Default.Options)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });

    private readonly ILogger<SettingsService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppSettings _current = AppSettings.CreateDefault();

    public SettingsService(ILogger<SettingsService> logger)
        : this(logger, GetDefaultSettingsPath())
    {
    }

    public SettingsService(ILogger<SettingsService> logger, string settingsFilePath)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);
        _logger = logger;
        SettingsFilePath = settingsFilePath;
    }

    public AppSettings Current => _current;

    public string SettingsFilePath { get; }

    public event EventHandler<AppSettings>? Changed;

    public static string GetApplicationDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
        return Path.Combine(localAppData, ApplicationFolderName);
    }

    public static string GetDefaultSettingsPath() => Path.Combine(GetApplicationDataDirectory(), SettingsFileName);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                _logger.LogInformation("No settings file at {Path}; using defaults.", SettingsFilePath);
                _current = AppSettings.CreateDefault();
                return;
            }

            try
            {
                await using var stream = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var loaded = await JsonSerializer.DeserializeAsync(stream, Json.AppSettings, cancellationToken);
                _current = Normalize(loaded ?? AppSettings.CreateDefault());
                _logger.LogInformation("Settings loaded from {Path}.", SettingsFilePath);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Settings file {Path} is unreadable; restoring defaults.", SettingsFilePath);
                TryBackupCorruptFile();
                _current = AppSettings.CreateDefault();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var snapshot = Normalize(settings.Clone());

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = SettingsFilePath + ".tmp";
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, snapshot, Json.AppSettings, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, SettingsFilePath, overwrite: true);
            _current = snapshot;
            _logger.LogInformation("Settings saved to {Path}.", SettingsFilePath);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke(this, snapshot);
    }

    public void Dispose() => _gate.Dispose();

    private static AppSettings Normalize(AppSettings settings)
    {
        settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
        settings.FullScreenHotkey ??= HotkeyBinding.DefaultFullScreen;
        settings.RegionHotkey ??= HotkeyBinding.DefaultRegion;
        if (!settings.FullScreenHotkey.IsValid)
        {
            settings.FullScreenHotkey = HotkeyBinding.DefaultFullScreen;
        }

        if (!settings.RegionHotkey.IsValid)
        {
            settings.RegionHotkey = HotkeyBinding.DefaultRegion;
        }

        if (string.IsNullOrWhiteSpace(settings.SaveDirectory))
        {
            settings.SaveDirectory = null;
        }

        return settings;
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            var backup = $"{SettingsFilePath}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Move(SettingsFilePath, backup, overwrite: true);
            _logger.LogWarning("Corrupt settings file moved to {Backup}.", backup);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not back up the corrupt settings file.");
        }
    }
}
