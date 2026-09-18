using System.IO;
using System.Text.Json;
using FileMerger.Wpf.Shared.Persistence;

namespace FileMerger.Wpf.Features.Workspace;

public sealed class JsonWorkspacePersistenceService : IWorkspacePersistenceService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SaveWorkspaceAsync(
        WorkspaceDto workspace,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        if (workspace.Document is null)
            throw new InvalidOperationException("Workspace document is missing.");

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        await AtomicFileWriter.WriteAsync(
            filePath,
            (stream, token) => JsonSerializer.SerializeAsync(stream, workspace, _jsonOptions, token),
            cancellationToken);
    }

    public async Task<WorkspaceDto> LoadWorkspaceAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        await using FileStream stream = File.OpenRead(filePath);
        WorkspaceDto? dto =
            await JsonSerializer.DeserializeAsync<WorkspaceDto>(stream, _jsonOptions, cancellationToken);

        if (dto?.Document is null)
            throw new InvalidOperationException("Workspace file is empty or invalid.");

        return dto;
    }
}