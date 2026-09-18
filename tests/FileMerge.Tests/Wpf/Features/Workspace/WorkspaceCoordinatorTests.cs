using System.IO;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Sources.ViewModels;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceCoordinatorTests
{
    [Fact]
    public async Task SaveWorkspaceAsync_Should_Persist_Document_Source_Exclusions()
    {
        var persistence = new FakeWorkspacePersistenceService();
        var saveDialog = new FakeSaveFileDialogService
        {
            SelectedPath = @"D:\Workspaces\test.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, saveDialog, new FakeOpenFileDialogService());

        WorkspaceDocumentViewModel document = CreateDocument();

        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                path: @"D:\Project",
                type: MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true,
                exclusions:
                [
                    new MergeSourceExclusion(
                        relativePath: "bin",
                        type: MergeSourceExclusionType.Directory,
                        isEnabled: true),

                    new MergeSourceExclusion(
                        relativePath: @"Secrets\ApiKeys.cs",
                        type: MergeSourceExclusionType.File,
                        isEnabled: false)
                ])
        ]);

        await coordinator.SaveWorkspaceAsync(document);

        Assert.NotNull(persistence.SavedWorkspace);

        WorkspaceSourceDto source = Assert.Single(persistence.SavedWorkspace!.Document.Sources);
        Assert.NotNull(source.Exclusions);
        Assert.Equal(2, source.Exclusions!.Count);

        Assert.Contains(
            source.Exclusions,
            x => x is { RelativePath: "bin", Type: MergeSourceExclusionType.Directory, IsEnabled: true });

        Assert.Contains(
            source.Exclusions,
            x => x is { RelativePath: @"Secrets\ApiKeys.cs", Type: MergeSourceExclusionType.File, IsEnabled: false });
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Set_Document_WorkspaceFilePath()
    {
        var persistence = new FakeWorkspacePersistenceService();
        var saveDialog = new FakeSaveFileDialogService
        {
            SelectedPath = @"D:\Workspaces\test.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, saveDialog, new FakeOpenFileDialogService());

        WorkspaceDocumentViewModel document = CreateDocument();

        await coordinator.SaveWorkspaceAsync(document);

        Assert.Equal(@"D:\Workspaces\test.filemerger.workspace.json", document.WorkspaceFilePath);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Persist_Profile_Metadata()
    {
        var persistence = new FakeWorkspacePersistenceService();
        var saveDialog = new FakeSaveFileDialogService
        {
            SelectedPath = @"D:\Workspaces\test.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, saveDialog, new FakeOpenFileDialogService());

        WorkspaceDocumentViewModel document = CreateDocument();
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();

        document.ProfileEditor.ApplyProfile(CreateProfile(skippedFileCategories: selection));
        document.ProfileEditor.WorkingProfileName = "Unity Repository";
        document.CurrentProfileEntryId = "profile-unity";
        document.ProfileOriginEntryId = "profile-origin";
        document.ProfileOriginDisplayName = "Origin Profile";

        bool result = await coordinator.SaveWorkspaceAsync(document);

        Assert.True(result);
        Assert.NotNull(persistence.SavedWorkspace);
        Assert.Equal("Unity Repository", persistence.SavedWorkspace!.Document.ProfileDisplayName);
        Assert.Equal("profile-unity", persistence.SavedWorkspace.Document.ProfileEntryId);
        Assert.Equal("profile-origin", persistence.SavedWorkspace.Document.ProfileOriginEntryId);
        Assert.Equal("Origin Profile", persistence.SavedWorkspace.Document.ProfileOriginDisplayName);
        Assert.Equal(selection, persistence.SavedWorkspace.Document.Profile.SkippedFileCategories);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Save_To_Existing_WorkspaceFilePath_Without_Dialog()
    {
        var persistence = new FakeWorkspacePersistenceService();
        var saveDialog = new FakeSaveFileDialogService
        {
            SelectedPath = @"D:\Workspaces\unexpected.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, saveDialog, new FakeOpenFileDialogService());

        WorkspaceDocumentViewModel document = CreateDocument();
        document.WorkspaceFilePath = @"D:\Workspaces\existing.filemerger.workspace.json";

        bool result = await coordinator.SaveWorkspaceAsync(document);

        Assert.True(result);
        Assert.Equal(@"D:\Workspaces\existing.filemerger.workspace.json", persistence.SavedFilePath);
        Assert.Equal(@"D:\Workspaces\existing.filemerger.workspace.json", document.WorkspaceFilePath);
        Assert.Equal(0, saveDialog.SelectSaveFilePathCalls);
    }

    [Fact]
    public async Task SaveWorkspaceAsAsync_Should_Show_SaveDialog_And_Update_WorkspaceFilePath()
    {
        var persistence = new FakeWorkspacePersistenceService();
        var saveDialog = new FakeSaveFileDialogService
        {
            SelectedPath = @"D:\Workspaces\saved-as.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, saveDialog, new FakeOpenFileDialogService());

        WorkspaceDocumentViewModel document = CreateDocument();
        document.WorkspaceFilePath = @"D:\Workspaces\existing.filemerger.workspace.json";

        bool result = await coordinator.SaveWorkspaceAsAsync(document);

        Assert.True(result);
        Assert.Equal(1, saveDialog.SelectSaveFilePathCalls);
        Assert.Equal(@"D:\Workspaces\existing.filemerger.workspace.json", saveDialog.InitialPath);
        Assert.Equal(@"D:\Workspaces\saved-as.filemerger.workspace.json", persistence.SavedFilePath);
        Assert.Equal(@"D:\Workspaces\saved-as.filemerger.workspace.json", document.WorkspaceFilePath);
    }

    [Fact]
    public async Task SaveWorkspaceAsAsync_Should_Return_False_When_SaveDialog_Is_Canceled()
    {
        var persistence = new FakeWorkspacePersistenceService();
        var saveDialog = new FakeSaveFileDialogService
        {
            SelectedPath = null
        };

        var coordinator = new WorkspaceCoordinator(persistence, saveDialog, new FakeOpenFileDialogService());

        WorkspaceDocumentViewModel document = CreateDocument();
        document.WorkspaceFilePath = @"D:\Workspaces\existing.filemerger.workspace.json";

        bool result = await coordinator.SaveWorkspaceAsAsync(document);

        Assert.False(result);
        Assert.Equal(1, saveDialog.SelectSaveFilePathCalls);
        Assert.Null(persistence.SavedWorkspace);
        Assert.Equal(@"D:\Workspaces\existing.filemerger.workspace.json", document.WorkspaceFilePath);
    }

    [Fact]
    public async Task SaveWorkspaceAsAsync_Should_Throw_When_Document_Is_Null()
    {
        var coordinator = new WorkspaceCoordinator(
            new FakeWorkspacePersistenceService(),
            new FakeSaveFileDialogService(),
            new FakeOpenFileDialogService());

        ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            coordinator.SaveWorkspaceAsAsync(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Open_From_Default_Workspaces_Directory()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(sources: [])
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\selected.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();
        string previousPath = @"D:\SomeOtherFolder\existing.filemerger.workspace.json";
        document.WorkspaceFilePath = previousPath;

        bool result = await coordinator.LoadWorkspaceAsync(document);

        string expectedDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileMerger",
            "Workspaces");

        Assert.True(result);
        Assert.Equal(1, openDialog.SelectFileCallCount);
        Assert.Equal(expectedDirectory, openDialog.InitialPath);
        Assert.NotEqual(previousPath, openDialog.InitialPath);
        Assert.True(Directory.Exists(openDialog.InitialPath));
        Assert.Equal("Loaded Session", document.SessionSettings.SessionName);
        Assert.Equal(openDialog.SelectedFile, document.WorkspaceFilePath);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Restore_Document_Source_Exclusions()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(
                sources:
                [
                    new WorkspaceSourceDto(
                        Path: @"D:\Project",
                        Type: MergeSourceType.Directory,
                        IsRecursive: true,
                        IsEnabled: true,
                        Exclusions:
                        [
                            new WorkspaceSourceExclusionDto(
                                RelativePath: "bin",
                                Type: MergeSourceExclusionType.Directory,
                                IsEnabled: true),

                            new WorkspaceSourceExclusionDto(
                                RelativePath: @"Secrets\ApiKeys.cs",
                                Type: MergeSourceExclusionType.File,
                                IsEnabled: false)
                        ])
                ])
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\test.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();

        await coordinator.LoadWorkspaceAsync(document);

        MergeSourceItemViewModel source = Assert.Single(document.SourcesPane.Sources);
        Assert.Equal(2, source.Exclusions.Count);

        Assert.Contains(
            source.Exclusions,
            x => x is { RelativePath: "bin", Type: MergeSourceExclusionType.Directory, IsEnabled: true });

        Assert.Contains(
            source.Exclusions,
            x => x is { RelativePath: @"Secrets\ApiKeys.cs", Type: MergeSourceExclusionType.File, IsEnabled: false });
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Treat_Missing_Exclusions_As_Empty()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(
                sources:
                [
                    new WorkspaceSourceDto(
                        Path: @"D:\Project",
                        Type: MergeSourceType.Directory,
                        IsRecursive: true,
                        IsEnabled: true,
                        Exclusions: null)
                ])
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\old.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();

        await coordinator.LoadWorkspaceAsync(document);

        MergeSourceItemViewModel source = Assert.Single(document.SourcesPane.Sources);
        Assert.Empty(source.Exclusions);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Not_Restore_Exclusions_For_File_Source()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(
                sources:
                [
                    new WorkspaceSourceDto(
                        Path: @"D:\Project\Program.cs",
                        Type: MergeSourceType.File,
                        IsRecursive: true,
                        IsEnabled: true,
                        Exclusions:
                        [
                            new WorkspaceSourceExclusionDto(
                                RelativePath: "bin",
                                Type: MergeSourceExclusionType.Directory,
                                IsEnabled: true)
                        ])
                ])
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\invalid.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();

        await coordinator.LoadWorkspaceAsync(document);

        MergeSourceItemViewModel source = Assert.Single(document.SourcesPane.Sources);

        Assert.Equal(MergeSourceType.File, source.Type);
        Assert.False(source.IsRecursive);
        Assert.Empty(source.Exclusions);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Restore_Document_Inclusion_Overrides()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(
                sources: [],
                inclusionOverrides: new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    [@"D:\Project\Excluded.cs"] = false,
                    [@"D:\Project\Included.cs"] = true
                })
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\test.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();

        await coordinator.LoadWorkspaceAsync(document);

        Dictionary<string, bool> overrides = document.FilesPane.CaptureOverridesDictionary();

        Assert.Equal(2, overrides.Count);
        Assert.False(overrides[@"D:\Project\Excluded.cs"]);
        Assert.True(overrides[@"D:\Project\Included.cs"]);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Set_Document_WorkspaceFilePath()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(sources: [])
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\loaded.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();

        await coordinator.LoadWorkspaceAsync(document);

        Assert.Equal(@"D:\Workspaces\loaded.filemerger.workspace.json", document.WorkspaceFilePath);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Restore_Profile_Metadata()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(
                sources: [],
                profile: CreateProfile(skippedFileCategories: CreateSkippedFileCategorySelection()),
                profileDisplayName: "Saved Workspace Profile",
                profileEntryId: "profile-saved",
                profileOriginEntryId: "profile-origin",
                profileOriginDisplayName: "Origin Profile")
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\loaded.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();

        document.ProfileEditor.WorkingProfileName = "Previous Profile";
        document.CurrentProfileEntryId = "previous-profile";
        document.ProfileOriginEntryId = "previous-origin";
        document.ProfileOriginDisplayName = "Previous Origin";

        bool result = await coordinator.LoadWorkspaceAsync(document);

        Assert.True(result);
        Assert.Equal("Saved Workspace Profile", document.CurrentProfileName);
        Assert.Equal("Saved Workspace Profile", document.ProfileEditor.WorkingProfileName);
        Assert.Equal("profile-saved", document.CurrentProfileEntryId);
        Assert.Equal("profile-origin", document.ProfileOriginEntryId);
        Assert.Equal("Origin Profile", document.ProfileOriginDisplayName);
        Assert.Equal(
            CreateSkippedFileCategorySelection(),
            document.ProfileEditor.CaptureProfile().SkippedFileCategories);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Derive_Profile_Origin_For_Legacy_Linked_Workspace()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(
                sources: [],
                profile: CreateProfile(includeSourceExcludedFiles: true, skippedFileCategories: null),
                profileDisplayName: "Legacy Profile",
                profileEntryId: "legacy-profile")
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\legacy.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await coordinator.LoadWorkspaceAsync(document);

        Assert.True(result);
        Assert.Equal("legacy-profile", document.CurrentProfileEntryId);
        Assert.Equal("legacy-profile", document.ProfileOriginEntryId);
        Assert.Equal("Legacy Profile", document.ProfileOriginDisplayName);
        Assert.True(document.ProfileEditor.IncludeSourceExclusionsInSkippedMetadata);
        Assert.Null(document.ProfileEditor.CaptureProfile().SkippedFileCategories);
        Assert.True(document.ProfileEditor.CaptureProfile().IncludeSourceExcludedFiles);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Not_Infer_Profile_Origin_For_Legacy_Custom_Workspace()
    {
        var persistence = new FakeWorkspacePersistenceService
        {
            WorkspaceToLoad = CreateWorkspace(sources: [], profileDisplayName: "Custom Profile", profileEntryId: null)
        };

        var openDialog = new FakeOpenFileDialogService
        {
            SelectedFile = @"D:\Workspaces\custom.filemerger.workspace.json"
        };

        var coordinator = new WorkspaceCoordinator(persistence, new FakeSaveFileDialogService(), openDialog);

        WorkspaceDocumentViewModel document = CreateDocument();

        bool result = await coordinator.LoadWorkspaceAsync(document);

        Assert.True(result);
        Assert.Null(document.CurrentProfileEntryId);
        Assert.Null(document.ProfileOriginEntryId);
        Assert.Null(document.ProfileOriginDisplayName);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Throw_When_Document_Is_Null()
    {
        var coordinator = new WorkspaceCoordinator(
            new FakeWorkspacePersistenceService(),
            new FakeSaveFileDialogService(),
            new FakeOpenFileDialogService());

        ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            coordinator.SaveWorkspaceAsync(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Throw_When_Document_Is_Null()
    {
        var coordinator = new WorkspaceCoordinator(
            new FakeWorkspacePersistenceService(),
            new FakeSaveFileDialogService(),
            new FakeOpenFileDialogService());

        ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            coordinator.LoadWorkspaceAsync(null!));

        Assert.Equal("document", ex.ParamName);
    }

    private static WorkspaceDocumentViewModel CreateDocument(string sessionName = "Test Session")
    {
        return WorkspaceDocumentTestFactory.CreateSavedDocument(sessionName: sessionName, outputPath: string.Empty);
    }

    private static WorkspaceDto CreateWorkspace(
        List<WorkspaceSourceDto> sources,
        Dictionary<string, bool>? inclusionOverrides = null,
        WorkspaceProfileDto? profile = null,
        string? profileDisplayName = null,
        string? profileEntryId = null,
        string? profileOriginEntryId = null,
        string? profileOriginDisplayName = null)
    {
        return new WorkspaceDto(
            Document: new WorkspaceDocumentDto(
                SessionName: "Loaded Session",
                OutputPath: @"D:\Output\loaded.txt",
                Sources: sources,
                Profile: profile ?? CreateProfile(),
                InclusionOverrides: inclusionOverrides ?? [],
                ProfileDisplayName: profileDisplayName,
                ProfileEntryId: profileEntryId,
                ProfileOriginEntryId: profileOriginEntryId,
                ProfileOriginDisplayName: profileOriginDisplayName));
    }

    private static WorkspaceProfileDto CreateProfile(
        bool includeSourceExcludedFiles = false,
        SkippedFileCategorySelection? skippedFileCategories = null)
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: false,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            FileTypes:
            [
                new WorkspaceFileTypeDto(
                    Extension: ".cs",
                    DisplayName: "C# source",
                    Kind: FileKind.CSharp,
                    IsEnabled: true,
                    SupportsLanguageSpecificProcessing: true)
            ],
            LineEndingMode: LineEndingMode.Preserve,
            SortMode: SortMode.ByRelativePathAscending,
            InputEncodingMode: InputEncodingMode.Auto,
            PreferredInputEncodingName: null,
            FallbackInputEncodingName: "windows-1251",
            FilterRules: [],
            IncludeSourceExcludedFiles: includeSourceExcludedFiles,
            SkippedFileCategories: skippedFileCategories);
    }

    private static SkippedFileCategorySelection CreateSkippedFileCategorySelection()
    {
        return new SkippedFileCategorySelection(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: false,
            IncludeProfileExclusions: true,
            IncludeManualExclusions: false,
            IncludeSourceExclusions: true,
            IncludeProcessingFailures: true,
            IncludeOther: false);
    }

    private sealed class FakeWorkspacePersistenceService : IWorkspacePersistenceService
    {
        public WorkspaceDto? SavedWorkspace { get; private set; }
        public string? SavedFilePath { get; private set; }
        public WorkspaceDto? WorkspaceToLoad { get; init; }

        public Task SaveWorkspaceAsync(
            WorkspaceDto workspace,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            SavedWorkspace = workspace;
            SavedFilePath = filePath;
            return Task.CompletedTask;
        }

        public Task<WorkspaceDto> LoadWorkspaceAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (WorkspaceToLoad is null)
                throw new InvalidOperationException("WorkspaceToLoad is not configured.");

            return Task.FromResult(WorkspaceToLoad);
        }
    }
}