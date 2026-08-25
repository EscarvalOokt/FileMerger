using FileMerger.Infrastructure.Common;
using FileMerger.Wpf.Features.Preview.State;

namespace FileMerger.Wpf.Features.Workspace.State;

public static class WorkspaceDocumentStateSnapshotFactory
{
    public static WorkspaceDocumentStateSnapshot Capture(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        IReadOnlyCollection<MergeSourceStateSnapshot> sources =
        [
            .. document.SourcesPane.BuildStateSnapshots()
                .Select(x => x with { Path = PathUtility.NormalizeForComparison(x.Path) })
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.IsRecursive)
                .ThenBy(x => x.IsEnabled)
        ];

        PreviewSessionStateSnapshot session =
            document.SessionSettings.BuildPreviewSessionSnapshot();

        PreviewProfileStateSnapshot profile =
            document.ProfileEditor.BuildPreviewProfileSnapshot();

        var overrides = document.FilesPane.BuildOverrides()
            .GroupBy(x => PathUtility.NormalizeForComparison(x.FullPath), StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().IsIncluded, StringComparer.OrdinalIgnoreCase);

        return new WorkspaceDocumentStateSnapshot(
            ProfileDisplayName: document.CurrentProfileName,
            ProfileEntryId: document.CurrentProfileEntryId,
            ProfileOriginEntryId: document.ProfileOriginEntryId,
            ProfileOriginDisplayName: document.ProfileOriginDisplayName,
            PreviewState: new PreviewStateSnapshot(
                sources,
                session,
                profile,
                overrides));
    }
}