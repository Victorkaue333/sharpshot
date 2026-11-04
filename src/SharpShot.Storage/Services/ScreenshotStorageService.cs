using Microsoft.Extensions.Logging;
using SharpShot.Core.Enums;
using SharpShot.Core.Exceptions;
using SharpShot.Core.Interfaces;
using SharpShot.Core.Models;
using SharpShot.Core.Services;

namespace SharpShot.Storage.Services;

/// <summary>
/// Writes screenshots to disk. Never overwrites: name collisions get a numeric suffix.
/// I/O failures are classified into <see cref="SaveErrorKind"/> so the UI can explain them.
/// </summary>
public sealed class ScreenshotStorageService : IScreenshotStorage
{
    public const string DefaultSubfolderName = "SharpShot";

    private const int ErrorSharingViolation = unchecked((int)0x80070020);
    private const int ErrorLockViolation = unchecked((int)0x80070021);
    private const int ErrorHandleDiskFull = unchecked((int)0x80070027);
    private const int ErrorDiskFull = unchecked((int)0x80070070);
    private const int ErrorFileExists = unchecked((int)0x80070050);
    private const int ErrorAlreadyExists = unchecked((int)0x800700B7);
    private const int MaxLockedRetries = 3;

    private readonly ILogger<ScreenshotStorageService> _logger;

    public ScreenshotStorageService(ILogger<ScreenshotStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        DefaultDirectory = ComputeDefaultDirectory();
    }

    public string DefaultDirectory { get; }

    public string ResolveDirectory(string? configuredDirectory)
    {
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return DefaultDirectory;
        }

        var expanded = Environment.ExpandEnvironmentVariables(configuredDirectory.Trim());
        try
        {
            return Path.GetFullPath(expanded);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            _logger.LogWarning(ex, "Configured directory '{Directory}' is invalid; falling back to default.", configuredDirectory);
            return DefaultDirectory;
        }
    }

    public PathValidationResult ValidateDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return PathValidationResult.Valid; // empty means "use default"
        }

        return PathValidator.ValidateDirectory(Environment.ExpandEnvironmentVariables(directory.Trim()));
    }

    public async Task<SavedScreenshot> SaveAsync(byte[] data, string directory, string preferredFileName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredFileName);

        var validation = PathValidator.ValidateDirectory(directory);
        if (!validation.IsValid)
        {
            throw new ScreenshotSaveException(SaveErrorKind.InvalidPath, validation.Error ?? "Invalid directory.");
        }

        EnsureDirectory(directory);

        var fileName = preferredFileName;
        for (var attempt = 0; ; attempt++)
        {
            fileName = FileNaming.MakeUnique(directory, fileName, File.Exists);
            var fullPath = Path.Combine(directory, fileName);
            var created = false; // only a file we created ourselves may be removed on failure
            try
            {
                await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
                {
                    created = true;
                    await stream.WriteAsync(data, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                _logger.LogInformation("Screenshot saved: {Path} ({Bytes} bytes).", fullPath, data.LongLength);
                return new SavedScreenshot(fullPath, data.LongLength);
            }
            catch (IOException ex) when (!created && IsCollision(ex) && attempt < MaxLockedRetries)
            {
                _logger.LogWarning(ex, "'{Path}' already exists or is locked; retrying with a new name.", fullPath);
                fileName = Path.GetFileNameWithoutExtension(fileName) + "_x" + Path.GetExtension(fileName);
                continue;
            }
            catch (OperationCanceledException)
            {
                if (created)
                {
                    TryDeletePartialFile(fullPath);
                }

                throw;
            }
            catch (Exception ex)
            {
                if (created)
                {
                    TryDeletePartialFile(fullPath);
                }

                throw Classify(ex, fullPath);
            }
        }
    }

    private static string ComputeDefaultDirectory()
    {
        // GetFolderPath honours Known Folder redirection (e.g. OneDrive "Pictures").
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrWhiteSpace(pictures))
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
            pictures = Path.Combine(profile, "Pictures");
        }

        return Path.Combine(pictures, DefaultSubfolderName);
    }

    private static void EnsureDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception ex)
        {
            throw Classify(ex, directory);
        }
    }

    private static bool IsCollision(IOException ex)
        => ex.HResult is ErrorSharingViolation or ErrorLockViolation or ErrorFileExists or ErrorAlreadyExists;

    private static ScreenshotSaveException Classify(Exception ex, string path)
    {
        if (ex is ScreenshotSaveException already)
        {
            return already;
        }

        var kind = ex switch
        {
            UnauthorizedAccessException => SaveErrorKind.PermissionDenied,
            DirectoryNotFoundException => SaveErrorKind.DirectoryNotFound,
            PathTooLongException or NotSupportedException or ArgumentException => SaveErrorKind.InvalidPath,
            IOException io when io.HResult is ErrorDiskFull or ErrorHandleDiskFull => SaveErrorKind.DiskFull,
            IOException io when io.HResult is ErrorSharingViolation or ErrorLockViolation => SaveErrorKind.FileLocked,
            _ => SaveErrorKind.Unknown,
        };

        return new ScreenshotSaveException(kind, $"Could not save '{path}': {ex.Message}", ex);
    }

    private void TryDeletePartialFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Could not remove partial file {Path}.", path);
        }
    }
}
