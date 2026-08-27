using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Profile.ViewModels;

public sealed class ProfileManagerViewModelTests
{
    [Fact]
    public async Task InitializeAsync_Should_Mark_Profile_Active_By_EntryId()
    {
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-repository",
            name: "Repository Profile");

        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Different Display Name",
            currentProfileEntryId: "profile-repository",
            profile: CreateProfile(includeHeaderComment: true));

        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileLibraryListItemViewModel item = Assert.Single(context.ViewModel.Profiles);
        Assert.True(item.IsUsedByCurrentSession);
    }

    [Fact]
    public async Task InitializeAsync_Should_Not_Infer_Active_Profile_From_Name_And_Content()
    {
        WorkspaceProfileDto profile = CreateProfile(includeHeaderComment: true);
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-repository",
            name: "Repository Profile",
            profile: profile);

        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Repository Profile",
            currentProfileEntryId: null,
            profile: profile);

        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileLibraryListItemViewModel item = Assert.Single(context.ViewModel.Profiles);
        Assert.False(item.IsUsedByCurrentSession);
    }

    [Fact]
    public async Task InitializeAsync_Should_Load_Category_Selection_Into_Editor_Without_Applying_To_Current_Session()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        WorkspaceProfileDto libraryProfile = CreateProfile(
            skippedFileCategories: selection);
        WorkspaceProfileDto currentProfile = CreateProfile();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-categories",
            name: "Category Profile",
            profile: libraryProfile);
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: currentProfile);

        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        Assert.Equal(selection, context.ViewModel.Editor.CaptureProfile().SkippedFileCategories);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Equal(currentProfile, host.CaptureCurrentProfile());
    }

    [Fact]
    public async Task SaveCommand_Should_Persist_Staged_Category_Selection()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-save",
            name: "Save Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: null,
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();
        context.ViewModel.Editor.IncludeUnsupportedFilesInSkippedMetadata = false;

        context.ViewModel.SaveCommand.Execute(null);

        Assert.NotNull(context.LibraryService.LastSavedEntry);
        Assert.False(context.LibraryService.LastSaveAsNew.GetValueOrDefault(true));
        Assert.Equal(
            selection with { IncludeUnsupportedFiles = false },
            context.LibraryService.LastSavedEntry!.Profile.SkippedFileCategories);
        Assert.False(context.ViewModel.IsDirty);
        Assert.Null(host.CaptureCurrentProfile().SkippedFileCategories);
    }

    [Fact]
    public async Task ImportCommand_Should_Load_Imported_Category_Selection_Without_Applying_To_Current_Session()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection(
            includeSourceExclusions: true);
        ProfileLibraryEntry existing = CreateEntry(
            id: "profile-existing",
            name: "Existing Profile");
        ProfileLibraryEntry imported = CreateEntry(
            id: "profile-imported",
            name: "Imported Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        WorkspaceProfileDto currentProfile = CreateProfile();
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: currentProfile);
        TestContext context = CreateContext(
            [existing],
            host,
            selectedImportFiles: [@"D:\Profiles\import.filemerger.profile.json"]);
        context.LibraryService.ImportResult = [imported];

        await context.ViewModel.InitializeAsync();

        context.ViewModel.ImportCommand.Execute(null);

        Assert.NotNull(context.LibraryService.LastImportedFilePaths);
        Assert.Equal(
            [@"D:\Profiles\import.filemerger.profile.json"],
            context.LibraryService.LastImportedFilePaths);
        Assert.Equal("profile-imported", context.ViewModel.SelectedProfile?.Id);
        Assert.Equal(selection, context.ViewModel.Editor.CaptureProfile().SkippedFileCategories);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Equal(currentProfile, host.CaptureCurrentProfile());
    }

    [Fact]
    public async Task ExportCommand_Should_Use_Staged_Category_Selection_Without_Applying_To_Current_Session()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-export",
            name: "Export Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        WorkspaceProfileDto currentProfile = CreateProfile();
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: null,
            profile: currentProfile);
        TestContext context = CreateContext(
            [entry],
            host,
            selectedExportPath: @"D:\Exports\profile.filemerger.profile.json");

        await context.ViewModel.InitializeAsync();
        context.ViewModel.Editor.IncludeManualExclusionsInSkippedMetadata = false;

        context.ViewModel.ExportCommand.Execute(null);

        Assert.NotNull(context.LibraryService.LastExportedEntry);
        Assert.Equal(
            selection with { IncludeManualExclusions = false },
            context.LibraryService.LastExportedEntry!.Profile.SkippedFileCategories);
        Assert.Equal(
            @"D:\Exports\profile.filemerger.profile.json",
            context.LibraryService.LastExportTargetPath);
        Assert.Equal(currentProfile, host.CaptureCurrentProfile());
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Preserve_EntryId_When_Category_Selection_Is_Unchanged()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-apply",
            name: "Apply Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: null,
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        context.ViewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal("Apply Profile", host.CurrentProfileName);
        Assert.Equal("profile-apply", host.CurrentProfileEntryId);
        Assert.Equal(selection, host.CaptureCurrentProfile().SkippedFileCategories);
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Clear_EntryId_When_Category_Selection_Changes()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-apply",
            name: "Apply Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();
        context.ViewModel.Editor.IncludeProfileExclusionsInSkippedMetadata = true;

        context.ViewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal("Apply Profile", host.CurrentProfileName);
        Assert.Null(host.CurrentProfileEntryId);
        Assert.Equal(
            selection with { IncludeProfileExclusions = true },
            host.CaptureCurrentProfile().SkippedFileCategories);
    }

    private static TestContext CreateContext(
        IReadOnlyCollection<ProfileLibraryEntry> entries,
        FakeCurrentSessionProfileHost currentSessionProfileHost,
        IReadOnlyList<string>? selectedImportFiles = null,
        string? selectedExportPath = null)
    {
        var libraryService = new FakeProfileLibraryService(entries);
        var openFileDialogService = new FakeOpenFileDialogService
        {
            SelectedFiles = selectedImportFiles ?? []
        };
        var saveFileDialogService = new FakeSaveFileDialogService
        {
            SelectedPath = selectedExportPath
        };

        ProfileManagerViewModel viewModel = new(
            libraryService,
            new FakeProfileEditorFactory(),
            currentSessionProfileHost,
            new FakeUserPromptService(),
            openFileDialogService,
            saveFileDialogService,
            new FakeClipboardService());

        return new TestContext(
            viewModel,
            libraryService);
    }

    private static ProfileLibraryEntry CreateEntry(
        string id,
        string name,
        WorkspaceProfileDto? profile = null)
    {
        return new ProfileLibraryEntry(
            Metadata: new ProfileMetadataDto(
                Id: id,
                Name: name,
                Description: null,
                CreatedAtUtc: DateTime.UtcNow,
                UpdatedAtUtc: DateTime.UtcNow),
            Profile: profile ?? CreateProfile(),
            FilePath: null,
            Kind: ProfileEntryKind.User);
    }

    private static WorkspaceProfileDto CreateProfile(
        bool includeHeaderComment = false,
        bool includeSourceExcludedFiles = false,
        SkippedFileCategorySelection? skippedFileCategories = null)
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: includeHeaderComment,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            RemoveUsingDirectives: false,
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
            IncludeSourceExcludedFiles: includeSourceExcludedFiles,
            SkippedFileCategories: skippedFileCategories);
    }

    private static SkippedFileCategorySelection CreateSkippedFileCategorySelection(
        bool includeSourceExclusions = false)
    {
        return new SkippedFileCategorySelection(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: true,
            IncludeProfileExclusions: false,
            IncludeManualExclusions: true,
            IncludeSourceExclusions: includeSourceExclusions,
            IncludeProcessingFailures: true,
            IncludeOther: false);
    }

    private sealed record TestContext(
        ProfileManagerViewModel ViewModel,
        FakeProfileLibraryService LibraryService);

    private sealed class FakeProfileEditorFactory : IProfileEditorFactory
    {
        public ProfileEditorViewModel Create()
        {
            return new ProfileEditorViewModel(new BuiltInFileTypeCatalog());
        }
    }

    private sealed class FakeProfileLibraryService(IReadOnlyCollection<ProfileLibraryEntry> entries) : IProfileLibraryService
    {
        private readonly List<ProfileLibraryEntry> _entries = [.. entries];

        public ProfileLibraryEntry? LastSavedEntry { get; private set; }

        public bool? LastSaveAsNew { get; private set; }

        public IReadOnlyList<string>? LastImportedFilePaths { get; private set; }

        public IReadOnlyCollection<ProfileLibraryEntry> ImportResult { get; set; } = [];

        public ProfileLibraryEntry? LastExportedEntry { get; private set; }

        public string? LastExportTargetPath { get; private set; }

        public string GetPrimaryProfilesDirectory()
        {
            return @"D:\Profiles";
        }

        public Task<IReadOnlyCollection<ProfileLibraryEntry>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<ProfileLibraryEntry> entries = [.. _entries];
            return Task.FromResult(entries);
        }

        public Task<ProfileLibraryEntry> LoadAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            ProfileLibraryEntry entry = _entries.Single(x =>
                string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(entry);
        }

        public Task<ProfileLibraryEntry> SaveAsync(
            ProfileLibraryEntry entry,
            bool saveAsNew = false,
            CancellationToken cancellationToken = default)
        {
            LastSavedEntry = entry;
            LastSaveAsNew = saveAsNew;

            int existingIndex = _entries.FindIndex(x =>
                string.Equals(x.Id, entry.Id, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
                _entries[existingIndex] = entry;
            else
                _entries.Add(entry);

            return Task.FromResult(entry);
        }

        public Task DeleteAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<ProfileLibraryEntry>> ImportAsync(
            IReadOnlyCollection<string> filePaths,
            CancellationToken cancellationToken = default)
        {
            LastImportedFilePaths = [.. filePaths];

            foreach (ProfileLibraryEntry importedEntry in ImportResult)
            {
                int existingIndex = _entries.FindIndex(x =>
                    string.Equals(x.Id, importedEntry.Id, StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                    _entries[existingIndex] = importedEntry;
                else
                    _entries.Add(importedEntry);
            }

            return Task.FromResult(ImportResult);
        }

        public Task ExportAsync(
            ProfileLibraryEntry entry,
            string targetFilePath,
            CancellationToken cancellationToken = default)
        {
            LastExportedEntry = entry;
            LastExportTargetPath = targetFilePath;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentSessionProfileHost(
        string currentProfileName,
        string? currentProfileEntryId,
        WorkspaceProfileDto profile)
        : ICurrentSessionProfileHost
    {
        private WorkspaceProfileDto _profile = profile;

        public string CurrentProfileName { get; private set; } = currentProfileName;

        public string? CurrentProfileEntryId { get; private set; } = currentProfileEntryId;

        public WorkspaceProfileDto CaptureCurrentProfile()
        {
            return _profile;
        }

        public void ApplyProfileToCurrentSession(
            string profileName,
            WorkspaceProfileDto profile,
            string? profileEntryId)
        {
            CurrentProfileName = profileName;
            CurrentProfileEntryId = profileEntryId;
            _profile = profile;
        }
    }
}