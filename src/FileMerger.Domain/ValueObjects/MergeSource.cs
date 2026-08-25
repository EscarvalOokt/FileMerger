using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects
{
    public sealed record MergeSource
    {
        public MergeSource(
            string path,
            MergeSourceType type,
            bool isRecursive = true,
            bool isEnabled = true,
            IReadOnlyCollection<MergeSourceExclusion>? exclusions = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Source path cannot be empty.", nameof(path));

            Path = path;
            Type = type;
            IsRecursive = type == MergeSourceType.Directory && isRecursive;
            IsEnabled = isEnabled;
            Exclusions = exclusions ?? [];
        }

        public string Path { get; }
        public MergeSourceType Type { get; }
        public bool IsRecursive { get; }
        public bool IsEnabled { get; }
        public IReadOnlyCollection<MergeSourceExclusion> Exclusions { get; }
    }
}