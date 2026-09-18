using System.Diagnostics.CodeAnalysis;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;

namespace FileMerger.Application.UseCases.PrepareUpdateInstallation;

public sealed class PrepareUpdateInstallationUseCase
{
    private readonly IApplicationVersionProvider _applicationVersionProvider;
    private readonly UpdateCompatibilityPolicy _compatibilityPolicy;
    private readonly IUpdateInstallationPreflightService _installationPreflightService;
    private readonly IUpdatePackageValidator _packageValidator;
    private readonly IUpdaterVersionProvider _updaterVersionProvider;

    public PrepareUpdateInstallationUseCase(
        IApplicationVersionProvider applicationVersionProvider,
        IUpdaterVersionProvider updaterVersionProvider,
        UpdateCompatibilityPolicy compatibilityPolicy,
        IUpdatePackageValidator packageValidator,
        IUpdateInstallationPreflightService installationPreflightService)
    {
        ArgumentNullException.ThrowIfNull(applicationVersionProvider);
        ArgumentNullException.ThrowIfNull(updaterVersionProvider);
        ArgumentNullException.ThrowIfNull(compatibilityPolicy);
        ArgumentNullException.ThrowIfNull(packageValidator);
        ArgumentNullException.ThrowIfNull(installationPreflightService);

        _applicationVersionProvider = applicationVersionProvider;
        _updaterVersionProvider = updaterVersionProvider;
        _compatibilityPolicy = compatibilityPolicy;
        _packageValidator = packageValidator;
        _installationPreflightService = installationPreflightService;
    }

    public async Task<PrepareUpdateInstallationResult> ExecuteAsync(
        VerifiedUpdatePackage verifiedPackage,
        IProgress<UpdatePackagePreparationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifiedPackage);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryReadVersions(
                out SemanticVersion? currentVersion,
                out SemanticVersion? updaterVersion,
                out string? versionFailure))
        {
            return PrepareUpdateInstallationResult.Failed(
                UpdateInstallationFailureCode.PreparationFailed,
                versionFailure);
        }

        UpdatePackage package = verifiedPackage.Package;

        if (!_compatibilityPolicy.IsReleaseEligible(currentVersion, package.Version) ||
            !_compatibilityPolicy.IsPackageCompatible(package, currentVersion, updaterVersion))
        {
            return PrepareUpdateInstallationResult.Failed(
                UpdateInstallationFailureCode.IncompatiblePackage,
                $"Package '{package.Id}' is no longer compatible with the running application and updater.");
        }

        DownloadedUpdatePackage downloadedPackage = new(
            package,
            verifiedPackage.AttemptDirectory,
            verifiedPackage.ArchivePath);

        try
        {
            VerifiedUpdatePackage revalidatedPackage = await _packageValidator.ValidateAsync(
                downloadedPackage,
                progress,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await _installationPreflightService.ValidateAsync(revalidatedPackage, cancellationToken);
            return PrepareUpdateInstallationResult.Ready(revalidatedPackage);
        }
        catch (UpdatePackagePreparationException ex)
        {
            return PrepareUpdateInstallationResult.Failed(
                UpdateInstallationFailureCode.PackageNoLongerValid,
                $"The verified update package is no longer valid: {ex.Message}");
        }
        catch (UpdateInstallationException ex)
        {
            return PrepareUpdateInstallationResult.Failed(ex.FailureCode, ex.Message);
        }
    }

    private bool TryReadVersions(
        [NotNullWhen(true)] out SemanticVersion? currentVersion,
        [NotNullWhen(true)] out SemanticVersion? updaterVersion,
        [NotNullWhen(false)] out string? failureMessage)
    {
        currentVersion = null;
        updaterVersion = null;
        failureMessage = null;

        try
        {
            string rawCurrentVersion = _applicationVersionProvider.GetCurrentVersion();
            if (!SemanticVersion.TryParse(rawCurrentVersion, out currentVersion))
            {
                failureMessage =
                    $"The running application version '{rawCurrentVersion}' is not a valid semantic version.";
                return false;
            }

            string rawUpdaterVersion = _updaterVersionProvider.GetCurrentUpdaterVersion();
            if (!SemanticVersion.TryParse(rawUpdaterVersion, out updaterVersion))
            {
                failureMessage = $"The updater protocol version '{rawUpdaterVersion}' is not a valid semantic version.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            failureMessage = $"Failed to read application or updater version information: {ex.Message}";
            return false;
        }
    }
}