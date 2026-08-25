using FileMerger.Wpf.Features.Preview.State;

namespace FileMerger.Wpf.Features.Workspace.State;

public sealed record WorkspaceDocumentStateSnapshot(
    string ProfileDisplayName,
    string? ProfileEntryId,
    string? ProfileOriginEntryId,
    string? ProfileOriginDisplayName,
    PreviewStateSnapshot PreviewState);