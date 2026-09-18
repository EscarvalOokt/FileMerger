using System.IO;
using FileMerger.Wpf.Shared.Dialogs;

namespace FileMerger.Wpf.Features.Workspace;

public sealed class WorkspaceCoordinator : IWorkspaceCoordinator
{
    private const string WorkspaceFilter =
        "Workspace files (*.filemerger.workspace.json)|*.filemerger.workspace.json|JSON files (*.json)|*.json|All files (*.*)|*.*";

    private readonly IOpenFileDialogService _openFileDialogService;
    private readonly ISaveFileDialogService _saveFileDialogService;

    private readonly IWorkspacePersistenceService _workspacePersistenceService;

    public WorkspaceCoordinator(
        IWorkspacePersistenceService workspacePersistenceService,
        ISaveFileDialogService saveFileDialogService,
        IOpenFileDialogService openFileDialogService)
    {
        ArgumentNullException.ThrowIfNull(workspacePersistenceService);
        ArgumentNullException.ThrowIfNull(saveFileDialogService);
        ArgumentNullException.ThrowIfNull(openFileDialogService);

        _workspacePersistenceService = workspacePersistenceService;
        _saveFileDialogService = saveFileDialogService;
        _openFileDialogService = openFileDialogService;
    }

    public async Task<bool> SaveWorkspaceAsync(
        WorkspaceDocumentViewModel document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(document.WorkspaceFilePath))
            return await SaveWorkspaceAsAsync(document, cancellationToken);

        await SaveWorkspaceToPathAsync(document, document.WorkspaceFilePath, cancellationToken);

        return true;
    }

    public async Task<bool> SaveWorkspaceAsAsync(
        WorkspaceDocumentViewModel document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string initialPath = BuildInitialWorkspacePath(document);
        string? path = _saveFileDialogService.SelectSaveFilePath(initialPath, WorkspaceFilter);

        if (string.IsNullOrWhiteSpace(path))
            return false;

        await SaveWorkspaceToPathAsync(document, path, cancellationToken);

        document.WorkspaceFilePath = path;
        return true;
    }

    public async Task<bool> LoadWorkspaceAsync(
        WorkspaceDocumentViewModel document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        string initialPath = BuildOpenWorkspaceInitialPath();
        string? path = _openFileDialogService.SelectFile(initialPath, WorkspaceFilter);

        if (string.IsNullOrWhiteSpace(path))
            return false;

        return await LoadWorkspaceFromPathAsync(document, path, cancellationToken);
    }

    public async Task<bool> LoadWorkspaceFromPathAsync(
        WorkspaceDocumentViewModel document,
        string workspaceFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(workspaceFilePath))
            throw new ArgumentException("Workspace file path cannot be empty.", nameof(workspaceFilePath));

        string normalizedPath = Path.GetFullPath(workspaceFilePath.Trim());

        WorkspaceDto dto = await _workspacePersistenceService.LoadWorkspaceAsync(normalizedPath, cancellationToken);

        WorkspaceDocumentMapper.Apply(document, dto);

        document.WorkspaceFilePath = normalizedPath;
        return true;
    }

    private async Task SaveWorkspaceToPathAsync(
        WorkspaceDocumentViewModel document,
        string path,
        CancellationToken cancellationToken)
    {
        WorkspaceDto dto = WorkspaceDocumentMapper.Capture(document);

        await _workspacePersistenceService.SaveWorkspaceAsync(dto, path, cancellationToken);
    }

    private static string BuildInitialWorkspacePath(WorkspaceDocumentViewModel document)
    {
        if (!string.IsNullOrWhiteSpace(document.WorkspaceFilePath))
            return document.WorkspaceFilePath;

        string fileName =
            $"{MakeSafeFileName(document.SessionSettings.SessionName, "DefaultSession")}.filemerger.workspace.json";
        return Path.Combine(GetDefaultStorageDirectory(), fileName);
    }

    private static string BuildOpenWorkspaceInitialPath()
    {
        string directory = GetDefaultStorageDirectory();
        Directory.CreateDirectory(directory);

        return directory;
    }

    private static string GetDefaultStorageDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileMerger",
            "Workspaces");
    }

    private static string MakeSafeFileName(string? value, string fallback)
    {
        string candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            candidate = candidate.Replace(invalidChar, '_');

        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
    }
}