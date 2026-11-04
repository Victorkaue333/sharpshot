using System.Globalization;

namespace SharpShot.Core.Services;

/// <summary>Builds screenshot file names: <c>SharpShot_YYYY-MM-DD_HH-mm-ss-fff.png</c>.</summary>
public static class FileNaming
{
    public const string Prefix = "SharpShot";
    public const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss-fff";
    public const int MaxUniqueAttempts = 1000;

    /// <summary>Returns a file name for the given local timestamp and extension (".png").</summary>
    public static string CreateFileName(DateTimeOffset timestamp, string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        return $"{Prefix}_{timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture)}{ext}";
    }

    /// <summary>
    /// Returns <paramref name="fileName"/> unchanged when no file exists at that path, otherwise appends
    /// "_1", "_2", ... before the extension until a free name is found.
    /// </summary>
    public static string MakeUnique(string directory, string fileName, Func<string, bool> fileExists)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(fileExists);

        if (!fileExists(Path.Combine(directory, fileName)))
        {
            return fileName;
        }

        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (var i = 1; i <= MaxUniqueAttempts; i++)
        {
            var candidate = $"{stem}_{i}{ext}";
            if (!fileExists(Path.Combine(directory, candidate)))
            {
                return candidate;
            }
        }

        throw new IOException($"Could not find a free file name for '{fileName}' in '{directory}'.");
    }
}
