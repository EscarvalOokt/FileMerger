using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentDirtyStateServiceTests
{
    [Fact]
    public void MarkPreviewApplied_Should_Clear_Dirty_State_For_Current_Document()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        service.MarkPreviewApplied(document);

        Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);
        Assert.True(document.PreviewDirtyTracker.HasAppliedPreview);
    }

    [Fact]
    public void RefreshPreviewDirtyState_Should_Update_Document_Tracker()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        service.MarkPreviewApplied(document);

        document.SessionSettings.SessionName = "Changed Session";

        service.RefreshPreviewDirtyState(document);

        Assert.True(document.PreviewDirtyTracker.IsPreviewDirty);
    }

    [Fact]
    public void RefreshPreviewDirtyState_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.RefreshPreviewDirtyState(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void MarkPreviewApplied_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.MarkPreviewApplied(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void MarkWorkspaceSaved_Should_Clear_Workspace_Dirty_State()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        document.SessionSettings.OutputPath = @"D:\Output\changed.txt";
        service.RefreshWorkspaceDirtyState(document);

        Assert.True(document.WorkspaceDirtyTracker.IsWorkspaceDirty);

        service.MarkWorkspaceSaved(document);

        Assert.False(document.WorkspaceDirtyTracker.IsWorkspaceDirty);
        Assert.True(document.WorkspaceDirtyTracker.HasSavedState);
    }

    [Fact]
    public void RefreshWorkspaceDirtyState_Should_Update_Document_Tracker()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        service.MarkWorkspaceSaved(document);

        document.SessionSettings.SessionName = "Changed Session";

        service.RefreshWorkspaceDirtyState(document);

        Assert.True(document.WorkspaceDirtyTracker.IsWorkspaceDirty);
    }

    [Fact]
    public void RefreshWorkspaceDirtyState_Should_Update_When_Profile_DisplayName_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        document.ProfileEditor.WorkingProfileName = "Saved Profile";
        service.MarkWorkspaceSaved(document);

        document.ProfileEditor.WorkingProfileName = "Changed Profile";

        service.RefreshWorkspaceDirtyState(document);

        Assert.True(document.WorkspaceDirtyTracker.IsWorkspaceDirty);
    }

    [Fact]
    public void RefreshWorkspaceDirtyState_Should_Update_When_Profile_EntryId_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        document.CurrentProfileEntryId = "profile-before";
        service.MarkWorkspaceSaved(document);

        document.CurrentProfileEntryId = "profile-after";

        service.RefreshWorkspaceDirtyState(document);

        Assert.True(document.WorkspaceDirtyTracker.IsWorkspaceDirty);
    }

    [Fact]
    public void RefreshWorkspaceDirtyState_Should_Update_When_Profile_OriginEntryId_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        document.ProfileOriginEntryId = "origin-before";
        service.MarkWorkspaceSaved(document);

        document.ProfileOriginEntryId = "origin-after";

        service.RefreshWorkspaceDirtyState(document);

        Assert.True(document.WorkspaceDirtyTracker.IsWorkspaceDirty);
    }

    [Fact]
    public void RefreshWorkspaceDirtyState_Should_Update_When_Profile_OriginDisplayName_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        document.ProfileOriginDisplayName = "Origin Before";
        service.MarkWorkspaceSaved(document);

        document.ProfileOriginDisplayName = "Origin After";

        service.RefreshWorkspaceDirtyState(document);

        Assert.True(document.WorkspaceDirtyTracker.IsWorkspaceDirty);
    }

    [Fact]
    public void RefreshPreviewDirtyState_Should_Ignore_Profile_Provenance_Changes()
    {
        WorkspaceDocumentViewModel document = CreateDocument();
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        service.MarkPreviewApplied(document);

        document.ProfileOriginEntryId = "origin-profile";
        document.ProfileOriginDisplayName = "Origin Profile";

        service.RefreshPreviewDirtyState(document);

        Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);
    }

    [Fact]
    public void WorkspaceDirtyState_Should_Be_Independent_Per_Document()
    {
        WorkspaceDocumentViewModel first = CreateDocument();
        WorkspaceDocumentViewModel second = CreateDocument();

        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        service.MarkWorkspaceSaved(first);
        service.MarkWorkspaceSaved(second);

        second.SessionSettings.SessionName = "Changed Session";
        service.RefreshWorkspaceDirtyState(second);

        Assert.False(first.WorkspaceDirtyTracker.IsWorkspaceDirty);
        Assert.True(second.WorkspaceDirtyTracker.IsWorkspaceDirty);
    }

    [Fact]
    public void RefreshWorkspaceDirtyState_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.RefreshWorkspaceDirtyState(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void MarkWorkspaceSaved_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentDirtyStateService service = new(new MainStateFactory());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.MarkWorkspaceSaved(null!));

        Assert.Equal("document", ex.ParamName);
    }

    private static WorkspaceDocumentViewModel CreateDocument()
    {
        return WorkspaceDocumentTestFactory.CreateDocument();
    }
}