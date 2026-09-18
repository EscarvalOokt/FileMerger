namespace FileMerger.Updater;

public static class UpdaterPathUtility
{
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    public static bool IsPathInsideDirectory(string candidatePath, string directoryPath)
    {
        string candidate = Normalize(candidatePath);
        string directory = EnsureTrailingSeparator(Normalize(directoryPath));

        return candidate.StartsWith(directory, StringComparison.OrdinalIgnoreCase);
    }

    public static bool PathsOverlap(string left, string right)
    {
        string leftDirectory = EnsureTrailingSeparator(Normalize(left));
        string rightDirectory = EnsureTrailingSeparator(Normalize(right));

        return leftDirectory.StartsWith(rightDirectory, StringComparison.OrdinalIgnoreCase) ||
               rightDirectory.StartsWith(leftDirectory, StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveSafeRelativePath(string directoryPath, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        string normalizedRelative = relativePath.Replace('\\', '/');
        if (normalizedRelative.StartsWith('/') || normalizedRelative.Contains(':'))
            throw new InvalidDataException($"Unsafe relative path '{relativePath}'.");

        string[] segments = normalizedRelative.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw new InvalidDataException($"Unsafe relative path '{relativePath}'.");

        string combined = Path.GetFullPath(
            Path.Combine(directoryPath, normalizedRelative.Replace('/', Path.DirectorySeparatorChar)));

        if (!IsPathInsideDirectory(combined, directoryPath))
            throw new InvalidDataException($"Relative path '{relativePath}' leaves its root directory.");

        return combined;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}