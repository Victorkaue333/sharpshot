using SharpShot.Core.Models;

namespace SharpShot.Core.Services;

/// <summary>Validates a directory path syntactically (no file-system access).</summary>
public static class PathValidator
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    private static readonly System.Buffers.SearchValues<char> InvalidSegmentChars =
        System.Buffers.SearchValues.Create(['<', '>', ':', '"', '/', '\\', '|', '?', '*']);

    public static PathValidationResult ValidateDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return PathValidationResult.Invalid("O caminho está vazio.");
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return PathValidationResult.Invalid("O caminho contém caracteres inválidos.");
        }

        if (!Path.IsPathFullyQualified(path))
        {
            return PathValidationResult.Invalid("Informe um caminho completo (por exemplo, C:\\Imagens\\SharpShot).");
        }

        var root = Path.GetPathRoot(path) ?? string.Empty;
        var remainder = path[root.Length..];
        foreach (var segment in remainder.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.AsSpan().IndexOfAny(InvalidSegmentChars) >= 0)
            {
                return PathValidationResult.Invalid($"O nome de pasta \"{segment}\" contém caracteres inválidos.");
            }

            var stem = segment.Split('.')[0];
            if (ReservedNames.Contains(stem))
            {
                return PathValidationResult.Invalid($"\"{segment}\" é um nome reservado do Windows.");
            }

            if (segment.EndsWith(' ') || segment.EndsWith('.'))
            {
                return PathValidationResult.Invalid($"O nome de pasta \"{segment}\" não pode terminar com espaço ou ponto.");
            }
        }

        return PathValidationResult.Valid;
    }
}
