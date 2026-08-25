using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Features.Profile.Models;

public sealed record ProfileLibraryEntry(
    ProfileMetadataDto Metadata,
    WorkspaceProfileDto Profile,
    string? FilePath,
    ProfileEntryKind Kind)
{
    public string Id => Metadata.Id;
    public string DisplayName => Metadata.Name;
    public string Description => Metadata.Description ?? string.Empty;
    public DateTime? CreatedAtUtc => Metadata.CreatedAtUtc;
    public DateTime? UpdatedAtUtc => Metadata.UpdatedAtUtc;

    public bool IsBuiltIn => Kind == ProfileEntryKind.BuiltIn || Metadata.IsBuiltIn;
    public bool IsReadOnly => IsBuiltIn || Metadata.IsReadOnly;
    public bool IsUserDefined => Kind == ProfileEntryKind.User;
}