using SharpShot.Core.Models;

namespace SharpShot.Core.Interfaces;

/// <summary>Loads and persists <see cref="AppSettings"/>.</summary>
public interface ISettingsService
{
    /// <summary>The settings currently in effect. Treat as read-only; use <see cref="SaveAsync"/> to change.</summary>
    AppSettings Current { get; }

    string SettingsFilePath { get; }

    /// <summary>Raised after settings were saved successfully.</summary>
    event EventHandler<AppSettings>? Changed;

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
