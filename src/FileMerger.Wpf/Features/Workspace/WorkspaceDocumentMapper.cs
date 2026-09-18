using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Sources.ViewModels;

namespace FileMerger.Wpf.Features.Workspace;

public static class WorkspaceDocumentMapper
{
    public static WorkspaceDto Capture(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new WorkspaceDto(
            Document: new WorkspaceDocumentDto(
                SessionName: document.SessionSettings.SessionName,
                OutputPath: document.SessionSettings.OutputPath,
                Sources:
                [
                    .. document.SourcesPane.Sources.Select(ToWorkspaceSourceDto)
                ],
                Profile: document.ProfileEditor.CaptureProfile(),
                InclusionOverrides: document.FilesPane.CaptureOverridesDictionary(),
                ProfileDisplayName: document.CurrentProfileName,
                ProfileEntryId: document.CurrentProfileEntryId,
                ProfileOriginEntryId: document.ProfileOriginEntryId,
                ProfileOriginDisplayName: document.ProfileOriginDisplayName));
    }

    public static void Apply(WorkspaceDocumentViewModel document, WorkspaceDto workspace)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(workspace);

        if (workspace.Document is null)
            throw new InvalidOperationException("Workspace document is missing.");

        WorkspaceDocumentDto dto = workspace.Document;

        document.SessionSettings.SessionName = dto.SessionName;
        document.SessionSettings.OutputPath = dto.OutputPath;

        document.ProfileEditor.ApplyProfile(dto.Profile);

        string profileDisplayName = string.IsNullOrWhiteSpace(dto.ProfileDisplayName)
            ? "Default"
            : dto.ProfileDisplayName.Trim();

        string? profileEntryId = NormalizeOptional(dto.ProfileEntryId);
        string? profileOriginEntryId = NormalizeOptional(dto.ProfileOriginEntryId);
        string? profileOriginDisplayName = NormalizeOptional(dto.ProfileOriginDisplayName);

        if (profileEntryId is not null)
        {
            profileOriginEntryId ??= profileEntryId;
            profileOriginDisplayName ??= profileDisplayName;
        }

        document.ProfileEditor.WorkingProfileName = profileDisplayName;
        document.CurrentProfileEntryId = profileEntryId;
        document.ProfileOriginEntryId = profileOriginEntryId;
        document.ProfileOriginDisplayName = profileOriginDisplayName;

        document.SourcesPane.LoadSources(
        [
            .. dto.Sources.Select(ToMergeSource)
        ]);

        document.FilesPane.ApplyOverridesDictionary(dto.InclusionOverrides);
        document.FilesPane.LoadFiles([]);
        document.FilesPane.ResetSelections();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static WorkspaceSourceDto ToWorkspaceSourceDto(MergeSourceItemViewModel source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<WorkspaceSourceExclusionDto> exclusions = source.Type == MergeSourceType.Directory
            ?
            [
                .. source.Exclusions.Select(ToWorkspaceSourceExclusionDto)
            ]
            : [];

        return new WorkspaceSourceDto(
            Path: source.Path,
            Type: source.Type,
            IsRecursive: source.IsRecursive,
            IsEnabled: source.IsEnabled,
            Exclusions: exclusions);
    }

    private static WorkspaceSourceExclusionDto ToWorkspaceSourceExclusionDto(
        MergeSourceExclusionItemViewModel exclusion)
    {
        ArgumentNullException.ThrowIfNull(exclusion);

        return new WorkspaceSourceExclusionDto(
            RelativePath: exclusion.RelativePath,
            Type: exclusion.Type,
            IsEnabled: exclusion.IsEnabled);
    }

    private static MergeSource ToMergeSource(WorkspaceSourceDto source)
    {
        ArgumentNullException.ThrowIfNull(source);

        IReadOnlyCollection<MergeSourceExclusion> exclusions = source.Type == MergeSourceType.Directory
            ?
            [
                .. (source.Exclusions ?? []).Select(ToMergeSourceExclusion)
            ]
            : [];

        return new MergeSource(
            path: source.Path,
            type: source.Type,
            isRecursive: source.IsRecursive,
            isEnabled: source.IsEnabled,
            exclusions: exclusions);
    }

    private static MergeSourceExclusion ToMergeSourceExclusion(WorkspaceSourceExclusionDto exclusion)
    {
        ArgumentNullException.ThrowIfNull(exclusion);

        return new MergeSourceExclusion(
            relativePath: exclusion.RelativePath,
            type: exclusion.Type,
            isEnabled: exclusion.IsEnabled);
    }
}