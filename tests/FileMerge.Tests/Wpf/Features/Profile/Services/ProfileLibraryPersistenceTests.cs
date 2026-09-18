using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Profile.Services;

public sealed class ProfileLibraryPersistenceTests : IDisposable
{
    private readonly string _tempRoot;

    public ProfileLibraryPersistenceTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(ProfileLibraryPersistenceTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public async Task SaveAsync_Should_Replace_Existing_User_Profile_After_Successful_Write()
    {
        ProfileLibraryService service = CreateService();
        ProfileLibraryEntry? saved = null;

        try
        {
            saved = await service.SaveAsync(CreateEntry(CreateProfile(includeSourceExcludedFiles: false)));
            string originalFilePath = Assert.IsType<string>(saved.FilePath);

            ProfileLibraryEntry updated = await service.SaveAsync(
                saved with
                {
                    Profile = CreateProfile(includeSourceExcludedFiles: true)
                });

            Assert.Equal(originalFilePath, updated.FilePath);

            ProfileLibraryEntry loaded = await service.LoadAsync(updated.Id);

            Assert.True(loaded.Profile.IncludeSourceExcludedFiles);
            AssertNoTemporaryFiles(originalFilePath);
        }
        finally
        {
            DeleteProfileFile(saved?.FilePath);
        }
    }

    [Fact]
    public async Task SaveAsync_Should_Preserve_Existing_User_Profile_When_Commit_Fails()
    {
        ProfileLibraryService service = CreateService();
        ProfileLibraryEntry? saved = null;

        try
        {
            saved = await service.SaveAsync(CreateEntry(CreateProfile(includeSourceExcludedFiles: false)));
            string filePath = Assert.IsType<string>(saved.FilePath);
            string originalJson = await File.ReadAllTextAsync(filePath);
            ProfileLibraryEntry updated = saved with
            {
                Profile = CreateProfile(includeSourceExcludedFiles: true)
            };

            {
                await using FileStream lockStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.None);

                Exception exception = await Assert.ThrowsAnyAsync<Exception>(() => service.SaveAsync(updated));

                Assert.True(
                    exception is IOException or UnauthorizedAccessException,
                    $"Expected an I/O commit failure, but got '{exception.GetType().FullName}'.");
            }

            string persistedJson = await File.ReadAllTextAsync(filePath);

            Assert.Equal(originalJson, persistedJson);
            AssertNoTemporaryFiles(filePath);
        }
        finally
        {
            DeleteProfileFile(saved?.FilePath);
        }
    }

    [Fact]
    public async Task SaveAsync_And_LoadAsync_Should_RoundTrip_Skipped_File_Category_Selection()
    {
        ProfileLibraryService service = CreateService();
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection(includeSourceExclusions: false);
        ProfileLibraryEntry entry = CreateEntry(
            CreateProfile(includeSourceExcludedFiles: true, skippedFileCategories: selection));

        ProfileLibraryEntry? saved = null;
        try
        {
            saved = await service.SaveAsync(entry);

            ProfileLibraryEntry loaded = await service.LoadAsync(saved.Id);

            Assert.True(loaded.Profile.IncludeSourceExcludedFiles);
            Assert.Equal(selection, loaded.Profile.SkippedFileCategories);

            ProfileEditorViewModel editor = CreateEditor();
            editor.ApplyProfile(loaded.Profile);

            OutputMetadataOptions options = editor.BuildProfile().GeneralOptions.OutputMetadataOptions;

            Assert.True(options.IncludeSourceExcludedFiles);
            Assert.Equal(selection, options.SkippedFileCategories);
            Assert.False(options.EffectiveSkippedFileCategories.IncludeSourceExclusions);
        }
        finally
        {
            DeleteProfileFile(saved?.FilePath);
        }
    }

    [Fact]
    public async Task ImportAsync_Should_Persist_Skipped_File_Category_Selection()
    {
        ProfileLibraryService service = CreateService();
        string path = Path.Combine(_tempRoot, "import.filemerger.profile.json");
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection(includeSourceExclusions: true);
        ProfileLibraryEntry source = CreateEntry(
            CreateProfile(includeSourceExcludedFiles: false, skippedFileCategories: selection));

        await service.ExportAsync(source, path);

        IReadOnlyCollection<ProfileLibraryEntry>? imported = null;
        try
        {
            imported = await service.ImportAsync([path]);

            ProfileLibraryEntry importedEntry = Assert.Single(imported);
            Assert.False(importedEntry.Profile.IncludeSourceExcludedFiles);
            Assert.Equal(selection, importedEntry.Profile.SkippedFileCategories);

            ProfileLibraryEntry loaded = await service.LoadAsync(importedEntry.Id);
            Assert.Equal(selection, loaded.Profile.SkippedFileCategories);
        }
        finally
        {
            if (imported is not null)
            {
                foreach (ProfileLibraryEntry importedEntry in imported)
                    DeleteProfileFile(importedEntry.FilePath);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_Should_Persist_Skipped_File_Category_Selection_In_Schema_V1()
    {
        ProfileLibraryService service = CreateService();
        string path = Path.Combine(_tempRoot, "profile.filemerger.profile.json");
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            CreateProfile(includeSourceExcludedFiles: true, skippedFileCategories: selection));

        await service.ExportAsync(entry, path);

        string json = await File.ReadAllTextAsync(path);
        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement profile = root.GetProperty("profile");
        JsonElement categories = profile.GetProperty("skippedFileCategories");

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.True(profile.GetProperty("includeSourceExcludedFiles").GetBoolean());
        Assert.Equal(
            selection.IncludeDisabledFileTypes,
            categories.GetProperty("includeDisabledFileTypes").GetBoolean());
        Assert.Equal(selection.IncludeUnsupportedFiles, categories.GetProperty("includeUnsupportedFiles").GetBoolean());
        Assert.Equal(
            selection.IncludeProfileExclusions,
            categories.GetProperty("includeProfileExclusions").GetBoolean());
        Assert.Equal(selection.IncludeManualExclusions, categories.GetProperty("includeManualExclusions").GetBoolean());
        Assert.Equal(selection.IncludeSourceExclusions, categories.GetProperty("includeSourceExclusions").GetBoolean());
        Assert.Equal(
            selection.IncludeProcessingFailures,
            categories.GetProperty("includeProcessingFailures").GetBoolean());
        Assert.Equal(selection.IncludeOther, categories.GetProperty("includeOther").GetBoolean());
    }

    [Fact]
    public async Task ExportAsync_Should_Preserve_Explicit_Category_Selection_Over_Legacy_Source_Flag()
    {
        ProfileLibraryService service = CreateService();
        string path = Path.Combine(_tempRoot, "profile.filemerger.profile.json");
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection(includeSourceExclusions: false);
        ProfileLibraryEntry entry = CreateEntry(
            CreateProfile(includeSourceExcludedFiles: true, skippedFileCategories: selection));

        await service.ExportAsync(entry, path);

        ProfileLibraryDocumentDto document = await DeserializeProfileDocumentAsync(path);

        ProfileEditorViewModel editor = CreateEditor();
        editor.ApplyProfile(document.Profile);

        OutputMetadataOptions options = editor.BuildProfile().GeneralOptions.OutputMetadataOptions;

        Assert.True(options.IncludeSourceExcludedFiles);
        Assert.Equal(selection, options.SkippedFileCategories);
        Assert.False(options.EffectiveSkippedFileCategories.IncludeSourceExclusions);
    }

    [Fact]
    public async Task Legacy_Schema_V1_Profile_Without_Category_Selection_Should_Preserve_Legacy_Semantics()
    {
        string path = Path.Combine(_tempRoot, "legacy.filemerger.profile.json");
        ProfileLibraryDocumentDto source = new(
            SchemaVersion: 1,
            Metadata: CreateMetadata(),
            Profile: CreateProfile(
                includeSourceExcludedFiles: true,
                skippedFileCategories: CreateSkippedFileCategorySelection()));

        string json = JsonSerializer.Serialize(source, CreateJsonOptions());
        var root = JsonNode.Parse(json)!;
        root["profile"]!.AsObject().Remove("skippedFileCategories");

        await File.WriteAllTextAsync(path, root.ToJsonString(CreateJsonOptions()));

        ProfileLibraryDocumentDto document = await DeserializeProfileDocumentAsync(path);

        Assert.Equal(1, document.SchemaVersion);
        Assert.Null(document.Profile.SkippedFileCategories);
        Assert.True(document.Profile.IncludeSourceExcludedFiles);

        ProfileEditorViewModel editor = CreateEditor();
        editor.ApplyProfile(document.Profile);

        OutputMetadataOptions options = editor.BuildProfile().GeneralOptions.OutputMetadataOptions;

        Assert.Null(options.SkippedFileCategories);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeDisabledFileTypes);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeUnsupportedFiles);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeProfileExclusions);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeManualExclusions);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeSourceExclusions);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeProcessingFailures);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeOther);
    }

    [Fact]
    public async Task Legacy_Schema_V1_Profile_With_RemoveUsingDirectives_Should_Load_And_Save_Without_Field()
    {
        ProfileLibraryService service = CreateService();
        string path = Path.Combine(_tempRoot, "legacy-using.filemerger.profile.json");
        ProfileLibraryDocumentDto source = new(SchemaVersion: 1, Metadata: CreateMetadata(), Profile: CreateProfile());

        string json = JsonSerializer.Serialize(source, CreateJsonOptions());
        var root = JsonNode.Parse(json)!;
        root["profile"]!.AsObject()["removeUsingDirectives"] = true;

        await File.WriteAllTextAsync(path, root.ToJsonString(CreateJsonOptions()));

        IReadOnlyCollection<ProfileLibraryEntry>? imported = null;
        try
        {
            imported = await service.ImportAsync([path]);

            ProfileLibraryEntry importedEntry = Assert.Single(imported);
            Assert.True(importedEntry.Profile.TrimTrailingEmptyLines);

            string savedPath = Assert.IsType<string>(importedEntry.FilePath);
            string savedJson = await File.ReadAllTextAsync(savedPath);
            using var savedDocument = JsonDocument.Parse(savedJson);

            Assert.False(
                savedDocument.RootElement.GetProperty("profile").TryGetProperty("removeUsingDirectives", out _));
        }
        finally
        {
            if (imported is not null)
            {
                foreach (ProfileLibraryEntry importedEntry in imported)
                    DeleteProfileFile(importedEntry.FilePath);
            }
        }
    }

    private static void AssertNoTemporaryFiles(string targetPath)
    {
        string directory = Path.GetDirectoryName(targetPath)!;
        string pattern = $".{Path.GetFileName(targetPath)}.*.tmp";

        Assert.Empty(Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly));
    }

    private static ProfileLibraryService CreateService()
    {
        return new ProfileLibraryService(new EmptyBuiltInProfilePresetProvider());
    }

    private static ProfileEditorViewModel CreateEditor()
    {
        return new ProfileEditorViewModel(new BuiltInFileTypeCatalog(), new NoOpProfileFilterRulesDialogService());
    }

    private static ProfileLibraryEntry CreateEntry(WorkspaceProfileDto profile)
    {
        return new ProfileLibraryEntry(
            Metadata: CreateMetadata(),
            Profile: profile,
            FilePath: null,
            Kind: ProfileEntryKind.User);
    }

    private static ProfileMetadataDto CreateMetadata()
    {
        return new ProfileMetadataDto(
            Id: "profile-test",
            Name: "Test Profile",
            Description: "Persistence test profile",
            CreatedAtUtc: new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc: new DateTime(2026, 8, 27, 1, 0, 0, DateTimeKind.Utc),
            IsBuiltIn: false,
            IsReadOnly: false);
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
            IncludeUnsupportedTextFiles: false,
            IncludeBuildTimestampMetadata: true,
            IncludeSessionNameMetadata: true,
            IncludeOutputPathMetadata: true,
            IncludeFileSummaryMetadata: true,
            SkippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
            IncludeSourceExcludedFiles: includeSourceExcludedFiles,
            SkippedFileCategories: skippedFileCategories);
    }

    private static SkippedFileCategorySelection CreateSkippedFileCategorySelection(bool includeSourceExclusions = false)
    {
        return new SkippedFileCategorySelection(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: false,
            IncludeProfileExclusions: true,
            IncludeManualExclusions: false,
            IncludeSourceExclusions: includeSourceExclusions,
            IncludeProcessingFailures: true,
            IncludeOther: false);
    }

    private static void DeleteProfileFile(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            File.Delete(filePath);
    }

    private static async Task<ProfileLibraryDocumentDto> DeserializeProfileDocumentAsync(string path)
    {
        await using FileStream stream = File.OpenRead(path);

        ProfileLibraryDocumentDto? document =
            await JsonSerializer.DeserializeAsync<ProfileLibraryDocumentDto>(stream, CreateJsonOptions());

        return Assert.IsType<ProfileLibraryDocumentDto>(document);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private sealed class EmptyBuiltInProfilePresetProvider : IBuiltInProfilePresetProvider
    {
        public IReadOnlyCollection<ProfileLibraryEntry> GetAll()
        {
            return [];
        }
    }

    private sealed class NoOpProfileFilterRulesDialogService : IProfileFilterRulesDialogService
    {
        public void Show(ProfileEditorViewModel editor)
        {
        }
    }
}