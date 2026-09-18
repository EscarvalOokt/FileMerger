using FileMerger.Domain.Enums;

namespace FileMerger.Wpf.Features.Workspace;

public sealed record WorkspaceSourceExclusionDto(string RelativePath, MergeSourceExclusionType Type, bool IsEnabled);