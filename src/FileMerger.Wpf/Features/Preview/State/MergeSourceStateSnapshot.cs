using FileMerger.Domain.Enums;

namespace FileMerger.Wpf.Features.Preview.State;

public sealed record MergeSourceStateSnapshot(
    string Path,
    MergeSourceType Type,
    bool IsRecursive,
    bool IsEnabled,
    IReadOnlyCollection<MergeSourceExclusionStateSnapshot> Exclusions);