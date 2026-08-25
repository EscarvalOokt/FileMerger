using FileMerger.Domain.Profiles;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.State;

namespace FileMerger.Tests.Wpf.TestSupport;

public static class WorkspaceDocumentTestFactory
{
    public const string DefaultSessionName = "Test Session";
    public const string DefaultOutputPath = @"D:\Output\merged.txt";

    public static WorkspaceDocumentFactory CreateFactory(
        FakeApplicationPreferencesStore? applicationPreferencesStore = null)
    {
        return new WorkspaceDocumentFactory(
            new ProfileEditorFactory(new BuiltInFileTypeCatalog()),
            new FakeFolderBrowserService(),
            new FakeOpenFileDialogService(),
            new FakeSourceDetailsDialogService(),
            new FakeClipboardService(),
            applicationPreferencesStore ?? new FakeApplicationPreferencesStore());
    }

    public static WorkspaceDocumentViewModel CreateDefaultDocument()
    {
        return CreateFactory().CreateDefaultDocument();
    }

    public static WorkspaceDocumentViewModel CreateDocument(
        string sessionName = DefaultSessionName,
        string outputPath = DefaultOutputPath)
    {
        WorkspaceDocumentViewModel document = CreateDefaultDocument();

        document.SessionSettings.SessionName = sessionName;
        document.SessionSettings.OutputPath = outputPath;

        return document;
    }

    public static WorkspaceDocumentViewModel CreateSavedDocument(
        string sessionName = DefaultSessionName,
        string outputPath = "")
    {
        WorkspaceDocumentViewModel document = CreateDocument(
            sessionName,
            outputPath);

        MarkWorkspaceSaved(document);

        return document;
    }

    public static void RefreshWorkspaceDirtyState(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.WorkspaceDirtyTracker.Refresh(
            WorkspaceDocumentStateSnapshotFactory.Capture(document));
    }

    public static void MarkWorkspaceSaved(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.WorkspaceDirtyTracker.MarkWorkspaceSaved(
            WorkspaceDocumentStateSnapshotFactory.Capture(document));
    }
}