using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Common;
using FileMerger.UpdateProtocol;

namespace FileMerger.Infrastructure.Updates;

public sealed class UpdateInstallationPreflightService : IUpdateInstallationPreflightService
{
    private readonly ApplicationInstallationPathProvider _installationPathProvider;
    private readonly UpdateStagingPathPolicy _stagingPathPolicy;

    public UpdateInstallationPreflightService(
        ApplicationInstallationPathProvider installationPathProvider,
        UpdateStagingPathPolicy stagingPathPolicy)
    {
        ArgumentNullException.ThrowIfNull(installationPathProvider);
        ArgumentNullException.ThrowIfNull(stagingPathPolicy);

        _installationPathProvider = installationPathProvider;
        _stagingPathPolicy = stagingPathPolicy;
    }

    public Task ValidateAsync(VerifiedUpdatePackage verifiedPackage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifiedPackage);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateStagingLayout(verifiedPackage);

        string installationDirectory = _installationPathProvider.GetInstallationDirectory();
        if (!Directory.Exists(installationDirectory))
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.InstallationDirectoryUnavailable,
                $"The FileMerger installation directory '{installationDirectory}' does not exist.");
        }

        string updatesRoot = _stagingPathPolicy.GetUpdatesRootDirectory();
        if (PathUtility.PathsOverlap(installationDirectory, updatesRoot))
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.InvalidStaging,
                "The application installation directory and update staging directory must not overlap.");
        }

        foreach (string fileName in UpdateProtocolConstants.RequiredUpdaterRuntimeFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string path = Path.Combine(installationDirectory, fileName);
            if (!File.Exists(path))
            {
                throw new UpdateInstallationException(
                    UpdateInstallationFailureCode.UpdaterRuntimeUnavailable,
                    $"The current installation is missing updater runtime file '{fileName}'.");
            }
        }

        VerifyInstallationDirectoryIsWritable(installationDirectory, cancellationToken);
        return Task.CompletedTask;
    }

    private void ValidateStagingLayout(VerifiedUpdatePackage verifiedPackage)
    {
        string updatesRoot = _stagingPathPolicy.GetUpdatesRootDirectory();
        if (!PathUtility.IsPathInsideDirectory(verifiedPackage.AttemptDirectory, updatesRoot))
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.InvalidStaging,
                "The verified update attempt is outside the FileMerger update staging root.");
        }

        string expectedArchivePath = _stagingPathPolicy.GetArchivePath(verifiedPackage.AttemptDirectory);
        string expectedPayloadDirectory = _stagingPathPolicy.GetPayloadDirectory(verifiedPackage.AttemptDirectory);

        if (!PathUtility.PathEquals(verifiedPackage.ArchivePath, expectedArchivePath) ||
            !PathUtility.PathEquals(verifiedPackage.PayloadDirectory, expectedPayloadDirectory))
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.InvalidStaging,
                "The verified update package does not match the expected staging layout.");
        }

        if (!File.Exists(verifiedPackage.ArchivePath) ||
            !Directory.Exists(verifiedPackage.PayloadDirectory) ||
            !File.Exists(verifiedPackage.EntryExecutablePath) ||
            !PathUtility.IsPathInsideDirectory(verifiedPackage.EntryExecutablePath, verifiedPackage.PayloadDirectory))
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.InvalidStaging,
                "The verified update package is missing required staged files.");
        }
    }

    private static void VerifyInstallationDirectoryIsWritable(
        string installationDirectory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string probePath = Path.Combine(
            installationDirectory,
            $".filemerger-update-write-probe-{Guid.NewGuid():N}.tmp");

        try
        {
            using FileStream stream = new(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            stream.WriteByte(0);
            stream.Flush(flushToDisk: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.InstallationDirectoryNotWritable,
                $"The FileMerger installation directory is not writable: {ex.Message}",
                ex);
        }
        finally
        {
            try
            {
                if (File.Exists(probePath))
                    File.Delete(probePath);
            }
            catch
            {
                // The probe result has already been determined. Cleanup is best-effort.
            }
        }
    }
}