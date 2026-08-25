using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects;

public sealed record SkipRuleDetails
{
    public SkipRuleDetails(
        FilterMode mode,
        FilterTarget target,
        RulePatternType patternType,
        string pattern,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be empty.", nameof(pattern));

        Mode = mode;
        Target = target;
        PatternType = patternType;
        Pattern = pattern;
        Description = description;
    }

    public FilterMode Mode { get; }
    public FilterTarget Target { get; }
    public RulePatternType PatternType { get; }
    public string Pattern { get; }
    public string? Description { get; }
}