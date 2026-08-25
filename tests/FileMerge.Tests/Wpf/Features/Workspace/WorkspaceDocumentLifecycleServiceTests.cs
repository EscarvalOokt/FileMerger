using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Recent;
using FileMerger.Wpf.Shared.Status;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentLifecycleServiceTests
{
    [Fact]
    public async Task SaveWorkspaceAsync_Should_Set_Busy_Progress_And_Success_Status()
    {
        FakeWorkspaceCoordinator coordinator = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        int stateChanges = 0;

        bool result = await service.SaveWorkspaceAsync(document, () => stateChanges++);

        Assert.True(result);
        Assert.True(coordinator.SaveWasCalled);
        Assert.Same(document, dirtyStateService.LastSavedDocument);

        Assert.False(document.OperationStatus.IsBusy);
        Assert.False(document.OperationStatus.IsProgressVisible);
        Assert.Equal("Workspace saved.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Success, document.OperationStatus.StatusSeverity);
        Assert.True(stateChanges >= 2);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Report_Canceled_Status_When_Coordinator_Returns_False()
    {
        FakeWorkspaceCoordinator coordinator = new()
        {
            SaveResult = false
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.SaveWorkspaceAsync(document);

        Assert.False(result);
        Assert.True(coordinator.SaveWasCalled);
        Assert.Null(dirtyStateService.LastSavedDocument);

        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Workspace save canceled.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Warning, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Reset_Runtime_State_Without_Clearing_Profile_Metadata()
    {
        FakeWorkspaceCoordinator coordinator = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        document.ProfileEditor.WorkingProfileName = "Loaded Profile";
        document.CurrentProfileEntryId = "profile-id";
        document.SetLastOutput(CreateOutput());
        document.PreviewContent = "preview";
        document.PreviewNotice = "notice";
        document.SetPreviewCharacterCount(100);

        bool result = await service.LoadWorkspaceAsync(document);

        Assert.True(result);
        Assert.True(coordinator.LoadWasCalled);
        Assert.Same(document, dirtyStateService.LastSavedDocument);

        Assert.Equal("Loaded Profile", document.CurrentProfileName);
        Assert.Equal("profile-id", document.CurrentProfileEntryId);
        Assert.Null(document.LastOutput);
        Assert.Equal(string.Empty, document.PreviewContent);
        Assert.Equal(string.Empty, document.PreviewNotice);
        Assert.Equal("Characters: 0", document.PreviewCharacterCountText);

        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Workspace loaded.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Success, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Report_Canceled_Status_When_Coordinator_Returns_False()
    {
        FakeWorkspaceCoordinator coordinator = new()
        {
            LoadResult = false
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.LoadWorkspaceAsync(document);

        Assert.False(result);
        Assert.True(coordinator.LoadWasCalled);
        Assert.Null(dirtyStateService.LastSavedDocument);

        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Workspace load canceled.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Warning, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Report_Error_Status_When_Coordinator_Fails()
    {
        FakeWorkspaceCoordinator coordinator = new()
        {
            SaveException = new InvalidOperationException("Save failed.")
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.SaveWorkspaceAsync(document);

        Assert.False(result);
        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Failed to save workspace: Save failed.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Error, document.OperationStatus.StatusSeverity);
        Assert.Null(dirtyStateService.LastSavedDocument);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Report_Error_Status_When_Coordinator_Fails()
    {
        FakeWorkspaceCoordinator coordinator = new()
        {
            LoadException = new InvalidOperationException("Load failed.")
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.LoadWorkspaceAsync(document);

        Assert.False(result);
        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Failed to load workspace: Load failed.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Error, document.OperationStatus.StatusSeverity);
        Assert.Null(dirtyStateService.LastSavedDocument);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentLifecycleService service = CreateService();

        ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SaveWorkspaceAsync(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentLifecycleService service = CreateService();

        ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.LoadWorkspaceAsync(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public async Task SaveWorkspaceAsAsync_Should_Set_Busy_Progress_And_Success_Status()
    {
        FakeWorkspaceCoordinator coordinator = new();
        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        int stateChanges = 0;

        bool result = await service.SaveWorkspaceAsAsync(document, () => stateChanges++);

        Assert.True(result);
        Assert.True(coordinator.SaveAsWasCalled);
        Assert.Same(document, dirtyStateService.LastSavedDocument);

        Assert.False(document.OperationStatus.IsBusy);
        Assert.False(document.OperationStatus.IsProgressVisible);
        Assert.Equal("Workspace saved.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Success, document.OperationStatus.StatusSeverity);
        Assert.True(stateChanges >= 2);
    }

    [Fact]
    public async Task SaveWorkspaceAsAsync_Should_Report_Canceled_Status_When_Coordinator_Returns_False()
    {
        FakeWorkspaceCoordinator coordinator = new()
        {
            SaveAsResult = false
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.SaveWorkspaceAsAsync(document);

        Assert.False(result);
        Assert.True(coordinator.SaveAsWasCalled);
        Assert.Null(dirtyStateService.LastSavedDocument);

        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Workspace save canceled.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Warning, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public async Task SaveWorkspaceAsAsync_Should_Report_Error_Status_When_Coordinator_Fails()
    {
        FakeWorkspaceCoordinator coordinator = new()
        {
            SaveAsException = new InvalidOperationException("Save as failed.")
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.SaveWorkspaceAsAsync(document);

        Assert.False(result);
        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Failed to save workspace: Save as failed.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Error, document.OperationStatus.StatusSeverity);
        Assert.Null(dirtyStateService.LastSavedDocument);
    }

    [Fact]
    public async Task SaveWorkspaceAsAsync_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentLifecycleService service = CreateService();

        ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SaveWorkspaceAsAsync(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Add_Workspace_To_Recent_When_Save_Succeeds()
    {
        string workspacePath = @"D:\Workspaces\Project.filemerger.workspace.json";

        FakeWorkspaceCoordinator coordinator = new()
        {
            WorkspacePathToAssign = workspacePath
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        FakeRecentWorkspacesService recentWorkspacesService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService, recentWorkspacesService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.SaveWorkspaceAsync(document);

        Assert.True(result);
        Assert.Equal(workspacePath, Assert.Single(recentWorkspacesService.AddedPaths));
    }

    [Fact]
    public async Task SaveWorkspaceAsAsync_Should_Add_Workspace_To_Recent_When_SaveAs_Succeeds()
    {
        string workspacePath = @"D:\Workspaces\ProjectAs.filemerger.workspace.json";

        FakeWorkspaceCoordinator coordinator = new()
        {
            WorkspacePathToAssign = workspacePath
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        FakeRecentWorkspacesService recentWorkspacesService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService, recentWorkspacesService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.SaveWorkspaceAsAsync(document);

        Assert.True(result);
        Assert.Equal(workspacePath, Assert.Single(recentWorkspacesService.AddedPaths));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Add_Workspace_To_Recent_When_Load_Succeeds()
    {
        string workspacePath = @"D:\Workspaces\Loaded.filemerger.workspace.json";

        FakeWorkspaceCoordinator coordinator = new()
        {
            WorkspacePathToAssign = workspacePath
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        FakeRecentWorkspacesService recentWorkspacesService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService, recentWorkspacesService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.LoadWorkspaceAsync(document);

        Assert.True(result);
        Assert.Equal(workspacePath, Assert.Single(recentWorkspacesService.AddedPaths));
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Not_Add_Workspace_To_Recent_When_Save_Is_Canceled()
    {
        FakeWorkspaceCoordinator coordinator = new()
        {
            SaveResult = false,
            WorkspacePathToAssign = @"D:\Workspaces\Canceled.filemerger.workspace.json"
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        FakeRecentWorkspacesService recentWorkspacesService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService, recentWorkspacesService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.SaveWorkspaceAsync(document);

        Assert.False(result);
        Assert.Empty(recentWorkspacesService.AddedPaths);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Not_Add_Workspace_To_Recent_When_Load_Is_Canceled()
    {
        FakeWorkspaceCoordinator coordinator = new()
        {
            LoadResult = false,
            WorkspacePathToAssign = @"D:\Workspaces\CanceledLoad.filemerger.workspace.json"
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        FakeRecentWorkspacesService recentWorkspacesService = new();
        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService, recentWorkspacesService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.LoadWorkspaceAsync(document);

        Assert.False(result);
        Assert.Empty(recentWorkspacesService.AddedPaths);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Still_Return_Success_When_Recent_Update_Fails()
    {
        string workspacePath = @"D:\Workspaces\Project.filemerger.workspace.json";

        FakeWorkspaceCoordinator coordinator = new()
        {
            WorkspacePathToAssign = workspacePath
        };

        FakeWorkspaceDocumentDirtyStateService dirtyStateService = new();
        FakeRecentWorkspacesService recentWorkspacesService = new()
        {
            AddException = new IOException("Recent storage failed.")
        };

        WorkspaceDocumentLifecycleService service = CreateService(coordinator, dirtyStateService, recentWorkspacesService);
        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await service.SaveWorkspaceAsync(document);

        Assert.True(result);
        Assert.Same(document, dirtyStateService.LastSavedDocument);
        Assert.Equal("Workspace saved.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Success, document.OperationStatus.StatusSeverity);
    }

    private static WorkspaceDocumentLifecycleService CreateService(
        FakeWorkspaceCoordinator? coordinator = null,
        FakeWorkspaceDocumentDirtyStateService? dirtyStateService = null,
        FakeRecentWorkspacesService? recentWorkspacesService = null)
    {
        return new WorkspaceDocumentLifecycleService(
            coordinator ?? new FakeWorkspaceCoordinator(),
            dirtyStateService ?? new FakeWorkspaceDocumentDirtyStateService(),
            recentWorkspacesService ?? new FakeRecentWorkspacesService());
    }

    private static WorkspaceDocumentViewModel CreateDocument()
    {
        return WorkspaceDocumentTestFactory.CreateDocument();
    }

    private static MergeOutput CreateOutput()
    {
        return new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(
                filesScanned: 1,
                filesIncluded: 1,
                filesSkipped: 0,
                totalCharacters: 6,
                duration: TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));
    }

    private sealed class FakeWorkspaceCoordinator : IWorkspaceCoordinator
    {
        public bool SaveWasCalled { get; private set; }
        public bool SaveAsWasCalled { get; private set; }
        public bool LoadWasCalled { get; private set; }
        public bool LoadFromPathWasCalled { get; private set; }

        public string? LastLoadWorkspaceFromPath { get; private set; }

        public bool SaveResult { get; init; } = true;
        public bool SaveAsResult { get; init; } = true;
        public bool LoadResult { get; init; } = true;

        public string? WorkspacePathToAssign { get; init; }

        public Exception? SaveException { get; init; }
        public Exception? SaveAsException { get; init; }
        public Exception? LoadException { get; init; }

        public Task<bool> SaveWorkspaceAsync(
            WorkspaceDocumentViewModel document,
            CancellationToken cancellationToken = default)
        {
            SaveWasCalled = true;

            if (SaveException is not null)
                throw SaveException;

            if (SaveResult && !string.IsNullOrWhiteSpace(WorkspacePathToAssign))
                document.WorkspaceFilePath = WorkspacePathToAssign;

            return Task.FromResult(SaveResult);
        }

        public Task<bool> SaveWorkspaceAsAsync(
            WorkspaceDocumentViewModel document,
            CancellationToken cancellationToken = default)
        {
            SaveAsWasCalled = true;

            if (SaveAsException is not null)
                throw SaveAsException;

            if (SaveAsResult && !string.IsNullOrWhiteSpace(WorkspacePathToAssign))
                document.WorkspaceFilePath = WorkspacePathToAssign;

            return Task.FromResult(SaveAsResult);
        }

        public Task<bool> LoadWorkspaceAsync(
            WorkspaceDocumentViewModel document,
            CancellationToken cancellationToken = default)
        {
            LoadWasCalled = true;

            if (LoadException is not null)
                throw LoadException;

            if (LoadResult && !string.IsNullOrWhiteSpace(WorkspacePathToAssign))
                document.WorkspaceFilePath = WorkspacePathToAssign;

            return Task.FromResult(LoadResult);
        }

        public Task<bool> LoadWorkspaceFromPathAsync(
            WorkspaceDocumentViewModel document,
            string workspaceFilePath,
            CancellationToken cancellationToken = default)
        {
            LoadFromPathWasCalled = true;
            LastLoadWorkspaceFromPath = workspaceFilePath;

            if (LoadException is not null)
                throw LoadException;

            if (LoadResult)
            {
                document.WorkspaceFilePath = string.IsNullOrWhiteSpace(WorkspacePathToAssign)
                    ? workspaceFilePath
                    : WorkspacePathToAssign;
            }

            return Task.FromResult(LoadResult);
        }
    }

    private sealed class FakeWorkspaceDocumentDirtyStateService : IWorkspaceDocumentDirtyStateService
    {
        public WorkspaceDocumentViewModel? LastPreviewRefreshedDocument { get; private set; }

        public WorkspaceDocumentViewModel? LastPreviewAppliedDocument { get; private set; }

        public WorkspaceDocumentViewModel? LastWorkspaceRefreshedDocument { get; private set; }

        public WorkspaceDocumentViewModel? LastSavedDocument { get; private set; }

        public void RefreshPreviewDirtyState(WorkspaceDocumentViewModel document)
        {
            LastPreviewRefreshedDocument = document;
        }

        public void MarkPreviewApplied(WorkspaceDocumentViewModel document)
        {
            LastPreviewAppliedDocument = document;
        }

        public void RefreshWorkspaceDirtyState(WorkspaceDocumentViewModel document)
        {
            LastWorkspaceRefreshedDocument = document;
        }

        public void MarkWorkspaceSaved(WorkspaceDocumentViewModel document)
        {
            LastSavedDocument = document;
        }
    }

    private sealed class FakeRecentWorkspacesService : IRecentWorkspacesService
    {
        public List<string> AddedPaths { get; } = [];

        public Exception? AddException { get; init; }

        public Task<IReadOnlyCollection<RecentWorkspaceEntry>> GetRecentWorkspacesAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<RecentWorkspaceEntry> result = [];
            return Task.FromResult(result);
        }

        public Task AddOrUpdateAsync(
            string workspaceFilePath,
            CancellationToken cancellationToken = default)
        {
            if (AddException is not null)
                throw AddException;

            AddedPaths.Add(workspaceFilePath);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            string workspaceFilePath,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}