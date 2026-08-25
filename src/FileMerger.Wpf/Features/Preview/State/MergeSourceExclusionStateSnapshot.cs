using FileMerger.Domain.Enums;

namespace FileMerger.Wpf.Features.Preview.State;

public sealed record MergeSourceExclusionStateSnapshot(
    string RelativePath,
    MergeSourceExclusionType Type,
    bool IsEnabled);