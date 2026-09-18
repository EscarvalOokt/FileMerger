using System.IO;
using System.Text.Json;
using FileMerger.Wpf.Features.Settings;

namespace FileMerger.Tests.Wpf.Features.Settings;

public sealed class JsonApplicationPreferencesServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public JsonApplicationPreferencesServiceTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(JsonApplicationPreferencesServiceTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Default_When_Storage_File_Is_Missing()
    {
        JsonApplicationPreferencesService service = CreateService();

        ApplicationPreferences result = await service.LoadAsync();

        Assert.Equal(ApplicationPreferences.Default, result);
    }

    [Fact]
    public async Task SaveAsync_Should_Write_Preferences_File()
    {
        JsonApplicationPreferencesService service = CreateService();

        await service.SaveAsync(new ApplicationPreferences(true, 250_000, 50));

        string storagePath = CreatePathPolicy().GetStorageFilePath();
        string json = await File.ReadAllTextAsync(storagePath);

        Assert.True(File.Exists(storagePath));
        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"isPreviewLineWrapEnabledByDefault\": true", json);
        Assert.Contains("\"previewDisplayCharacterLimit\": 250000", json);
        Assert.Contains("\"crashLogRetentionLimit\": 50", json);
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Saved_Preferences()
    {
        JsonApplicationPreferencesService service = CreateService();
        var expected = new ApplicationPreferences(true, 750_000, 40);

        await service.SaveAsync(expected);

        ApplicationPreferences result = await CreateService().LoadAsync();

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Default_When_File_Is_Corrupted()
    {
        JsonApplicationPreferencesService service = CreateService();
        await WriteStorageFileAsync("{ invalid json");

        ApplicationPreferences result = await service.LoadAsync();

        Assert.Equal(ApplicationPreferences.Default, result);
    }

    [Fact]
    public async Task LoadAsync_Should_Return_Default_When_SchemaVersion_Is_Unsupported()
    {
        JsonApplicationPreferencesService service = CreateService();
        await WriteStorageFileAsync(
            """
            {
              "schemaVersion": 999,
              "preferences": {
                "isPreviewLineWrapEnabledByDefault": true,
                "previewDisplayCharacterLimit": 250000,
                "crashLogRetentionLimit": 50
              }
            }
            """);

        ApplicationPreferences result = await service.LoadAsync();

        Assert.Equal(ApplicationPreferences.Default, result);
    }

    [Fact]
    public async Task LoadAsync_Should_Use_Defaults_For_Missing_Preference_Values()
    {
        JsonApplicationPreferencesService service = CreateService();
        await WriteStorageFileAsync(
            """
            {
              "schemaVersion": 1,
              "preferences": {
                "isPreviewLineWrapEnabledByDefault": true
              }
            }
            """);

        ApplicationPreferences result = await service.LoadAsync();

        Assert.True(result.IsPreviewLineWrapEnabledByDefault);
        Assert.Equal(ApplicationPreferences.DefaultPreviewDisplayCharacterLimit, result.PreviewDisplayCharacterLimit);
        Assert.Equal(ApplicationPreferences.DefaultCrashLogRetentionLimit, result.CrashLogRetentionLimit);
    }

    [Fact]
    public async Task LoadAsync_Should_Normalize_Invalid_PreviewDisplayCharacterLimit()
    {
        JsonApplicationPreferencesService service = CreateService();
        await WriteStorageFileAsync(
            """
            {
              "schemaVersion": 1,
              "preferences": {
                "previewDisplayCharacterLimit": -100
              }
            }
            """);

        ApplicationPreferences result = await service.LoadAsync();

        Assert.Equal(ApplicationPreferences.MinimumPreviewDisplayCharacterLimit, result.PreviewDisplayCharacterLimit);
    }

    [Fact]
    public async Task LoadAsync_Should_Normalize_Invalid_CrashLogRetentionLimit()
    {
        JsonApplicationPreferencesService service = CreateService();
        await WriteStorageFileAsync(
            """
            {
              "schemaVersion": 1,
              "preferences": {
                "crashLogRetentionLimit": 9999
              }
            }
            """);

        ApplicationPreferences result = await service.LoadAsync();

        Assert.Equal(ApplicationPreferences.MaximumCrashLogRetentionLimit, result.CrashLogRetentionLimit);
    }

    [Fact]
    public async Task SaveAsync_Should_Normalize_Values_Before_Writing()
    {
        JsonApplicationPreferencesService service = CreateService();
        var preferences = new ApplicationPreferences(true, int.MinValue, int.MaxValue);

        await service.SaveAsync(preferences);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(CreatePathPolicy().GetStorageFilePath()));

        JsonElement savedPreferences = document.RootElement.GetProperty("preferences");

        Assert.Equal(
            ApplicationPreferences.MinimumPreviewDisplayCharacterLimit,
            savedPreferences.GetProperty("previewDisplayCharacterLimit").GetInt32());
        Assert.Equal(
            ApplicationPreferences.MaximumCrashLogRetentionLimit,
            savedPreferences.GetProperty("crashLogRetentionLimit").GetInt32());
    }

    [Fact]
    public async Task SaveAsync_Should_Create_Storage_Directory()
    {
        JsonApplicationPreferencesService service = CreateService();
        string storageDirectory = Path.GetDirectoryName(CreatePathPolicy().GetStorageFilePath())!;

        Assert.False(Directory.Exists(storageDirectory));

        await service.SaveAsync(ApplicationPreferences.Default);

        Assert.True(Directory.Exists(storageDirectory));
    }

    private JsonApplicationPreferencesService CreateService()
    {
        return new JsonApplicationPreferencesService(CreatePathPolicy());
    }

    private ApplicationPreferencesStoragePathPolicy CreatePathPolicy()
    {
        return new ApplicationPreferencesStoragePathPolicy(_tempRoot);
    }

    private async Task WriteStorageFileAsync(string json)
    {
        string storagePath = CreatePathPolicy().GetStorageFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
        await File.WriteAllTextAsync(storagePath, json);
    }
}