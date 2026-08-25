using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
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

        ProfileManagerViewModel viewModel = CreateViewModel([entry], host);

        await viewModel.InitializeAsync();

        ProfileLibraryListItemViewModel item = Assert.Single(viewModel.Profiles);
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

        ProfileManagerViewModel viewModel = CreateViewModel([entry], host);

        await viewModel.InitializeAsync();

        ProfileLibraryListItemViewModel item = Assert.Single(viewModel.Profiles);
        Assert.False(item.IsUsedByCurrentSession);
    }

    private static ProfileManagerViewModel CreateViewModel(
        IReadOnlyCollection<ProfileLibraryEntry> entries,
        ICurrentSessionProfileHost currentSessionProfileHost)
    {
        return new ProfileManagerViewModel(
            new FakeProfileLibraryService(entries),
            new FakeProfileEditorFactory(),
            currentSessionProfileHost,
            new FakeUserPromptService(),
            new FakeOpenFileDialogService(),
            new FakeSaveFileDialogService(),
            new FakeClipboardService());
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

    private static WorkspaceProfileDto CreateProfile(bool includeHeaderComment = false)
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
            FallbackInputEncodingName: "windows-1251");
    }

    private sealed class FakeProfileEditorFactory : IProfileEditorFactory
    {
        public ProfileEditorViewModel Create()
        {
            return new ProfileEditorViewModel(new BuiltInFileTypeCatalog());
        }
    }

    private sealed class FakeProfileLibraryService(IReadOnlyCollection<ProfileLibraryEntry> entries) : IProfileLibraryService
    {
        public string GetPrimaryProfilesDirectory()
        {
            return @"D:\Profiles";
        }

        public Task<IReadOnlyCollection<ProfileLibraryEntry>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(entries);
        }

        public Task<ProfileLibraryEntry> LoadAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            ProfileLibraryEntry entry = entries.Single(x =>
                string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(entry);
        }

        public Task<ProfileLibraryEntry> SaveAsync(
            ProfileLibraryEntry entry,
            bool saveAsNew = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }

        public Task ExportAsync(
            ProfileLibraryEntry entry,
            string targetFilePath,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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