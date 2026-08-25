namespace FileMerger.Domain.Entities
{
    public sealed record MergeStatistics
    {
        public MergeStatistics(
            int filesScanned,
            int filesIncluded,
            int filesSkipped,
            int totalCharacters,
            TimeSpan duration)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(filesScanned);
            ArgumentOutOfRangeException.ThrowIfNegative(filesIncluded);
            ArgumentOutOfRangeException.ThrowIfNegative(filesSkipped);
            ArgumentOutOfRangeException.ThrowIfNegative(totalCharacters);

            FilesScanned = filesScanned;
            FilesIncluded = filesIncluded;
            FilesSkipped = filesSkipped;
            TotalCharacters = totalCharacters;
            Duration = duration;
        }

        public int FilesScanned { get; }
        public int FilesIncluded { get; }
        public int FilesSkipped { get; }
        public int TotalCharacters { get; }
        public TimeSpan Duration { get; }
    }
}