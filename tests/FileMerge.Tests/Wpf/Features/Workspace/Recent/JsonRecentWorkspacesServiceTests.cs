using System.IO;
using FileMerger.Wpf.Features.Workspace.Recent;

namespace FileMerger.Tests.Wpf.Features.Workspace.Recent;

public sealed class JsonRecentWorkspacesServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public JsonRecentWorkspacesServiceTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(JsonRecentWorkspacesServiceTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task GetRecentWorkspacesAsync_Should_Return_Empty_When_Storage_File_Is_Missing()
    {
        JsonRecentWorkspacesService service = CreateService();

        IReadOnlyCollection<RecentWorkspaceEntry> result = await service.GetRecentWorkspacesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddOrUpdateAsync_Should_Add_New_Entry()
    {
        ManualTimeProvider timeProvider = new();
        JsonRecentWorkspacesService service = CreateService(timeProvider);

        string workspacePath = CreateWorkspaceFile("workspace.filemerger.workspace.json");

        await service.AddOrUpdateAsync(workspacePath);

        RecentWorkspaceEntry entry = Assert.Single(await service.GetRecentWorkspacesAsync());

        Assert.Equal(Path.GetFullPath(workspacePath), entry.FilePath);
        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime, entry.LastUsedAtUtc);
        Assert.True(entry.Exists);
        Assert.False(entry.IsMissing);
    }

    [Fact]
    public async Task AddOrUpdateAsync_Should_Update_Timestamp_And_Move_Existing_Entry_To_Top()
    {
        ManualTimeProvider timeProvider = new();
        JsonRecentWorkspacesService service = CreateService(timeProvider);

        string firstPath = CreateWorkspaceFile("first.filemerger.workspace.json");
        string secondPath = CreateWorkspaceFile("second.filemerger.workspace.json");

        timeProvider.SetUtcNow(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        await service.AddOrUpdateAsync(firstPath);

        timeProvider.SetUtcNow(new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc));
        await service.AddOrUpdateAsync(secondPath);

        timeProvider.SetUtcNow(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        await service.AddOrUpdateAsync(firstPath);

        RecentWorkspaceEntry[] result = [.. await service.GetRecentWorkspacesAsync()];

        Assert.Equal(2, result.Length);
        Assert.Equal(Path.GetFullPath(firstPath), result[0].FilePath);
        Assert.Equal(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), result[0].LastUsedAtUtc);
        Assert.Equal(Path.GetFullPath(secondPath), result[1].FilePath);
    }

    [Fact]
    public async Task AddOrUpdateAsync_Should_Deduplicate_By_Normalized_Path_Case_Insensitively()
    {
        ManualTimeProvider timeProvider = new();
        JsonRecentWorkspacesService service = CreateService(timeProvider);

        string workspacePath = CreateWorkspaceFile("Project.filemerger.workspace.json");

        await service.AddOrUpdateAsync(workspacePath);

        timeProvider.SetUtcNow(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        await service.AddOrUpdateAsync(workspacePath.ToUpperInvariant());

        RecentWorkspaceEntry entry = Assert.Single(await service.GetRecentWorkspacesAsync());

        Assert.Equal(Path.GetFullPath(workspacePath.ToUpperInvariant()), entry.FilePath);
        Assert.Equal(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), entry.LastUsedAtUtc);
    }

    [Fact]
    public async Task AddOrUpdateAsync_Should_Trim_To_MaxEntries()
    {
        ManualTimeProvider timeProvider = new();
        JsonRecentWorkspacesService service = CreateService(timeProvider);

        for (int i = 0; i < JsonRecentWorkspacesService.MaxEntries + 3; i++)
        {
            timeProvider.SetUtcNow(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc).AddMinutes(i));
            await service.AddOrUpdateAsync(CreateWorkspaceFile($"workspace-{i}.filemerger.workspace.json"));
        }

        RecentWorkspaceEntry[] result = [.. await service.GetRecentWorkspacesAsync()];

        Assert.Equal(JsonRecentWorkspacesService.MaxEntries, result.Length);
        Assert.DoesNotContain(result, x => x.FilePath.EndsWith("workspace-0.filemerger.workspace.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, x => x.FilePath.EndsWith("workspace-12.filemerger.workspace.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetRecentWorkspacesAsync_Should_Mark_Missing_Workspace_As_Missing()
    {
        JsonRecentWorkspacesService service = CreateService();

        string missingPath = Path.Combine(_tempRoot, "missing.filemerger.workspace.json");

        await service.AddOrUpdateAsync(missingPath);

        RecentWorkspaceEntry entry = Assert.Single(await service.GetRecentWorkspacesAsync());

        Assert.Equal(Path.GetFullPath(missingPath), entry.FilePath);
        Assert.False(entry.Exists);
        Assert.True(entry.IsMissing);
    }

    [Fact]
    public async Task GetRecentWorkspacesAsync_Should_Not_Remove_Missing_Workspace()
    {
        JsonRecentWorkspacesService service = CreateService();

        string missingPath = Path.Combine(_tempRoot, "missing.filemerger.workspace.json");

        await service.AddOrUpdateAsync(missingPath);

        RecentWorkspaceEntry firstRead = Assert.Single(await service.GetRecentWorkspacesAsync());
        RecentWorkspaceEntry secondRead = Assert.Single(await service.GetRecentWorkspacesAsync());

        Assert.False(firstRead.Exists);
        Assert.False(secondRead.Exists);
        Assert.Equal(firstRead.FilePath, secondRead.FilePath);
    }

    [Fact]
    public async Task Service_Should_Persist_And_Reload_Entries()
    {
        ManualTimeProvider timeProvider = new();
        JsonRecentWorkspacesService firstService = CreateService(timeProvider);

        string workspacePath = CreateWorkspaceFile("persisted.filemerger.workspace.json");

        timeProvider.SetUtcNow(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        await firstService.AddOrUpdateAsync(workspacePath);

        JsonRecentWorkspacesService secondService = CreateService();

        RecentWorkspaceEntry entry = Assert.Single(await secondService.GetRecentWorkspacesAsync());

        Assert.Equal(Path.GetFullPath(workspacePath), entry.FilePath);
        Assert.Equal(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), entry.LastUsedAtUtc);
        Assert.True(entry.Exists);
    }

    [Fact]
    public async Task RemoveAsync_Should_Remove_Entry_By_Normalized_Path()
    {
        JsonRecentWorkspacesService service = CreateService();

        string firstPath = CreateWorkspaceFile("first.filemerger.workspace.json");
        string secondPath = CreateWorkspaceFile("second.filemerger.workspace.json");

        await service.AddOrUpdateAsync(firstPath);
        await service.AddOrUpdateAsync(secondPath);

        await service.RemoveAsync(firstPath.ToUpperInvariant());

        RecentWorkspaceEntry entry = Assert.Single(await service.GetRecentWorkspacesAsync());

        Assert.Equal(Path.GetFullPath(secondPath), entry.FilePath);
    }

    [Fact]
    public async Task ClearAsync_Should_Remove_All_Entries()
    {
        JsonRecentWorkspacesService service = CreateService();

        await service.AddOrUpdateAsync(CreateWorkspaceFile("first.filemerger.workspace.json"));
        await service.AddOrUpdateAsync(CreateWorkspaceFile("second.filemerger.workspace.json"));

        await service.ClearAsync();

        Assert.Empty(await service.GetRecentWorkspacesAsync());
    }

    [Fact]
    public async Task GetRecentWorkspacesAsync_Should_Return_Empty_When_File_Is_Corrupted()
    {
        JsonRecentWorkspacesService service = CreateService();

        string storagePath = CreatePathPolicy().GetStorageFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
        await File.WriteAllTextAsync(storagePath, "{ invalid json");

        IReadOnlyCollection<RecentWorkspaceEntry> result = await service.GetRecentWorkspacesAsync();

        Assert.Empty(result);
    }

    private JsonRecentWorkspacesService CreateService(ManualTimeProvider? timeProvider = null)
    {
        return new JsonRecentWorkspacesService(
            CreatePathPolicy(),
            timeProvider ?? new ManualTimeProvider());
    }

    private RecentWorkspacesStoragePathPolicy CreatePathPolicy()
    {
        return new RecentWorkspacesStoragePathPolicy(_tempRoot);
    }

    private string CreateWorkspaceFile(string fileName)
    {
        string path = Path.Combine(_tempRoot, fileName);
        File.WriteAllText(path, "{}");
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void SetUtcNow(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
        }
    }
}