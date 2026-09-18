namespace FileMerger.Wpf.Features.Workspace.Recent;

public sealed record RecentWorkspacesDocumentDto(int SchemaVersion, List<RecentWorkspaceEntryDto>? Entries);