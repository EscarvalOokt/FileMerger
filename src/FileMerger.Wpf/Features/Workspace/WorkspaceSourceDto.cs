using FileMerger.Domain.Enums;

namespace FileMerger.Wpf.Features.Workspace;

public sealed record WorkspaceSourceDto(
    string Path,
    MergeSourceType Type,
    bool IsRecursive,
    bool IsEnabled,
    List<WorkspaceSourceExclusionDto>? Exclusions = null);