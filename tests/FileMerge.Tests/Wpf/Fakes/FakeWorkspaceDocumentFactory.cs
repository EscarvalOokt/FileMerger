using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeWorkspaceDocumentFactory : IWorkspaceDocumentFactory
{
    private int _createdCount;

    public List<WorkspaceDocumentViewModel> CreatedDocuments { get; } = [];

    public Func<int, WorkspaceDocumentViewModel>? CreateDocument { get; set; }

    public WorkspaceDocumentViewModel CreateDefaultDocument()
    {
        _createdCount++;

        WorkspaceDocumentViewModel document = CreateDocument?.Invoke(_createdCount)
                                              ?? WorkspaceDocumentTestFactory.CreateSavedDocument(
                                                  sessionName: $"Workspace {_createdCount}",
                                                  outputPath: string.Empty);

        CreatedDocuments.Add(document);

        return document;
    }
}