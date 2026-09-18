using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Infrastructure.Common;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Shell.Main;

public sealed class MainStateFactory : IMainStateFactory
{
    public MergeSession BuildSession(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new MergeSession(
            id: Guid.NewGuid(),
            name: document.SessionSettings.SessionName,
            sources: document.SourcesPane.BuildSources(),
            profile: document.ProfileEditor.BuildProfile(),
            outputTarget: document.SessionSettings.BuildOutputTarget());
    }

    public PreviewStateSnapshot BuildPreviewState(WorkspaceDocumentViewModel document)
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

        PreviewSessionStateSnapshot session = document.SessionSettings.BuildPreviewSessionSnapshot();

        PreviewProfileStateSnapshot profile = document.ProfileEditor.BuildPreviewProfileSnapshot();

        var overrides = document.FilesPane.BuildOverrides()
            .GroupBy(x => PathUtility.NormalizeForComparison(x.FullPath), StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().IsIncluded, StringComparer.OrdinalIgnoreCase);

        return new PreviewStateSnapshot(sources, session, profile, overrides);
    }

    public string? BuildInitialOutputPath(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!string.IsNullOrWhiteSpace(document.SessionSettings.OutputPath))
            return document.SessionSettings.OutputPath;

        string? firstDirectorySource = document.SourcesPane.Sources
            .Where(x => x.Type == MergeSourceType.Directory && !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path)
            .FirstOrDefault(Directory.Exists);

        if (!string.IsNullOrWhiteSpace(firstDirectorySource))
            return Path.Combine(firstDirectorySource, "MergedOutput.txt");

        string? firstFileSource = document.SourcesPane.Sources
            .Where(x => x.Type == MergeSourceType.File && !string.IsNullOrWhiteSpace(x.Path))
            .Select(x => x.Path)
            .FirstOrDefault(File.Exists);

        if (!string.IsNullOrWhiteSpace(firstFileSource))
            return Path.Combine(Path.GetDirectoryName(firstFileSource)!, "MergedOutput.txt");

        return null;
    }
}