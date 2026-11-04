using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SharpShot.App.Services;

/// <summary>Opens files and folders with the Windows shell.</summary>
public sealed class ShellService
{
    private readonly ILogger<ShellService> _logger;

    public ShellService(ILogger<ShellService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>Opens Explorer with <paramref name="filePath"/> selected (or the folder itself when the file is gone).</summary>
    public void RevealInExplorer(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = false });
                return;
            }

            var directory = Directory.Exists(filePath) ? filePath : Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                OpenFolder(directory);
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            _logger.LogWarning(ex, "Could not reveal {Path} in Explorer.", filePath);
        }
    }

    public void OpenFolder(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{directory}\"") { UseShellExecute = false });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not open folder {Directory}.", directory);
        }
    }

    public void OpenFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                RevealInExplorer(filePath);
                return;
            }

            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            _logger.LogWarning(ex, "Could not open {Path}.", filePath);
        }
    }

    public void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not open {Url}.", url);
        }
    }
}
