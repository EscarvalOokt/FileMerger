using FileMerger.Application.UseCases.BuildPreview;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Preview;
using FileMerger.Wpf.Features.Preview.ViewModels;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentPreviewService : IWorkspaceDocumentPreviewService
{
    private const string OutdatedPreviewNotice =
        "Current configuration could not produce a new preview. Showing the last successfully built preview; it may be outdated.";

    private const string CanceledPreviewNotice =
        "Preview build was canceled. Showing the last successfully built preview; it may be outdated.";

    private readonly IApplicationPreferencesStore _applicationPreferencesStore;

    private readonly BuildMergePreviewUseCase _buildMergePreviewUseCase;
    private readonly IWorkspaceDocumentDirtyStateService _dirtyStateService;
    private readonly IMainStateFactory _mainStateFactory;

    public WorkspaceDocumentPreviewService(
        BuildMergePreviewUseCase buildMergePreviewUseCase,
        IMainStateFactory mainStateFactory,
        IWorkspaceDocumentDirtyStateService dirtyStateService,
        IApplicationPreferencesStore applicationPreferencesStore)
    {
        ArgumentNullException.ThrowIfNull(buildMergePreviewUseCase);
        ArgumentNullException.ThrowIfNull(mainStateFactory);
        ArgumentNullException.ThrowIfNull(dirtyStateService);
        ArgumentNullException.ThrowIfNull(applicationPreferencesStore);

        _buildMergePreviewUseCase = buildMergePreviewUseCase;
        _mainStateFactory = mainStateFactory;
        _dirtyStateService = dirtyStateService;
        _applicationPreferencesStore = applicationPreferencesStore;
    }

    public async Task BuildPreviewAsync(
        WorkspaceDocumentViewModel document,
        Action? stateChanged = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            document.OperationStatus.IsBusy = true;
            document.OperationStatus.IsCancelable = true;
            document.OperationStatus.ShowIndeterminateProgress("Preparing preview build.");
            stateChanged?.Invoke();

            CancellationToken documentCancellationToken = document.BeginPreviewBuild();

            using var linkedCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(documentCancellationToken, cancellationToken);

            linkedCancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (!document.ProfileEditor.CanUseProfile)
            {
                string validationMessage = document.ProfileEditor.DraftValidationMessage!;

                ApplyPreflightValidationFailure(
                    document,
                    new ValidationIssue(
                        severity: ValidationSeverity.Error,
                        code: "profile.filterRules.invalid",
                        message: validationMessage));

                stateChanged?.Invoke();
                return;
            }

            if (string.IsNullOrWhiteSpace(document.SessionSettings.OutputPath))
            {
                const string validationMessage =
                    "Output path cannot be empty. Choose an output file in Session settings.";

                ApplyPreflightValidationFailure(
                    document,
                    new ValidationIssue(
                        severity: ValidationSeverity.Error,
                        code: "output.path.empty",
                        message: validationMessage));

                stateChanged?.Invoke();
                return;
            }

            BuildMergePreviewRequest request = new(
                session: _mainStateFactory.BuildSession(document),
                inclusionOverrides: document.FilesPane.BuildOverrides());

            Progress<BuildMergePreviewProgress> progress = new(document.OperationStatus.Apply);

            BuildMergePreviewResult result = await _buildMergePreviewUseCase.ExecuteAsync(
                request,
                progress,
                linkedCancellationTokenSource.Token);

            document.ValidationPane.Load(result.ValidationIssues);

            if (!result.IsSuccessful || result.Output is null)
            {
                _dirtyStateService.RefreshPreviewDirtyState(document);

                if (!document.AppliedPreviewFileStateStore.HasAppliedState)
                {
                    document.ClearLastOutput();
                    document.PreviewContent = string.Empty;
                    document.ResetPreviewCharacterCount();
                    document.ResetPreviewSummary();
                }

                UpdatePreviewNoticeForUnsuccessfulBuild(document, OutdatedPreviewNotice);

                bool hasErrors = result.ValidationIssues.Any(x => x.Severity == ValidationSeverity.Error);

                document.OperationStatus.SetStatus(
                    hasErrors
                        ? "Preview build failed due to validation errors."
                        : "Preview build completed with warnings.",
                    hasErrors ? StatusSeverity.Error : StatusSeverity.Warning);

                stateChanged?.Invoke();
                return;
            }

            document.SetLastOutput(result.Output);
            document.AppliedPreviewFileStateStore.Set(result.Session.Files);

            document.FilesPane.ApplyFiles(
                result.AutomaticFiles,
                result.Session.Files,
                document.AppliedPreviewFileStateStore.Current);

            PreviewTextFormatResult previewText = PreviewTextFormatter.Format(
                result.Output.Content,
                _applicationPreferencesStore.Current.PreviewDisplayCharacterLimit);

            document.PreviewContent = previewText.Text;
            document.SetPreviewCharacterCount(previewText.TotalCharacters);
            document.SetPreviewSummary(
                PreviewGenerationSummaryViewModel.From(
                    result.Output,
                    result.AutomaticFiles,
                    result.Session.Files,
                    result.SourceExcludedFiles,
                    previewText));

            document.PreviewNotice = string.Empty;

            _dirtyStateService.MarkPreviewApplied(document);

            document.OperationStatus.SetStatus("Preview built successfully.", StatusSeverity.Success);
            stateChanged?.Invoke();
        }
        catch (OperationCanceledException)
        {
            UpdatePreviewNoticeForUnsuccessfulBuild(document, CanceledPreviewNotice);
            document.OperationStatus.SetStatus("Preview build canceled.", StatusSeverity.Warning);
        }
        catch (Exception ex)
        {
            UpdatePreviewNoticeForUnsuccessfulBuild(document, OutdatedPreviewNotice);
            document.OperationStatus.SetStatus($"Failed to build preview: {ex.Message}", StatusSeverity.Error);
        }
        finally
        {
            document.OperationStatus.HideProgress();
            document.OperationStatus.IsCancelable = false;
            document.OperationStatus.IsBusy = false;
            document.CompletePreviewBuild();

            stateChanged?.Invoke();
        }
    }

    private void ApplyPreflightValidationFailure(WorkspaceDocumentViewModel document, ValidationIssue issue)
    {
        document.ValidationPane.Load([issue]);

        document.OperationStatus.SetStatus(
            $"Preview build failed due to validation errors. {issue.Message}",
            StatusSeverity.Error);

        _dirtyStateService.RefreshPreviewDirtyState(document);
        UpdatePreviewNoticeForUnsuccessfulBuild(document, OutdatedPreviewNotice);
    }

    private static void UpdatePreviewNoticeForUnsuccessfulBuild(WorkspaceDocumentViewModel document, string notice)
    {
        if (document.AppliedPreviewFileStateStore.HasAppliedState)
        {
            document.PreviewNotice = notice;
            return;
        }

        document.PreviewNotice = string.Empty;
        document.ResetPreviewSummary();
    }
}