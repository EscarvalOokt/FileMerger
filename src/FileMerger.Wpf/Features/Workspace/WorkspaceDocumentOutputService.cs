using System.IO;
using FileMerger.Application.UseCases.SaveOutput;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.Integration;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentOutputService : IWorkspaceDocumentOutputService
{
    private readonly IFileSystemLauncher _fileSystemLauncher;
    private readonly IMainStateFactory _mainStateFactory;
    private readonly ISaveFileDialogService _saveFileDialogService;
    private readonly SaveMergeOutputUseCase _saveMergeOutputUseCase;

    public WorkspaceDocumentOutputService(
        SaveMergeOutputUseCase saveMergeOutputUseCase,
        ISaveFileDialogService saveFileDialogService,
        IMainStateFactory mainStateFactory,
        IFileSystemLauncher fileSystemLauncher)
    {
        ArgumentNullException.ThrowIfNull(saveMergeOutputUseCase);
        ArgumentNullException.ThrowIfNull(saveFileDialogService);
        ArgumentNullException.ThrowIfNull(mainStateFactory);
        ArgumentNullException.ThrowIfNull(fileSystemLauncher);

        _saveMergeOutputUseCase = saveMergeOutputUseCase;
        _saveFileDialogService = saveFileDialogService;
        _mainStateFactory = mainStateFactory;
        _fileSystemLauncher = fileSystemLauncher;
    }

    public Task SaveOutputAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.LastOutput is null)
            return Task.CompletedTask;

        try
        {
            document.OperationStatus.IsBusy = true;
            stateChanged?.Invoke();

            cancellationToken.ThrowIfCancellationRequested();

            OutputTarget target = document.SessionSettings.BuildOutputTarget();
            bool isSavingStaleBuild = document.PreviewDirtyTracker is { HasAppliedPreview: true, IsPreviewDirty: true };

            SaveMergeOutputRequest request = new(output: document.LastOutput, target: target);

            SaveMergeOutputResult result = _saveMergeOutputUseCase.Execute(request);

            if (result.ValidationIssues.Count > 0)
                document.ValidationPane.Load(result.ValidationIssues);

            string successMessage = isSavingStaleBuild
                ? "Last built output saved successfully. Changes made since that build are not included in the saved content; run Build Preview to rebuild it."
                : "Merged output saved successfully.";

            document.OperationStatus.SetStatus(
                result.IsSuccessful ? successMessage : "Failed to save merged output.",
                result.IsSuccessful ? StatusSeverity.Success : StatusSeverity.Error);
        }
        catch (OperationCanceledException)
        {
            document.OperationStatus.SetStatus("Save output canceled.", StatusSeverity.Warning);
        }
        catch (Exception ex)
        {
            document.OperationStatus.SetStatus($"Failed to save merged output: {ex.Message}", StatusSeverity.Error);
        }
        finally
        {
            document.OperationStatus.IsBusy = false;
            stateChanged?.Invoke();
        }

        return Task.CompletedTask;
    }

    public void BrowseOutputPath(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        string? selectedPath = _saveFileDialogService.SelectSaveFilePath(
            initialPath: _mainStateFactory.BuildInitialOutputPath(document),
            filter: null);

        if (string.IsNullOrWhiteSpace(selectedPath))
            return;

        document.SessionSettings.OutputPath = selectedPath;
    }

    public bool CanOpenOutputFolder(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        string? directoryPath = GetOutputDirectoryPath(document);

        return _fileSystemLauncher.CanOpenDirectory(directoryPath);
    }

    public void OpenOutputFolder(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        string? directoryPath = GetOutputDirectoryPath(document);

        if (!_fileSystemLauncher.CanOpenDirectory(directoryPath))
        {
            document.OperationStatus.SetStatus("Output folder is not available.", StatusSeverity.Warning);
            return;
        }

        try
        {
            _fileSystemLauncher.OpenDirectory(directoryPath!);
            document.OperationStatus.SetStatus("Output folder opened.", StatusSeverity.Success);
        }
        catch (Exception ex)
        {
            document.OperationStatus.SetStatus($"Failed to open output folder: {ex.Message}", StatusSeverity.Error);
        }
    }

    private static string? GetOutputDirectoryPath(WorkspaceDocumentViewModel document)
    {
        string outputPath = document.SessionSettings.OutputPath;

        if (string.IsNullOrWhiteSpace(outputPath))
            return null;

        return Path.GetDirectoryName(outputPath);
    }
}