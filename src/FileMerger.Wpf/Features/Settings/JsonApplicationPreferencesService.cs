using System.IO;
using System.Text.Json;

namespace FileMerger.Wpf.Features.Settings;

public sealed class JsonApplicationPreferencesService : IApplicationPreferencesService
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationPreferencesStoragePathPolicy _pathPolicy;

    public JsonApplicationPreferencesService(ApplicationPreferencesStoragePathPolicy pathPolicy)
    {
        ArgumentNullException.ThrowIfNull(pathPolicy);
        _pathPolicy = pathPolicy;
    }

    public async Task<ApplicationPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        string filePath = _pathPolicy.GetStorageFilePath();

        if (!File.Exists(filePath))
            return ApplicationPreferences.Default;

        try
        {
            await using FileStream stream = File.OpenRead(filePath);

            ApplicationPreferencesDocumentDto? document =
                await JsonSerializer.DeserializeAsync<ApplicationPreferencesDocumentDto>(
                    stream,
                    JsonOptions,
                    cancellationToken);

            if (document is null || document.SchemaVersion != CurrentSchemaVersion || document.Preferences is null)
            {
                return ApplicationPreferences.Default;
            }

            return document.Preferences.ToModel();
        }
        catch (Exception ex) when (ex is JsonException ||
                                   ex is IOException ||
                                   ex is UnauthorizedAccessException ||
                                   ex is NotSupportedException)
        {
            return ApplicationPreferences.Default;
        }
    }

    public async Task SaveAsync(ApplicationPreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var normalizedPreferences = new ApplicationPreferences(
            preferences.IsPreviewLineWrapEnabledByDefault,
            preferences.PreviewDisplayCharacterLimit,
            preferences.CrashLogRetentionLimit);

        var document = new ApplicationPreferencesDocumentDto(
            SchemaVersion: CurrentSchemaVersion,
            Preferences: ApplicationPreferencesDto.FromModel(normalizedPreferences));

        string filePath = _pathPolicy.GetStorageFilePath();
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using FileStream stream = File.Create(filePath);

        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }
}