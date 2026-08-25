namespace FileMerger.Infrastructure.Common;

public static class PathUtility
{
    public static string? TryNormalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            string normalized = Path.GetFullPath(path.Trim());
            return TrimTrailingSeparators(normalized);
        }
        catch
        {
            return null;
        }
    }

    public static string NormalizeForComparison(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        string? normalized = TryNormalize(path);
        return normalized ?? path.Trim();
    }

    public static bool PathEquals(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        string normalizedLeft = NormalizeForComparison(left);
        string normalizedRight = NormalizeForComparison(right);

        return string.Equals(
            normalizedLeft,
            normalizedRight,
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool PathsOverlap(string left, string right)
    {
        string leftWithSeparator = EnsureTrailingSeparator(NormalizeForComparison(left));
        string rightWithSeparator = EnsureTrailingSeparator(NormalizeForComparison(right));

        return leftWithSeparator.StartsWith(rightWithSeparator, StringComparison.OrdinalIgnoreCase) ||
               rightWithSeparator.StartsWith(leftWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPathInsideDirectory(string candidatePath, string directoryPath)
    {
        string normalizedCandidate = NormalizeForComparison(candidatePath);
        string normalizedDirectory = EnsureTrailingSeparator(NormalizeForComparison(directoryPath));

        return normalizedCandidate.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
    }

    public static string EnsureTrailingSeparator(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        return path.EndsWith(Path.DirectorySeparatorChar) ||
               path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static string TrimTrailingSeparators(string path)
    {
        string root = Path.GetPathRoot(path) ?? string.Empty;

        while (path.Length > root.Length &&
               (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)))
        {
            path = path[..^1];
        }

        return path;
    }
}