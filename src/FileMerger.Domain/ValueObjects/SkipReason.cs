using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects;

public sealed record SkipReason
{
    public SkipReason(string code, string description, SkipRuleDetails? ruleDetails = null)
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

    public SkippedFileCategory Category =>
        Code switch
        {
            "discovery.file-type-disabled" => SkippedFileCategory.DisabledFileType,
            "discovery.unsupported-file-type" => SkippedFileCategory.UnsupportedFile,
            "filter.rule.exclude" => SkippedFileCategory.ProfileExclusion,
            "manual.exclude" => SkippedFileCategory.ManualExclusion,
            "source.exclude" => SkippedFileCategory.SourceExclusion,
            "file.read.failed" => SkippedFileCategory.ProcessingFailure,
            _ => SkippedFileCategory.Other
        };
}