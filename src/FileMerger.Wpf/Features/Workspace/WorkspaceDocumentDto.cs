namespace FileMerger.Wpf.Features.Workspace;

public sealed record WorkspaceDocumentDto(
    string SessionName,
    string OutputPath,
    List<WorkspaceSourceDto> Sources,
    WorkspaceProfileDto Profile,
    Dictionary<string, bool> InclusionOverrides,
    string? ProfileDisplayName = null,
    string? ProfileEntryId = null,
    string? ProfileOriginEntryId = null,
    string? ProfileOriginDisplayName = null);