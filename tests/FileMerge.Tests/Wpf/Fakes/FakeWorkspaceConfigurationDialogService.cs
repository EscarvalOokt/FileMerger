using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeWorkspaceConfigurationDialogService : IWorkspaceConfigurationDialogService
{
    public int ShowDialogCalls { get; private set; }

    public WorkspaceDocumentViewModel? LastDocument { get; private set; }

    public bool Result { get; set; }

    public Func<WorkspaceDocumentViewModel, bool>? OnShowDialog { get; set; }

    public Task<bool> ShowDialogAsync(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        ShowDialogCalls++;
        LastDocument = document;

        bool result = OnShowDialog?.Invoke(document) ?? Result;
        return Task.FromResult(result);
    }
}