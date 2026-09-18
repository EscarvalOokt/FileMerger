using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.CheckForUpdates;
using FileMerger.Application.UseCases.DownloadAndValidateUpdatePackage;
using FileMerger.Wpf.Features.Updates.Services;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Updates.ViewModels;

public sealed class UpdateCheckDialogViewModel : ViewModelBase
{
    private readonly CheckForUpdatesUseCase _checkForUpdatesUseCase;
    private readonly DownloadAndValidateUpdatePackageUseCase _downloadAndValidateUpdatePackageUseCase;
    private readonly IUpdateInstallationCoordinator _installationCoordinator;

    private CancellationTokenSource? _activeOperationCancellation;
    private string? _availableVersionFullText;
    private string _availableVersionText = "Not checked";
    private string? _currentVersionFullText;
    private string _currentVersionText = "Checking...";
    private SemanticVersion? _selectedCurrentVersion;
    private UpdatePackage? _selectedPackage;
    private SemanticVersion? _selectedReleaseVersion;
    private VerifiedUpdatePackage? _verifiedPackage;

    public UpdateCheckDialogViewModel(
        CheckForUpdatesUseCase checkForUpdatesUseCase,
        DownloadAndValidateUpdatePackageUseCase downloadAndValidateUpdatePackageUseCase,
        IUpdateInstallationCoordinator installationCoordinator)
    {
        ArgumentNullException.ThrowIfNull(checkForUpdatesUseCase);
        ArgumentNullException.ThrowIfNull(downloadAndValidateUpdatePackageUseCase);
        ArgumentNullException.ThrowIfNull(installationCoordinator);

        _checkForUpdatesUseCase = checkForUpdatesUseCase;
        _downloadAndValidateUpdatePackageUseCase = downloadAndValidateUpdatePackageUseCase;
        _installationCoordinator = installationCoordinator;

        OperationStatus = new OperationStatusViewModel(CancelActiveOperation);
        OperationStatus.SetStatus("Ready to check for updates.", StatusSeverity.Info);

        CheckAgainCommand = new AsyncRelayCommand(CheckAsync, () => !OperationStatus.IsBusy);
        DownloadCommand = new AsyncRelayCommand(DownloadAsync, CanDownload);
        InstallCommand = new AsyncRelayCommand(InstallAsync, CanInstall);
    }

    public OperationStatusViewModel OperationStatus { get; }

    public string CurrentVersionText
    {
        get => _currentVersionText;
        private set => SetProperty(ref _currentVersionText, value);
    }

    public string AvailableVersionText
    {
        get => _availableVersionText;
        private set => SetProperty(ref _availableVersionText, value);
    }

    public string? CurrentVersionFullText
    {
        get => _currentVersionFullText;
        private set => SetProperty(ref _currentVersionFullText, value);
    }

    public string? AvailableVersionFullText
    {
        get => _availableVersionFullText;
        private set => SetProperty(ref _availableVersionFullText, value);
    }

    public VerifiedUpdatePackage? VerifiedPackage
    {
        get => _verifiedPackage;
        private set
        {
            if (!SetProperty(ref _verifiedPackage, value))
                return;

            OnPropertyChanged(nameof(IsPackageVerified));
            DownloadCommand.RaiseCanExecuteChanged();
            InstallCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsPackageVerified => VerifiedPackage is not null;

    public AsyncRelayCommand CheckAgainCommand { get; }
    public AsyncRelayCommand DownloadCommand { get; }
    public AsyncRelayCommand InstallCommand { get; }

    public async Task CheckAsync()
    {
        await CheckAsync(CancellationToken.None);
    }

    public async Task CheckAsync(CancellationToken cancellationToken)
    {
        if (OperationStatus.IsBusy)
            return;

        ResetSelectedPackageState();
        CurrentVersionText = "Checking...";
        AvailableVersionText = "Not checked";
        CurrentVersionFullText = null;
        AvailableVersionFullText = null;

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperationCancellation = operationCancellation;

        OperationStatus.IsBusy = true;
        OperationStatus.IsCancelable = false;
        OperationStatus.ShowIndeterminateProgress("Checking for updates...");
        OperationStatus.SetStatus("Checking the configured release source.", StatusSeverity.Info);
        RaiseCommandStates();

        try
        {
            CheckForUpdatesResult result = await _checkForUpdatesUseCase.ExecuteAsync(operationCancellation.Token);
            ApplyCheckResult(result);
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            OperationStatus.SetStatus("Update check canceled.", StatusSeverity.Info);
        }
        finally
        {
            ClearActiveOperation(operationCancellation);
            OperationStatus.HideProgress();
            OperationStatus.IsCancelable = false;
            OperationStatus.IsBusy = false;
            RaiseCommandStates();
        }
    }

    public async Task DownloadAsync()
    {
        await DownloadAsync(CancellationToken.None);
    }

    public async Task DownloadAsync(CancellationToken cancellationToken)
    {
        if (OperationStatus.IsBusy ||
            _selectedCurrentVersion is null ||
            _selectedReleaseVersion is null ||
            _selectedPackage is null)
        {
            return;
        }

        VerifiedPackage = null;

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken operationToken = operationCancellation.Token;
        _activeOperationCancellation = operationCancellation;

        OperationStatus.IsBusy = true;
        OperationStatus.IsCancelable = true;
        OperationStatus.ShowDeterminateProgress("Downloading update package...", 0);
        OperationStatus.SetStatus("Downloading the selected update package.", StatusSeverity.Info);
        RaiseCommandStates();

        Progress<UpdatePackagePreparationProgress> progress = CreateProgressReporter(operationToken);

        try
        {
            DownloadAndValidateUpdatePackageResult result = await _downloadAndValidateUpdatePackageUseCase.ExecuteAsync(
                _selectedCurrentVersion,
                _selectedReleaseVersion,
                _selectedPackage,
                progress,
                operationCancellation.Token);

            if (result is { Outcome: DownloadAndValidateUpdatePackageOutcome.Verified, VerifiedPackage: not null })
            {
                VerifiedPackage = result.VerifiedPackage;
                OperationStatus.SetStatus(
                    $"Update package {result.VerifiedPackage.Package.Version} is verified and ready for installation.",
                    StatusSeverity.Success);
            }
            else
            {
                OperationStatus.SetStatus(
                    $"Update package preparation failed. {result.FailureMessage}",
                    StatusSeverity.Error);
            }
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            VerifiedPackage = null;
            OperationStatus.SetStatus("Update package preparation canceled.", StatusSeverity.Info);
        }
        finally
        {
            ClearActiveOperation(operationCancellation);
            OperationStatus.HideProgress();
            OperationStatus.IsCancelable = false;
            OperationStatus.IsBusy = false;
            RaiseCommandStates();
        }
    }

    public async Task InstallAsync()
    {
        await InstallAsync(CancellationToken.None);
    }

    public async Task InstallAsync(CancellationToken cancellationToken)
    {
        if (OperationStatus.IsBusy || VerifiedPackage is null)
            return;

        VerifiedUpdatePackage packageToInstall = VerifiedPackage;
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken operationToken = operationCancellation.Token;
        _activeOperationCancellation = operationCancellation;
        bool shutdownCommitted = false;

        OperationStatus.IsBusy = true;
        OperationStatus.IsCancelable = true;
        OperationStatus.ShowIndeterminateProgress("Preparing update installation...");
        OperationStatus.SetStatus(
            "Revalidating the verified package and preparing FileMerger to close.",
            StatusSeverity.Info);
        RaiseCommandStates();

        Progress<UpdatePackagePreparationProgress> progress = CreateProgressReporter(operationToken);

        try
        {
            UpdateInstallationCoordinationResult result = await _installationCoordinator.PrepareAndLaunchAsync(
                packageToInstall,
                progress,
                operationCancellation.Token);

            switch (result.Outcome)
            {
                case UpdateInstallationCoordinationOutcome.ReadyToShutdown:
                    OperationStatus.SetStatus(result.Message, StatusSeverity.Success);
                    OperationStatus.HideProgress();
                    OperationStatus.IsCancelable = false;
                    OperationStatus.IsBusy = false;
                    ClearActiveOperation(operationCancellation);
                    RaiseCommandStates();

                    shutdownCommitted = true;
                    _installationCoordinator.CommitShutdown();
                    break;

                case UpdateInstallationCoordinationOutcome.ActiveOperation:
                    OperationStatus.SetStatus(result.Message, StatusSeverity.Warning);
                    break;

                case UpdateInstallationCoordinationOutcome.GuardCanceled:
                    OperationStatus.SetStatus(result.Message, StatusSeverity.Info);
                    break;

                case UpdateInstallationCoordinationOutcome.PreflightFailed:
                case UpdateInstallationCoordinationOutcome.HelperLaunchFailed:
                    if (ShouldInvalidateVerifiedPackage(result.FailureCode))
                        VerifiedPackage = null;

                    OperationStatus.SetStatus(result.Message, StatusSeverity.Error);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(result),
                        result.Outcome,
                        "Unsupported installation outcome.");
            }
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            OperationStatus.SetStatus(
                "Update installation canceled before the installer was launched.",
                StatusSeverity.Info);
        }
        finally
        {
            if (!shutdownCommitted)
            {
                ClearActiveOperation(operationCancellation);
                OperationStatus.HideProgress();
                OperationStatus.IsCancelable = false;
                OperationStatus.IsBusy = false;
                RaiseCommandStates();
            }
        }
    }

    public void CancelActiveOperation()
    {
        try
        {
            _activeOperationCancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The operation completed while the cancellation request was being dispatched.
        }
    }

    private static string FormatConciseVersion(SemanticVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        string coreVersion = $"{version.Major}.{version.Minor}.{version.Patch}";

        if (version.PrereleaseIdentifiers.Count == 0)
            return coreVersion;

        return $"{coreVersion}-{string.Join(".", version.PrereleaseIdentifiers)}";
    }

    private static bool ShouldInvalidateVerifiedPackage(UpdateInstallationFailureCode failureCode)
    {
        return failureCode is UpdateInstallationFailureCode.PackageNoLongerValid
            or UpdateInstallationFailureCode.IncompatiblePackage
            or UpdateInstallationFailureCode.InvalidStaging;
    }

    private bool CanDownload()
    {
        return !OperationStatus.IsBusy &&
               _selectedCurrentVersion is not null &&
               _selectedReleaseVersion is not null &&
               _selectedPackage is not null &&
               !IsPackageVerified;
    }

    private bool CanInstall()
    {
        return !OperationStatus.IsBusy && VerifiedPackage is not null;
    }

    private void ApplyCheckResult(CheckForUpdatesResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        CurrentVersionText = result.CurrentVersion is null ? "Unknown" : FormatConciseVersion(result.CurrentVersion);
        CurrentVersionFullText = result.CurrentVersion?.ToString();

        AvailableVersionText = result.ReleaseVersion is null
            ? "Unavailable"
            : FormatConciseVersion(result.ReleaseVersion);
        AvailableVersionFullText = result.ReleaseVersion?.ToString();

        switch (result.Outcome)
        {
            case UpdateCheckOutcome.UpdateAvailable:
                if (result.CurrentVersion is null || result.ReleaseVersion is null || result.SelectedPackage is null)
                {
                    OperationStatus.SetStatus(
                        "Update check failed. The available update result is incomplete.",
                        StatusSeverity.Error);
                    ResetSelectedPackageState();
                    break;
                }

                _selectedCurrentVersion = result.CurrentVersion;
                _selectedReleaseVersion = result.ReleaseVersion;
                _selectedPackage = result.SelectedPackage;
                OperationStatus.SetStatus(
                    $"A newer compatible version is available: {result.ReleaseVersion}.",
                    StatusSeverity.Success);
                break;

            case UpdateCheckOutcome.NoUpdateAvailable:
                ResetSelectedPackageState();
                OperationStatus.SetStatus("No newer compatible update is available.", StatusSeverity.Info);
                break;

            case UpdateCheckOutcome.Failed:
                ResetSelectedPackageState();
                OperationStatus.SetStatus($"Update check failed. {result.FailureMessage}", StatusSeverity.Error);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(result),
                    result.Outcome,
                    "Unsupported update check outcome.");
        }

        RaiseCommandStates();
    }

    private Progress<UpdatePackagePreparationProgress> CreateProgressReporter(CancellationToken operationToken)
    {
        return new Progress<UpdatePackagePreparationProgress>(value =>
        {
            CancellationTokenSource? activeOperation = _activeOperationCancellation;

            if (activeOperation is not null && activeOperation.Token == operationToken)
                ApplyPreparationProgress(value);
        });
    }

    private void ApplyPreparationProgress(UpdatePackagePreparationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (progress.Stage == UpdatePackagePreparationStage.Completed)
        {
            OperationStatus.ShowDeterminateProgress(progress.Message, 100);
            return;
        }

        if (progress.IsDeterminate)
        {
            double progressValue = progress.Current * 100d / progress.Total;
            OperationStatus.ShowDeterminateProgress(progress.Message, progressValue);
            return;
        }

        OperationStatus.ShowIndeterminateProgress(progress.Message);
    }

    private void ResetSelectedPackageState()
    {
        _selectedCurrentVersion = null;
        _selectedReleaseVersion = null;
        _selectedPackage = null;
        VerifiedPackage = null;
        DownloadCommand.RaiseCanExecuteChanged();
        InstallCommand.RaiseCanExecuteChanged();
    }

    private void RaiseCommandStates()
    {
        CheckAgainCommand.RaiseCanExecuteChanged();
        DownloadCommand.RaiseCanExecuteChanged();
        InstallCommand.RaiseCanExecuteChanged();
    }

    private void ClearActiveOperation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_activeOperationCancellation, operationCancellation))
            _activeOperationCancellation = null;
    }
}