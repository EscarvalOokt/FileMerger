using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;

namespace FileMerger.Application.UseCases.CheckForUpdates;

public sealed class CheckForUpdatesUseCase
{
    private readonly IApplicationVersionProvider _applicationVersionProvider;
    private readonly UpdateCompatibilityPolicy _compatibilityPolicy;
    private readonly UpdateReleaseManifestParser _manifestParser;
    private readonly IReleaseManifestSource _releaseManifestSource;
    private readonly IUpdaterVersionProvider _updaterVersionProvider;

    public CheckForUpdatesUseCase(
        IApplicationVersionProvider applicationVersionProvider,
        IUpdaterVersionProvider updaterVersionProvider,
        IReleaseManifestSource releaseManifestSource,
        UpdateReleaseManifestParser manifestParser,
        UpdateCompatibilityPolicy compatibilityPolicy)
    {
        ArgumentNullException.ThrowIfNull(applicationVersionProvider);
        ArgumentNullException.ThrowIfNull(updaterVersionProvider);
        ArgumentNullException.ThrowIfNull(releaseManifestSource);
        ArgumentNullException.ThrowIfNull(manifestParser);
        ArgumentNullException.ThrowIfNull(compatibilityPolicy);

        _applicationVersionProvider = applicationVersionProvider;
        _updaterVersionProvider = updaterVersionProvider;
        _releaseManifestSource = releaseManifestSource;
        _manifestParser = manifestParser;
        _compatibilityPolicy = compatibilityPolicy;
    }

    public async Task<CheckForUpdatesResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        SemanticVersion? currentVersion;
        SemanticVersion? updaterVersion;
        try
        {
            string rawCurrentVersion = _applicationVersionProvider.GetCurrentVersion();
            if (!SemanticVersion.TryParse(rawCurrentVersion, out currentVersion))
            {
                return CheckForUpdatesResult.Failed(
                    UpdateCheckFailureCode.CurrentVersionInvalid,
                    $"The running application version '{rawCurrentVersion}' is not a valid semantic version.");
            }

            string rawUpdaterVersion = _updaterVersionProvider.GetCurrentUpdaterVersion();
            if (!SemanticVersion.TryParse(rawUpdaterVersion, out updaterVersion))
            {
                return CheckForUpdatesResult.Failed(
                    UpdateCheckFailureCode.CurrentVersionInvalid,
                    $"The updater protocol version '{rawUpdaterVersion}' is not a valid semantic version.",
                    currentVersion);
            }
        }
        catch (Exception ex)
        {
            return CheckForUpdatesResult.Failed(
                UpdateCheckFailureCode.CurrentVersionInvalid,
                $"Failed to read application or updater version information: {ex.Message}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        string manifestJson;
        try
        {
            manifestJson = await _releaseManifestSource.GetManifestJsonAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CheckForUpdatesResult.Failed(
                UpdateCheckFailureCode.ReleaseSourceUnavailable,
                $"Failed to read the update manifest: {ex.Message}",
                currentVersion);
        }

        cancellationToken.ThrowIfCancellationRequested();

        UpdateReleaseManifest manifest;
        try
        {
            manifest = _manifestParser.Parse(manifestJson);
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException)
        {
            return CheckForUpdatesResult.Failed(
                UpdateCheckFailureCode.ManifestInvalid,
                $"The update manifest is invalid: {ex.Message}",
                currentVersion);
        }

        SemanticVersion releaseVersion = manifest.Release.Version;

        if (!_compatibilityPolicy.IsReleaseEligible(currentVersion, releaseVersion))
            return CheckForUpdatesResult.NoUpdateAvailable(currentVersion, releaseVersion);

        UpdatePackage[] compatiblePackages =
        [
            .. manifest.Release.Packages.Where(package =>
                _compatibilityPolicy.IsPackageCompatible(package, currentVersion, updaterVersion))
        ];

        if (compatiblePackages.Length == 0)
            return CheckForUpdatesResult.NoUpdateAvailable(currentVersion, releaseVersion);

        if (compatiblePackages.Length > 1)
        {
            return CheckForUpdatesResult.Failed(
                UpdateCheckFailureCode.AmbiguousCompatiblePackage,
                $"Release '{releaseVersion}' contains more than one compatible update package.",
                currentVersion,
                releaseVersion);
        }

        return CheckForUpdatesResult.UpdateAvailable(currentVersion, releaseVersion, compatiblePackages[0]);
    }
}