using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeWorkspaceDocumentOutputService : IWorkspaceDocumentOutputService
{
    public WorkspaceDocumentViewModel? LastSaveOutputDocument { get; private set; }

    public WorkspaceDocumentViewModel? LastBrowseOutputDocument { get; private set; }

    public WorkspaceDocumentViewModel? LastOpenOutputFolderDocument { get; private set; }

    public int SaveOutputCalls { get; private set; }

    public int BrowseOutputCalls { get; private set; }

    public int OpenOutputFolderCalls { get; private set; }

    public bool CanOpenOutputFolderResult { get; set; } = true;

    public Func<WorkspaceDocumentViewModel, Action?, CancellationToken, Task>? OnSaveOutputAsync { get; set; }

    public Task SaveOutputAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        LastSaveOutputDocument = document;
        SaveOutputCalls++;

        return OnSaveOutputAsync?.Invoke(document, stateChanged, cancellationToken) ?? Task.CompletedTask;
    }

    public void BrowseOutputPath(WorkspaceDocumentViewModel document)
    {
        LastBrowseOutputDocument = document;
        BrowseOutputCalls++;
    }

    public bool CanOpenOutputFolder(WorkspaceDocumentViewModel document)
    {
        return CanOpenOutputFolderResult;
    }

    public void OpenOutputFolder(WorkspaceDocumentViewModel document)
    {
        LastOpenOutputFolderDocument = document;
        OpenOutputFolderCalls++;
    }
}