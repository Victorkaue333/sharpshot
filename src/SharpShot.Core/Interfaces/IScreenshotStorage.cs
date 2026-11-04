using SharpShot.Core.Models;

namespace SharpShot.Core.Interfaces;

/// <summary>Writes encoded screenshots to the user's chosen folder.</summary>
public interface IScreenshotStorage
{
    /// <summary>"Pictures\SharpShot" resolved through the user's real (possibly redirected) Pictures folder.</summary>
    string DefaultDirectory { get; }

    /// <summary>Resolves the configured directory (null/empty → default), expanding environment variables.</summary>
    string ResolveDirectory(string? configuredDirectory);

    PathValidationResult ValidateDirectory(string? directory);

    /// <summary>
    /// Writes <paramref name="data"/> to <paramref name="directory"/> using <paramref name="preferredFileName"/>,
    /// creating the directory if needed and never overwriting an existing file.
    /// </summary>
    Task<SavedScreenshot> SaveAsync(byte[] data, string directory, string preferredFileName, CancellationToken cancellationToken = default);
}
