using FileMerger.Domain.ValueObjects;

namespace FileMerger.Wpf.Features.Workspace.Configuration;

public sealed record WorkspaceConfigurationSnapshot(
    string SessionName,
    string OutputPath,
    IReadOnlyCollection<MergeSource> Sources,
    WorkspaceProfileDto Profile,
    string ProfileDisplayName,
    string? ProfileEntryId,
    string? ProfileOriginEntryId,
    string? ProfileOriginDisplayName);