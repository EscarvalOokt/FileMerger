using System.IO;
using System.Text.Json;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Shared.Persistence;

namespace FileMerger.Wpf.Features.Profile.Services;

public sealed class ProfileLibraryService : IProfileLibraryService
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IBuiltInProfilePresetProvider _builtInProfilePresetProvider;

    public ProfileLibraryService(IBuiltInProfilePresetProvider builtInProfilePresetProvider)
    {
        ArgumentNullException.ThrowIfNull(builtInProfilePresetProvider);
        _builtInProfilePresetProvider = builtInProfilePresetProvider;
    }

    public string GetPrimaryProfilesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileMerger",
            "Profiles");
    }

    public async Task<IReadOnlyCollection<ProfileLibraryEntry>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        string directory = GetPrimaryProfilesDirectory();
        Directory.CreateDirectory(directory);

        List<ProfileLibraryEntry> result =
        [
            .. _builtInProfilePresetProvider.GetAll()
        ];

        IReadOnlyCollection<ProfileLibraryEntry> userEntries = await LoadUserEntriesAsync(directory, cancellationToken);

        result.AddRange(userEntries);

        return
        [
            .. result.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.IsBuiltIn ? 0 : 1)
                .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(x => x.UpdatedAtUtc ?? DateTime.MinValue)
        ];
    }

    public async Task<ProfileLibraryEntry> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Profile id cannot be empty.", nameof(id));

        IReadOnlyCollection<ProfileLibraryEntry> entries = await GetAllAsync(cancellationToken);

        return entries.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)) ??
               throw new InvalidOperationException($"Profile '{id}' was not found.");
    }

    public async Task<ProfileLibraryEntry> SaveAsync(
        ProfileLibraryEntry entry,
        bool saveAsNew = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string directory = GetPrimaryProfilesDirectory();
        Directory.CreateDirectory(directory);

        bool createNew = saveAsNew ||
                         entry.IsBuiltIn ||
                         entry.IsReadOnly ||
                         string.IsNullOrWhiteSpace(entry.FilePath) ||
                         !File.Exists(entry.FilePath);

        DateTime utcNow = DateTime.UtcNow;

        ProfileMetadataDto metadata = new(
            Id: createNew ? CreateNewProfileId() : NormalizeProfileId(entry.Id),
            Name: NormalizeProfileName(entry.DisplayName),
            Description: NormalizeDescription(entry.Description),
            CreatedAtUtc: createNew ? utcNow : entry.CreatedAtUtc ?? utcNow,
            UpdatedAtUtc: utcNow,
            IsBuiltIn: false,
            IsReadOnly: false);

        string filePath = createNew ? BuildUserProfilePath(directory, metadata) : entry.FilePath!;

        ProfileLibraryDocumentDto document = new(
            SchemaVersion: CurrentSchemaVersion,
            Metadata: metadata,
            Profile: entry.Profile);

        await WriteDocumentAsync(filePath, document, cancellationToken);

        return new ProfileLibraryEntry(
            Metadata: metadata,
            Profile: entry.Profile,
            FilePath: filePath,
            Kind: ProfileEntryKind.User);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ProfileLibraryEntry entry = await LoadAsync(id, cancellationToken);

        if (entry.IsReadOnly)
            throw new InvalidOperationException("Read-only profiles cannot be deleted.");

        if (string.IsNullOrWhiteSpace(entry.FilePath))
            return;

        if (File.Exists(entry.FilePath))
            File.Delete(entry.FilePath);
    }

    public async Task<IReadOnlyCollection<ProfileLibraryEntry>> ImportAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        IReadOnlyCollection<ProfileLibraryEntry> existingEntries = await GetAllAsync(cancellationToken);

        var usedNames = existingEntries.Select(x => x.DisplayName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> processedPaths = new(StringComparer.OrdinalIgnoreCase);
        List<ProfileLibraryEntry> importedEntries = [];

        foreach (string rawPath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(rawPath))
                continue;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(rawPath);
            }
            catch
            {
                continue;
            }

            if (!processedPaths.Add(fullPath))
                continue;

            ProfileLibraryDocumentDto? document = await TryReadProfileDocumentAsync(fullPath, cancellationToken);

            if (document is null)
                continue;

            string importedName = EnsureUniqueName(NormalizeProfileName(document.Metadata.Name), usedNames);

            DateTime utcNow = DateTime.UtcNow;

            ProfileLibraryEntry importedEntry = new(
                Metadata: new ProfileMetadataDto(
                    Id: CreateNewProfileId(),
                    Name: importedName,
                    Description: NormalizeDescription(document.Metadata.Description),
                    CreatedAtUtc: utcNow,
                    UpdatedAtUtc: utcNow,
                    IsBuiltIn: false,
                    IsReadOnly: false),
                Profile: document.Profile,
                FilePath: null,
                Kind: ProfileEntryKind.User);

            ProfileLibraryEntry savedEntry = await SaveAsync(importedEntry, saveAsNew: true, cancellationToken);

            importedEntries.Add(savedEntry);
        }

        return importedEntries;
    }

    public async Task ExportAsync(
        ProfileLibraryEntry entry,
        string targetFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(targetFilePath))
            throw new ArgumentException("Target file path cannot be empty.", nameof(targetFilePath));

        ProfileMetadataDto metadata = new(
            Id: NormalizeProfileId(entry.Id),
            Name: NormalizeProfileName(entry.DisplayName),
            Description: NormalizeDescription(entry.Description),
            CreatedAtUtc: entry.CreatedAtUtc,
            UpdatedAtUtc: entry.UpdatedAtUtc,
            IsBuiltIn: false,
            IsReadOnly: false);

        ProfileLibraryDocumentDto document = new(
            SchemaVersion: CurrentSchemaVersion,
            Metadata: metadata,
            Profile: entry.Profile);

        await WriteDocumentAsync(targetFilePath, document, cancellationToken);
    }

    private static async Task<IReadOnlyCollection<ProfileLibraryEntry>> LoadUserEntriesAsync(
        string directory,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directory))
            return [];

        List<ProfileLibraryEntry> result = [];

        foreach (string filePath in Directory
                     .EnumerateFiles(directory, "*.filemerger.profile.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProfileLibraryEntry? entry = await TryReadUserEntryAsync(filePath, cancellationToken);

            if (entry is not null)
                result.Add(entry);
        }

        return result;
    }

    private static async Task<ProfileLibraryEntry?> TryReadUserEntryAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        ProfileLibraryDocumentDto? document = await TryReadProfileDocumentAsync(filePath, cancellationToken);

        if (document is null)
            return null;

        ProfileMetadataDto metadata = new(
            Id: NormalizeProfileId(document.Metadata.Id),
            Name: NormalizeProfileName(document.Metadata.Name),
            Description: NormalizeDescription(document.Metadata.Description),
            CreatedAtUtc: document.Metadata.CreatedAtUtc,
            UpdatedAtUtc: document.Metadata.UpdatedAtUtc,
            IsBuiltIn: false,
            IsReadOnly: false);

        return new ProfileLibraryEntry(
            Metadata: metadata,
            Profile: document.Profile,
            FilePath: filePath,
            Kind: ProfileEntryKind.User);
    }

    private static async Task<ProfileLibraryDocumentDto?> TryReadProfileDocumentAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        try
        {
            await using FileStream stream = File.OpenRead(filePath);

            ProfileLibraryDocumentDto? document =
                await JsonSerializer.DeserializeAsync<ProfileLibraryDocumentDto>(
                    stream,
                    _jsonOptions,
                    cancellationToken);

            if (document is null)
                return null;

            if (document.SchemaVersion != CurrentSchemaVersion)
                return null;

            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (document.Metadata is null || document.Profile is null)
                return null;
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

            return document;
        }
        catch
        {
            return null;
        }
    }

    private static async Task WriteDocumentAsync(
        string filePath,
        ProfileLibraryDocumentDto document,
        CancellationToken cancellationToken)
    {
        await AtomicFileWriter.WriteAsync(
            filePath,
            (stream, token) => JsonSerializer.SerializeAsync(stream, document, _jsonOptions, token),
            cancellationToken);
    }

    private static string EnsureUniqueName(string baseName, HashSet<string> usedNames)
    {
        if (usedNames.Add(baseName))
            return baseName;

        string importedName = $"{baseName} (Imported)";
        if (usedNames.Add(importedName))
            return importedName;

        int index = 2;
        while (true)
        {
            string candidate = $"{baseName} (Imported {index})";
            if (usedNames.Add(candidate))
                return candidate;

            index++;
        }
    }

    private static string BuildUserProfilePath(string directory, ProfileMetadataDto metadata)
    {
        string safeName = MakeSafeFileName(metadata.Name, "Profile");
        string shortId = metadata.Id.Length > 8 ? metadata.Id[..8] : metadata.Id;

        string fileName = $"{safeName}.{shortId}.filemerger.profile.json";
        return Path.Combine(directory, fileName);
    }

    private static string NormalizeProfileId(string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? CreateNewProfileId() : id.Trim();
    }

    private static string NormalizeProfileName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Profile" : value.Trim();
    }

    private static string? NormalizeDescription(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string MakeSafeFileName(string? value, string fallback)
    {
        string candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            candidate = candidate.Replace(invalidChar, '_');

        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
    }

    private static string CreateNewProfileId()
    {
        return Guid.NewGuid().ToString("N");
    }
}