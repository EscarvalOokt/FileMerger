using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.LaunchUpdateInstaller;
using FileMerger.Application.UseCases.PrepareUpdateInstallation;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Shared.Integration;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Features.Updates.Services;

public sealed class UpdateInstallationCoordinator : IUpdateInstallationCoordinator
{
    private readonly IApplicationShutdownService _applicationShutdownService;
    private readonly IApplicationWindowCloseGuardService _closeGuardService;
    private readonly LaunchUpdateInstallerUseCase _launchUpdateInstallerUseCase;
    private readonly PrepareUpdateInstallationUseCase _prepareUpdateInstallationUseCase;
    private readonly WorkspaceTabManagerViewModel _workspaceTabs;

    private bool _readyToShutdown;

    public UpdateInstallationCoordinator(
        PrepareUpdateInstallationUseCase prepareUpdateInstallationUseCase,
        LaunchUpdateInstallerUseCase launchUpdateInstallerUseCase,
        WorkspaceTabManagerViewModel workspaceTabs,
        IApplicationWindowCloseGuardService closeGuardService,
        IApplicationShutdownService applicationShutdownService)
    {
        ArgumentNullException.ThrowIfNull(prepareUpdateInstallationUseCase);
        ArgumentNullException.ThrowIfNull(launchUpdateInstallerUseCase);
        ArgumentNullException.ThrowIfNull(workspaceTabs);
        ArgumentNullException.ThrowIfNull(closeGuardService);
        ArgumentNullException.ThrowIfNull(applicationShutdownService);

        _prepareUpdateInstallationUseCase = prepareUpdateInstallationUseCase;
        _launchUpdateInstallerUseCase = launchUpdateInstallerUseCase;
        _workspaceTabs = workspaceTabs;
        _closeGuardService = closeGuardService;
        _applicationShutdownService = applicationShutdownService;
    }

    public async Task<UpdateInstallationCoordinationResult> PrepareAndLaunchAsync(
        VerifiedUpdatePackage verifiedPackage,
        IProgress<UpdatePackagePreparationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifiedPackage);

        _readyToShutdown = false;
        _closeGuardService.ResetPreparedShutdown();

        PrepareUpdateInstallationResult preparation = await _prepareUpdateInstallationUseCase.ExecuteAsync(
            verifiedPackage,
            progress,
            cancellationToken);

        if (preparation is not { Outcome: PrepareUpdateInstallationOutcome.Ready, VerifiedPackage: not null })
        {
            return UpdateInstallationCoordinationResult.PreflightFailed(
                preparation.FailureMessage ?? "Update installation preflight failed.",
                preparation.FailureCode);
        }

        if (_workspaceTabs.HasBusyTabs())
        {
            return UpdateInstallationCoordinationResult.ActiveOperation(
                "Finish or cancel active workspace operations before installing the update.");
        }

        bool windowsReady;
        try
        {
            windowsReady = await _closeGuardService.TryPrepareAsync(cancellationToken);
        }
        catch
        {
            _closeGuardService.ResetPreparedShutdown();
            throw;
        }

        if (!windowsReady)
        {
            _closeGuardService.ResetPreparedShutdown();
            return UpdateInstallationCoordinationResult.GuardCanceled(
                "Update installation was canceled by an open window with unsaved changes.");
        }

        try
        {
            LaunchUpdateInstallerResult launchResult = await _launchUpdateInstallerUseCase.ExecuteAsync(
                preparation.VerifiedPackage,
                cancellationToken);

            if (launchResult.Outcome != LaunchUpdateInstallerOutcome.Started)
            {
                _closeGuardService.ResetPreparedShutdown();
                return UpdateInstallationCoordinationResult.HelperLaunchFailed(
                    launchResult.FailureMessage ?? "The update installer could not be started.",
                    launchResult.FailureCode);
            }
        }
        catch
        {
            _closeGuardService.ResetPreparedShutdown();
            throw;
        }

        _readyToShutdown = true;
        return UpdateInstallationCoordinationResult.ReadyToShutdown(
            "The update installer has started. FileMerger will close and restart after installation.");
    }

    public void CommitShutdown()
    {
        if (!_readyToShutdown)
            throw new InvalidOperationException("Update shutdown was not prepared successfully.");

        _readyToShutdown = false;
        _closeGuardService.CommitPreparedShutdown();
        _applicationShutdownService.Shutdown();
    }
}