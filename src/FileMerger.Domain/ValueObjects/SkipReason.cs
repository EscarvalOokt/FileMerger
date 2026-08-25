namespace FileMerger.Domain.ValueObjects;

public sealed record SkipReason
{
    public SkipReason(
        string code,
        string description,
        SkipRuleDetails? ruleDetails = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty.", nameof(code));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));

        Code = code;
        Description = description;
        RuleDetails = ruleDetails;
    }

    public string Code { get; }
    public string Description { get; }
    public SkipRuleDetails? RuleDetails { get; }
}