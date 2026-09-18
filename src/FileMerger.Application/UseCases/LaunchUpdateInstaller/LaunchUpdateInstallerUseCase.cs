using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;

namespace FileMerger.Application.UseCases.LaunchUpdateInstaller;

public sealed class LaunchUpdateInstallerUseCase
{
    private readonly IUpdateInstallerLauncher _installerLauncher;

    public LaunchUpdateInstallerUseCase(IUpdateInstallerLauncher installerLauncher)
    {
        ArgumentNullException.ThrowIfNull(installerLauncher);
        _installerLauncher = installerLauncher;
    }

    public async Task<LaunchUpdateInstallerResult> ExecuteAsync(
        VerifiedUpdatePackage verifiedPackage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifiedPackage);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _installerLauncher.StartAsync(verifiedPackage, cancellationToken);
            return LaunchUpdateInstallerResult.Started();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UpdateInstallationException ex)
        {
            return LaunchUpdateInstallerResult.Failed(ex.FailureCode, ex.Message);
        }
        catch (Exception ex)
        {
            return LaunchUpdateInstallerResult.Failed(
                UpdateInstallationFailureCode.HelperLaunchFailed,
                $"Failed to start the update installer: {ex.Message}");
        }
    }
}