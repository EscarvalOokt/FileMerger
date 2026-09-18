using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;

namespace FileMerger.Application.UseCases.DownloadAndValidateUpdatePackage;

public sealed class DownloadAndValidateUpdatePackageUseCase
{
    private readonly UpdateCompatibilityPolicy _compatibilityPolicy;
    private readonly IUpdatePackageDownloader _packageDownloader;
    private readonly IUpdatePackageValidator _packageValidator;
    private readonly IUpdaterVersionProvider _updaterVersionProvider;

    public DownloadAndValidateUpdatePackageUseCase(
        UpdateCompatibilityPolicy compatibilityPolicy,
        IUpdaterVersionProvider updaterVersionProvider,
        IUpdatePackageDownloader packageDownloader,
        IUpdatePackageValidator packageValidator)
    {
        ArgumentNullException.ThrowIfNull(compatibilityPolicy);
        ArgumentNullException.ThrowIfNull(updaterVersionProvider);
        ArgumentNullException.ThrowIfNull(packageDownloader);
        ArgumentNullException.ThrowIfNull(packageValidator);

        _compatibilityPolicy = compatibilityPolicy;
        _updaterVersionProvider = updaterVersionProvider;
        _packageDownloader = packageDownloader;
        _packageValidator = packageValidator;
    }

    public async Task<DownloadAndValidateUpdatePackageResult> ExecuteAsync(
        SemanticVersion currentVersion,
        SemanticVersion releaseVersion,
        UpdatePackage selectedPackage,
        IProgress<UpdatePackagePreparationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);
        ArgumentNullException.ThrowIfNull(releaseVersion);
        ArgumentNullException.ThrowIfNull(selectedPackage);

        cancellationToken.ThrowIfCancellationRequested();

        if (!selectedPackage.Version.Equals(releaseVersion))
        {
            return DownloadAndValidateUpdatePackageResult.Failed(
                UpdatePackagePreparationFailureCode.CandidateInvalid,
                $"Package '{selectedPackage.Id}' version '{selectedPackage.Version}' does not match selected release version '{releaseVersion}'.");
        }

        if (!_compatibilityPolicy.IsReleaseEligible(currentVersion, releaseVersion))
        {
            return DownloadAndValidateUpdatePackageResult.Failed(
                UpdatePackagePreparationFailureCode.CandidateInvalid,
                $"Release '{releaseVersion}' is not eligible for the running version '{currentVersion}'.");
        }

        SemanticVersion? updaterVersion;
        try
        {
            string rawUpdaterVersion = _updaterVersionProvider.GetCurrentUpdaterVersion();
            if (!SemanticVersion.TryParse(rawUpdaterVersion, out updaterVersion))
            {
                return DownloadAndValidateUpdatePackageResult.Failed(
                    UpdatePackagePreparationFailureCode.CandidateInvalid,
                    $"The updater protocol version '{rawUpdaterVersion}' is not a valid semantic version.");
            }
        }
        catch (Exception ex)
        {
            return DownloadAndValidateUpdatePackageResult.Failed(
                UpdatePackagePreparationFailureCode.CandidateInvalid,
                $"Failed to read the updater protocol version: {ex.Message}");
        }

        if (!_compatibilityPolicy.IsPackageCompatible(selectedPackage, currentVersion, updaterVersion))
        {
            return DownloadAndValidateUpdatePackageResult.Failed(
                UpdatePackagePreparationFailureCode.CandidateInvalid,
                $"Package '{selectedPackage.Id}' is not compatible with the running application and updater.");
        }

        try
        {
            DownloadedUpdatePackage downloadedPackage = await _packageDownloader.DownloadAsync(
                selectedPackage,
                progress,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            VerifiedUpdatePackage verifiedPackage = await _packageValidator.ValidateAsync(
                downloadedPackage,
                progress,
                cancellationToken);

            return DownloadAndValidateUpdatePackageResult.Verified(verifiedPackage);
        }
        catch (UpdatePackagePreparationException ex)
        {
            return DownloadAndValidateUpdatePackageResult.Failed(ex.FailureCode, ex.Message);
        }
    }
}