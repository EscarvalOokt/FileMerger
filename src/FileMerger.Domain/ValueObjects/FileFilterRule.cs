using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects
{
    public sealed record FileFilterRule
    {
        public FileFilterRule(
            FilterMode mode,
            FilterTarget target,
            RulePatternType patternType,
            string pattern,
            bool isEnabled = true,
            string? description = null)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be empty.", nameof(pattern));

            Mode = mode;
            Target = target;
            PatternType = patternType;
            Pattern = pattern;
            IsEnabled = isEnabled;
            Description = description;
        }

        public FilterMode Mode { get; }
        public FilterTarget Target { get; }
        public RulePatternType PatternType { get; }
        public string Pattern { get; }
        public bool IsEnabled { get; }
        public string? Description { get; }
    }
}