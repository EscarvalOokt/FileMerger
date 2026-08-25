namespace FileMerger.Wpf.Features.Workspace.Configuration;

public static class WorkspaceConfigurationMapper
{
    public static WorkspaceConfigurationSnapshot Capture(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new WorkspaceConfigurationSnapshot(
            SessionName: document.SessionSettings.SessionName,
            OutputPath: document.SessionSettings.OutputPath,
            Sources: [.. document.SourcesPane.BuildSources()],
            Profile: document.ProfileEditor.CaptureProfile(),
            ProfileDisplayName: document.CurrentProfileName,
            ProfileEntryId: document.CurrentProfileEntryId,
            ProfileOriginEntryId: document.ProfileOriginEntryId,
            ProfileOriginDisplayName: document.ProfileOriginDisplayName);
    }

    public static void Apply(
        WorkspaceDocumentViewModel document,
        WorkspaceConfigurationSnapshot configuration)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(configuration);

        document.SessionSettings.SessionName = configuration.SessionName;
        document.SessionSettings.OutputPath = configuration.OutputPath;

        document.ProfileEditor.ApplyProfile(configuration.Profile);
        document.ProfileEditor.WorkingProfileName = configuration.ProfileDisplayName;
        document.CurrentProfileEntryId = configuration.ProfileEntryId;
        document.ProfileOriginEntryId = configuration.ProfileOriginEntryId;
        document.ProfileOriginDisplayName = configuration.ProfileOriginDisplayName;

        document.SourcesPane.LoadSources(configuration.Sources);
    }
}