using FileMerger.Domain.Enums;

namespace FileMerger.Wpf.Features.Workspace;

public sealed record WorkspaceFileFilterRuleDto(
    FilterMode Mode,
    FilterTarget Target,
    RulePatternType PatternType,
    string Pattern,
    bool IsEnabled = true,
    string? Description = null,
    bool IsUserEditable = true);