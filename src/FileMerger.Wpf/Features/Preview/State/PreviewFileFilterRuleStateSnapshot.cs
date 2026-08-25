using FileMerger.Domain.Enums;

namespace FileMerger.Wpf.Features.Preview.State;

public sealed record PreviewFileFilterRuleStateSnapshot(
    FilterMode Mode,
    FilterTarget Target,
    RulePatternType PatternType,
    string Pattern,
    bool IsEnabled,
    string? Description);