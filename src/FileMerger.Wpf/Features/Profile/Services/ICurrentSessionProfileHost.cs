using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Features.Profile.Services;

public interface ICurrentSessionProfileHost
{
    string CurrentProfileName { get; }
    string? CurrentProfileEntryId { get; }

    WorkspaceProfileDto CaptureCurrentProfile();

    void ApplyProfileToCurrentSession(string profileName, WorkspaceProfileDto profile, string? profileEntryId);
}