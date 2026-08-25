using FileMerger.Domain.ValueObjects;

namespace FileMerger.Domain.Entities
{
    public sealed record MergeOutput
    {
        public MergeOutput(
            string content,
            IReadOnlyCollection<MergeSection> sections,
            MergeStatistics statistics,
            DateTime generatedAtUtc,
            OutputTarget? outputTarget = null)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(sections);
            ArgumentNullException.ThrowIfNull(statistics);

            Content = content;
            Sections = sections;
            Statistics = statistics;
            GeneratedAtUtc = generatedAtUtc;
            OutputTarget = outputTarget;
        }

        public string Content { get; }
        public IReadOnlyCollection<MergeSection> Sections { get; }
        public MergeStatistics Statistics { get; }
        public DateTime GeneratedAtUtc { get; }
        public OutputTarget? OutputTarget { get; }
    }
}