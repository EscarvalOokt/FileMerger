using System.IO;
using System.Text.Json;

namespace FileMerger.Wpf.Features.Workspace.Recent;

public sealed class JsonRecentWorkspacesService : IRecentWorkspacesService
{
    private const int CurrentSchemaVersion = 1;

    public const int MaxEntries = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RecentWorkspacesStoragePathPolicy _pathPolicy;
    private readonly TimeProvider _timeProvider;

    public JsonRecentWorkspacesService(RecentWorkspacesStoragePathPolicy pathPolicy)
        : this(pathPolicy, TimeProvider.System)
    {
    }

    public JsonRecentWorkspacesService(
        RecentWorkspacesStoragePathPolicy pathPolicy,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(pathPolicy);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _pathPolicy = pathPolicy;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyCollection<RecentWorkspaceEntry>> GetRecentWorkspacesAsync(
        CancellationToken cancellationToken = default)
    {
        RecentWorkspacesDocumentDto document = await LoadDocumentOrDefaultAsync(cancellationToken);

        return
        [
            .. NormalizeEntries(document.Entries)
                .Select(x => new RecentWorkspaceEntry(
                    FilePath: x.FilePath,
                    LastUsedAtUtc: x.LastUsedAtUtc,
                    Exists: File.Exists(x.FilePath)))
        ];
    }

    public async Task AddOrUpdateAsync(
        string workspaceFilePath,
        CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePathOrThrow(workspaceFilePath, nameof(workspaceFilePath));

        RecentWorkspacesDocumentDto document = await LoadDocumentOrDefaultAsync(cancellationToken);
        List<RecentWorkspaceEntryDto> entries = NormalizeEntries(document.Entries);

        entries.RemoveAll(x => PathsEqual(x.FilePath, normalizedPath));

        entries.Insert(0, new RecentWorkspaceEntryDto(
            FilePath: normalizedPath,
            LastUsedAtUtc: _timeProvider.GetUtcNow().UtcDateTime));

        entries = [.. entries.Take(MaxEntries)];

        await SaveDocumentAsync(
            new RecentWorkspacesDocumentDto(CurrentSchemaVersion, entries),
            cancellationToken);
    }

    public async Task RemoveAsync(
        string workspaceFilePath,
        CancellationToken cancellationToken = default)
    {
        string normalizedPath = NormalizePathOrThrow(workspaceFilePath, nameof(workspaceFilePath));

        RecentWorkspacesDocumentDto document = await LoadDocumentOrDefaultAsync(cancellationToken);
        List<RecentWorkspaceEntryDto> entries = NormalizeEntries(document.Entries);

        entries.RemoveAll(x => PathsEqual(x.FilePath, normalizedPath));

        await SaveDocumentAsync(
            new RecentWorkspacesDocumentDto(CurrentSchemaVersion, entries),
            cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await SaveDocumentAsync(CreateEmptyDocument(), cancellationToken);
    }

    private async Task<RecentWorkspacesDocumentDto> LoadDocumentOrDefaultAsync(
        CancellationToken cancellationToken)
    {
        string filePath = _pathPolicy.GetStorageFilePath();

        if (!File.Exists(filePath))
            return CreateEmptyDocument();

        try
        {
            await using FileStream stream = File.OpenRead(filePath);

            RecentWorkspacesDocumentDto? document =
                await JsonSerializer.DeserializeAsync<RecentWorkspacesDocumentDto>(
                    stream,
                    JsonOptions,
                    cancellationToken);

            if (document is null)
                return CreateEmptyDocument();

            return new RecentWorkspacesDocumentDto(
                SchemaVersion: document.SchemaVersion,
                Entries: NormalizeEntries(document.Entries));
        }
        catch (Exception ex) when (
            ex is JsonException ||
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is NotSupportedException)
        {
            return CreateEmptyDocument();
        }
    }

    private async Task SaveDocumentAsync(
        RecentWorkspacesDocumentDto document,
        CancellationToken cancellationToken)
    {
        string filePath = _pathPolicy.GetStorageFilePath();
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using FileStream stream = File.Create(filePath);

        await JsonSerializer.SerializeAsync(
            stream,
            document,
            JsonOptions,
            cancellationToken);
    }

    private static RecentWorkspacesDocumentDto CreateEmptyDocument()
    {
        return new RecentWorkspacesDocumentDto(CurrentSchemaVersion, []);
    }

    private static List<RecentWorkspaceEntryDto> NormalizeEntries(
        IEnumerable<RecentWorkspaceEntryDto>? entries)
    {
        Dictionary<string, RecentWorkspaceEntryDto> byPath = new(StringComparer.OrdinalIgnoreCase);

        foreach (RecentWorkspaceEntryDto? entry in entries ?? [])
        {
            if (entry is null)
                continue;

            string? normalizedPath = TryNormalizePath(entry.FilePath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
                continue;

            DateTime lastUsedAtUtc = EnsureUtc(entry.LastUsedAtUtc);

            if (!byPath.TryGetValue(normalizedPath, out RecentWorkspaceEntryDto? existing) ||
                lastUsedAtUtc > existing.LastUsedAtUtc)
            {
                byPath[normalizedPath] = new RecentWorkspaceEntryDto(
                    FilePath: normalizedPath,
                    LastUsedAtUtc: lastUsedAtUtc);
            }
        }

        return
        [
            .. byPath.Values
                .OrderByDescending(x => x.LastUsedAtUtc)
                .Take(MaxEntries)
        ];
    }

    private static string NormalizePathOrThrow(string path, string paramName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Workspace file path cannot be empty.", paramName);

        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch (Exception ex) when (
            ex is ArgumentException ||
            ex is NotSupportedException ||
            ex is PathTooLongException)
        {
            throw new ArgumentException("Workspace file path is invalid.", paramName, ex);
        }
    }

    private static string? TryNormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch
        {
            return null;
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}