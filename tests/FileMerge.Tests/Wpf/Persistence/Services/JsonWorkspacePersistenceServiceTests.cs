using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Persistence.Services;

public sealed class JsonWorkspacePersistenceServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public JsonWorkspacePersistenceServiceTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(JsonWorkspacePersistenceServiceTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Write_Document_Root()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(sources: []);

        await service.SaveWorkspaceAsync(workspace, path);

        string json = await File.ReadAllTextAsync(path);

        Assert.Contains("\"document\"", json);
        Assert.Contains("\"sessionName\"", json);
        Assert.Contains("\"outputPath\"", json);
        Assert.Contains("\"sources\"", json);
        Assert.Contains("\"profile\"", json);
        Assert.Contains("\"inclusionOverrides\"", json);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Write_Profile_Metadata()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profileDisplayName: "Repository Profile",
            profileEntryId: "profile-repository",
            profileOriginEntryId: "profile-origin",
            profileOriginDisplayName: "Origin Profile");

        await service.SaveWorkspaceAsync(workspace, path);

        string json = await File.ReadAllTextAsync(path);

        Assert.Contains("\"profileDisplayName\"", json);
        Assert.Contains("\"profileEntryId\"", json);
        Assert.Contains("\"profileOriginEntryId\"", json);
        Assert.Contains("\"profileOriginDisplayName\"", json);
        Assert.Contains("Repository Profile", json);
        Assert.Contains("profile-repository", json);
        Assert.Contains("profile-origin", json);
        Assert.Contains("Origin Profile", json);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Write_Source_Exclusions()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
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
            ]);

        await service.SaveWorkspaceAsync(workspace, path);

        string json = await File.ReadAllTextAsync(path);

        Assert.Contains("\"exclusions\"", json);
        Assert.Contains("\"relativePath\"", json);
        Assert.Contains("\"type\"", json);
        Assert.Contains("\"isEnabled\"", json);
        Assert.Contains("bin", json);
        Assert.Contains(@"Secrets\\ApiKeys.cs", json);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Read_Source_Exclusions()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
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
            ]);

        string json = JsonSerializer.Serialize(workspace, CreateJsonOptions());
        await File.WriteAllTextAsync(path, json);

        WorkspaceDto result = await service.LoadWorkspaceAsync(path);

        WorkspaceSourceDto source = Assert.Single(result.Document.Sources);
        Assert.NotNull(source.Exclusions);
        Assert.Equal(2, source.Exclusions!.Count);

        Assert.Contains(source.Exclusions, x =>
            x is { RelativePath: "bin", Type: MergeSourceExclusionType.Directory, IsEnabled: true });

        Assert.Contains(source.Exclusions, x =>
            x is { RelativePath: @"Secrets\ApiKeys.cs", Type: MergeSourceExclusionType.File, IsEnabled: false });
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Read_Profile_Metadata()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profileDisplayName: "Repository Profile",
            profileEntryId: "profile-repository",
            profileOriginEntryId: "profile-origin",
            profileOriginDisplayName: "Origin Profile");

        string json = JsonSerializer.Serialize(workspace, CreateJsonOptions());
        await File.WriteAllTextAsync(path, json);

        WorkspaceDto result = await service.LoadWorkspaceAsync(path);

        Assert.Equal("Repository Profile", result.Document.ProfileDisplayName);
        Assert.Equal("profile-repository", result.Document.ProfileEntryId);
        Assert.Equal("profile-origin", result.Document.ProfileOriginEntryId);
        Assert.Equal("Origin Profile", result.Document.ProfileOriginDisplayName);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Default_Profile_Origin_Metadata_When_Missing()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profileDisplayName: "Repository Profile",
            profileEntryId: "profile-repository",
            profileOriginEntryId: "profile-origin",
            profileOriginDisplayName: "Origin Profile");

        string json = JsonSerializer.Serialize(workspace, CreateJsonOptions());
        var root = JsonNode.Parse(json)!;
        JsonObject document = root["document"]!.AsObject();
        document.Remove("profileOriginEntryId");
        document.Remove("profileOriginDisplayName");

        await File.WriteAllTextAsync(
            path,
            root.ToJsonString(CreateJsonOptions()));

        WorkspaceDto result = await service.LoadWorkspaceAsync(path);

        Assert.Null(result.Document.ProfileOriginEntryId);
        Assert.Null(result.Document.ProfileOriginDisplayName);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Reject_Old_Flat_Workspace_Format()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "old.filemerger.workspace.json");

        string json = """
                      {
                        "sessionName": "Old Workspace",
                        "outputPath": "D:\\Output\\merged.txt",
                        "sources": [],
                        "profile": {
                          "includeHeaderComment": false,
                          "includeFileSeparators": true,
                          "includeRelativePathInSeparator": true,
                          "trimTrailingEmptyLines": true,
                          "removeUsingDirectives": false,
                          "fileTypes": [],
                          "lineEndingMode": 0,
                          "sortMode": 1,
                          "inputEncodingMode": 0,
                          "preferredInputEncodingName": null,
                          "fallbackInputEncodingName": "windows-1251"
                        },
                        "inclusionOverrides": {}
                      }
                      """;

        await File.WriteAllTextAsync(path, json);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoadWorkspaceAsync(path));

        Assert.Equal("Workspace file is empty or invalid.", ex.Message);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Write_Profile_FilterRules()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profile: CreateProfile(
                filterRules:
                [
                    new WorkspaceFileFilterRuleDto(
                        Mode: FilterMode.Exclude,
                        Target: FilterTarget.DirectorySegment,
                        PatternType: RulePatternType.Exact,
                        Pattern: "Library",
                        IsEnabled: true,
                        Description: "Exclude Unity Library directory",
                        IsUserEditable: true)
                ]));

        await service.SaveWorkspaceAsync(workspace, path);

        string json = await File.ReadAllTextAsync(path);

        Assert.Contains("\"filterRules\"", json);
        Assert.Contains("\"mode\"", json);
        Assert.Contains("\"target\"", json);
        Assert.Contains("\"patternType\"", json);
        Assert.Contains("\"pattern\"", json);
        Assert.Contains("\"isUserEditable\"", json);
        Assert.Contains("Library", json);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Read_Profile_FilterRules()
    {
        var service = new JsonWorkspacePersistenceService();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profile: CreateProfile(
                filterRules:
                [
                    new WorkspaceFileFilterRuleDto(
                        Mode: FilterMode.Exclude,
                        Target: FilterTarget.DirectorySegment,
                        PatternType: RulePatternType.Exact,
                        Pattern: "Library",
                        IsEnabled: true,
                        Description: "Exclude Unity Library directory",
                        IsUserEditable: true)
                ]));

        string json = JsonSerializer.Serialize(workspace, CreateJsonOptions());
        await File.WriteAllTextAsync(path, json);

        WorkspaceDto result = await service.LoadWorkspaceAsync(path);

        WorkspaceFileFilterRuleDto rule = Assert.Single(result.Document.Profile.FilterRules!);

        Assert.Equal(FilterMode.Exclude, rule.Mode);
        Assert.Equal(FilterTarget.DirectorySegment, rule.Target);
        Assert.Equal(RulePatternType.Exact, rule.PatternType);
        Assert.Equal("Library", rule.Pattern);
        Assert.True(rule.IsEnabled);
        Assert.Equal("Exclude Unity Library directory", rule.Description);
        Assert.True(rule.IsUserEditable);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Write_Unsupported_Text_Fallback_Options()
    {
        JsonWorkspacePersistenceService service = new();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profile: CreateProfile(
                includeUnsupportedTextFiles: true,
                unsupportedTextMaxFileSizeBytes: 2048,
                unsupportedTextProbeSizeBytes: 512,
                unsupportedTextMaxControlCharacterRatio: 0.20));

        await service.SaveWorkspaceAsync(workspace, path);

        string json = await File.ReadAllTextAsync(path);

        Assert.Contains("\"includeUnsupportedTextFiles\"", json);
        Assert.Contains("\"unsupportedTextMaxFileSizeBytes\"", json);
        Assert.Contains("\"unsupportedTextProbeSizeBytes\"", json);
        Assert.Contains("\"unsupportedTextMaxControlCharacterRatio\"", json);
        Assert.Contains("2048", json);
        Assert.Contains("512", json);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Read_Unsupported_Text_Fallback_Options()
    {
        JsonWorkspacePersistenceService service = new();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profile: CreateProfile(
                includeUnsupportedTextFiles: true,
                unsupportedTextMaxFileSizeBytes: 2048,
                unsupportedTextProbeSizeBytes: 512,
                unsupportedTextMaxControlCharacterRatio: 0.20));

        string json = JsonSerializer.Serialize(workspace, CreateJsonOptions());
        await File.WriteAllTextAsync(path, json);

        WorkspaceDto result = await service.LoadWorkspaceAsync(path);

        WorkspaceProfileDto profile = result.Document.Profile;

        Assert.True(profile.IncludeUnsupportedTextFiles);
        Assert.Equal(2048, profile.UnsupportedTextMaxFileSizeBytes);
        Assert.Equal(512, profile.UnsupportedTextProbeSizeBytes);
        Assert.Equal(0.20, profile.UnsupportedTextMaxControlCharacterRatio);
    }

    [Fact]
    public async Task SaveWorkspaceAsync_Should_Write_Output_Metadata_Options()
    {
        JsonWorkspacePersistenceService service = new();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profile: CreateProfile(
                includeBuildTimestampMetadata: false,
                includeSessionNameMetadata: false,
                includeOutputPathMetadata: false,
                includeFileSummaryMetadata: false,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                includeSourceExcludedFiles: true));

        await service.SaveWorkspaceAsync(workspace, path);

        string json = await File.ReadAllTextAsync(path);

        Assert.Contains("\"includeBuildTimestampMetadata\"", json);
        Assert.Contains("\"includeSessionNameMetadata\"", json);
        Assert.Contains("\"includeOutputPathMetadata\"", json);
        Assert.Contains("\"includeFileSummaryMetadata\"", json);
        Assert.Contains("\"skippedFilesMetadataMode\"", json);
        Assert.Contains("\"includeSourceExcludedFiles\"", json);

        using var document = JsonDocument.Parse(json);

        JsonElement profileElement = document
            .RootElement
            .GetProperty("document")
            .GetProperty("profile");

        Assert.False(profileElement.GetProperty("includeBuildTimestampMetadata").GetBoolean());
        Assert.False(profileElement.GetProperty("includeSessionNameMetadata").GetBoolean());
        Assert.False(profileElement.GetProperty("includeOutputPathMetadata").GetBoolean());
        Assert.False(profileElement.GetProperty("includeFileSummaryMetadata").GetBoolean());
        Assert.True(profileElement.GetProperty("includeSourceExcludedFiles").GetBoolean());

        Assert.Equal(
            (int)SkippedFilesMetadataMode.Detailed,
            profileElement.GetProperty("skippedFilesMetadataMode").GetInt32());
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Read_Output_Metadata_Options()
    {
        JsonWorkspacePersistenceService service = new();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profile: CreateProfile(
                includeBuildTimestampMetadata: false,
                includeSessionNameMetadata: false,
                includeOutputPathMetadata: false,
                includeFileSummaryMetadata: false,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
                includeSourceExcludedFiles: true));

        string json = JsonSerializer.Serialize(workspace, CreateJsonOptions());
        await File.WriteAllTextAsync(path, json);

        WorkspaceDto result = await service.LoadWorkspaceAsync(path);

        WorkspaceProfileDto profile = result.Document.Profile;

        Assert.False(profile.IncludeBuildTimestampMetadata);
        Assert.False(profile.IncludeSessionNameMetadata);
        Assert.False(profile.IncludeOutputPathMetadata);
        Assert.False(profile.IncludeFileSummaryMetadata);
        Assert.True(profile.IncludeSourceExcludedFiles);
        Assert.Equal(SkippedFilesMetadataMode.Simple, profile.SkippedFilesMetadataMode);
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_Default_Source_Excluded_Files_Option_To_False_When_Missing()
    {
        JsonWorkspacePersistenceService service = new();
        string path = Path.Combine(_tempRoot, "workspace.filemerger.workspace.json");

        WorkspaceDto workspace = CreateWorkspace(
            sources: [],
            profile: CreateProfile(includeSourceExcludedFiles: true));

        string json = JsonSerializer.Serialize(workspace, CreateJsonOptions());
        var root = JsonNode.Parse(json)!;
        JsonObject profile = root["document"]!["profile"]!.AsObject();
        profile.Remove("includeSourceExcludedFiles");

        await File.WriteAllTextAsync(
            path,
            root.ToJsonString(CreateJsonOptions()));

        WorkspaceDto result = await service.LoadWorkspaceAsync(path);

        Assert.False(result.Document.Profile.IncludeSourceExcludedFiles);
    }

    private static WorkspaceDto CreateWorkspace(
        List<WorkspaceSourceDto> sources,
        WorkspaceProfileDto? profile = null,
        string? profileDisplayName = null,
        string? profileEntryId = null,
        string? profileOriginEntryId = null,
        string? profileOriginDisplayName = null)
    {
        return new WorkspaceDto(
            Document: new WorkspaceDocumentDto(
                SessionName: "Test Workspace",
                OutputPath: @"D:\Output\merged.txt",
                Sources: sources,
                Profile: profile ?? CreateProfile(),
                InclusionOverrides: [],
                ProfileDisplayName: profileDisplayName,
                ProfileEntryId: profileEntryId,
                ProfileOriginEntryId: profileOriginEntryId,
                ProfileOriginDisplayName: profileOriginDisplayName));
    }

    private static WorkspaceProfileDto CreateProfile(
        List<WorkspaceFileFilterRuleDto>? filterRules = null,
        bool includeUnsupportedTextFiles = false,
        long unsupportedTextMaxFileSizeBytes = UnsupportedTextFallbackOptions.DefaultMaxFileSizeBytes,
        int unsupportedTextProbeSizeBytes = UnsupportedTextFallbackOptions.DefaultProbeSizeBytes,
        double unsupportedTextMaxControlCharacterRatio = UnsupportedTextFallbackOptions.DefaultMaxControlCharacterRatio,
        bool includeBuildTimestampMetadata = true,
        bool includeSessionNameMetadata = true,
        bool includeOutputPathMetadata = true,
        bool includeFileSummaryMetadata = true,
        SkippedFilesMetadataMode skippedFilesMetadataMode = SkippedFilesMetadataMode.None,
        bool includeSourceExcludedFiles = false)
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: false,
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
            FilterRules: filterRules,
            IncludeUnsupportedTextFiles: includeUnsupportedTextFiles,
            UnsupportedTextMaxFileSizeBytes: unsupportedTextMaxFileSizeBytes,
            UnsupportedTextProbeSizeBytes: unsupportedTextProbeSizeBytes,
            UnsupportedTextMaxControlCharacterRatio: unsupportedTextMaxControlCharacterRatio,
            IncludeBuildTimestampMetadata: includeBuildTimestampMetadata,
            IncludeSessionNameMetadata: includeSessionNameMetadata,
            IncludeOutputPathMetadata: includeOutputPathMetadata,
            IncludeFileSummaryMetadata: includeFileSummaryMetadata,
            SkippedFilesMetadataMode: skippedFilesMetadataMode,
            IncludeSourceExcludedFiles: includeSourceExcludedFiles);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}