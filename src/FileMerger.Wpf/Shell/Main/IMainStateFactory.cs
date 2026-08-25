using FileMerger.Domain.Entities;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Shell.Main;

public interface IMainStateFactory
{
    MergeSession BuildSession(WorkspaceDocumentViewModel document);

    PreviewStateSnapshot BuildPreviewState(WorkspaceDocumentViewModel document);

    string? BuildInitialOutputPath(WorkspaceDocumentViewModel document);
}