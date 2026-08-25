using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects
{
    public sealed record MergeSourceExclusion
    {
        public MergeSourceExclusion(
            string relativePath,
            MergeSourceExclusionType type,
            bool isEnabled = true)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path cannot be empty.", nameof(relativePath));

            string trimmedPath = relativePath.Trim();

            if (Path.IsPathRooted(trimmedPath))
                throw new ArgumentException("Relative path cannot be rooted.", nameof(relativePath));

            string normalizedRelativePath = NormalizeRelativePath(trimmedPath);

            if (string.IsNullOrWhiteSpace(normalizedRelativePath))
                throw new ArgumentException("Relative path cannot be empty.", nameof(relativePath));

            if (ContainsParentTraversal(normalizedRelativePath))
                throw new ArgumentException("Relative path cannot contain parent traversal segments.", nameof(relativePath));

            RelativePath = normalizedRelativePath;
            Type = type;
            IsEnabled = isEnabled;
        }

        public string RelativePath { get; }
        public MergeSourceExclusionType Type { get; }
        public bool IsEnabled { get; }

        private static string NormalizeRelativePath(string path)
        {
            return path
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                .Trim(Path.DirectorySeparatorChar);
        }

        private static bool ContainsParentTraversal(string path)
        {
            string[] segments = path.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries);

            return segments.Any(x => x == "..");
        }
    }
}