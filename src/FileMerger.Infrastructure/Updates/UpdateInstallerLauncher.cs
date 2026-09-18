using System.Diagnostics;
using System.Security.Cryptography;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Common;
using FileMerger.UpdateProtocol;

namespace FileMerger.Infrastructure.Updates;

public sealed class UpdateInstallerLauncher : IUpdateInstallerLauncher
{
    private readonly ApplicationInstallationPathProvider _installationPathProvider;
    private readonly Func<ProcessStartInfo, Process?> _processStarter;
    private readonly UpdateProtocolFileStore _protocolFileStore;
    private readonly UpdateStagingPathPolicy _stagingPathPolicy;

    public UpdateInstallerLauncher(
        ApplicationInstallationPathProvider installationPathProvider,
        UpdateStagingPathPolicy stagingPathPolicy,
        UpdateProtocolFileStore protocolFileStore) : this(
        installationPathProvider,
        stagingPathPolicy,
        protocolFileStore,
        Process.Start)
    {
    }

    public UpdateInstallerLauncher(
        ApplicationInstallationPathProvider installationPathProvider,
        UpdateStagingPathPolicy stagingPathPolicy,
        UpdateProtocolFileStore protocolFileStore,
        Func<ProcessStartInfo, Process?> processStarter)
    {
        ArgumentNullException.ThrowIfNull(installationPathProvider);
        ArgumentNullException.ThrowIfNull(stagingPathPolicy);
        ArgumentNullException.ThrowIfNull(protocolFileStore);
        ArgumentNullException.ThrowIfNull(processStarter);

        _installationPathProvider = installationPathProvider;
        _stagingPathPolicy = stagingPathPolicy;
        _protocolFileStore = protocolFileStore;
        _processStarter = processStarter;
    }

    public async Task StartAsync(VerifiedUpdatePackage verifiedPackage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifiedPackage);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCriticalStagingState(verifiedPackage);

        string installationDirectory = _installationPathProvider.GetInstallationDirectory();
        string installationAttemptDirectory =
            _stagingPathPolicy.CreateInstallationAttemptDirectoryPath(verifiedPackage.AttemptDirectory);
        string updaterDirectory = _stagingPathPolicy.GetUpdaterDirectory(installationAttemptDirectory);
        string rollbackDirectory = _stagingPathPolicy.GetRollbackDirectory(installationAttemptDirectory);
        string requestPath = _stagingPathPolicy.GetInstallRequestPath(installationAttemptDirectory);
        string receiptPath = _stagingPathPolicy.GetInstallReceiptPath(installationAttemptDirectory);
        string acknowledgementPath = _stagingPathPolicy.GetRestartVerificationPath(installationAttemptDirectory);

        try
        {
            Directory.CreateDirectory(updaterDirectory);
            CopyUpdaterRuntime(installationDirectory, updaterDirectory, cancellationToken);

            string attemptId = Path.GetFileName(installationAttemptDirectory);
            string verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

            UpdateInstallationRequest request = new()
            {
                AttemptId = attemptId,
                MainProcessId = Environment.ProcessId,
                InstallationDirectory = installationDirectory,
                PayloadDirectory = verifiedPackage.PayloadDirectory,
                EntryExecutable = verifiedPackage.Package.EntryExecutable.Replace('\\', '/'),
                ExpectedApplicationVersion = verifiedPackage.Package.Version.ToString(),
                PackageId = verifiedPackage.Package.Id,
                RollbackDirectory = rollbackDirectory,
                ReceiptPath = receiptPath,
                AcknowledgementPath = acknowledgementPath,
                VerificationToken = verificationToken
            };

            UpdateInstallationReceipt receipt = new()
            {
                AttemptId = attemptId,
                PackageId = request.PackageId,
                ExpectedApplicationVersion = request.ExpectedApplicationVersion,
                InstallationDirectory = installationDirectory,
                RollbackDirectory = rollbackDirectory,
                AcknowledgementPath = acknowledgementPath,
                VerificationToken = verificationToken,
                Status = UpdateInstallationStatus.Pending,
                Message = "Update installer prepared."
            };

            await _protocolFileStore.WriteRequestAsync(requestPath, request, cancellationToken);
            await _protocolFileStore.WriteReceiptAsync(receiptPath, receipt, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            string updaterExecutablePath = Path.Combine(
                updaterDirectory,
                UpdateProtocolConstants.UpdaterExecutableFileName);

            ProcessStartInfo startInfo = new(updaterExecutablePath)
            {
                UseShellExecute = false,
                WorkingDirectory = updaterDirectory
            };
            startInfo.ArgumentList.Add(UpdateProtocolConstants.RequestArgument);
            startInfo.ArgumentList.Add(requestPath);

            Process? process;
            try
            {
                process = _processStarter(startInfo);
            }
            catch (Exception ex)
            {
                await TryWriteLaunchFailureAsync(receiptPath, receipt, ex.Message);
                throw new UpdateInstallationException(
                    UpdateInstallationFailureCode.HelperLaunchFailed,
                    $"Failed to start the staged update installer: {ex.Message}",
                    ex);
            }

            if (process is null)
            {
                await TryWriteLaunchFailureAsync(receiptPath, receipt, "The process could not be created.");
                throw new UpdateInstallationException(
                    UpdateInstallationFailureCode.HelperLaunchFailed,
                    "Failed to start the staged update installer process.");
            }

            process.Dispose();
        }
        catch (OperationCanceledException)
        {
            TryDeleteDirectory(installationAttemptDirectory);
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.PreparationFailed,
                $"Failed to prepare the update installer: {ex.Message}",
                ex);
        }
    }

    private void ValidateCriticalStagingState(VerifiedUpdatePackage verifiedPackage)
    {
        string updatesRoot = _stagingPathPolicy.GetUpdatesRootDirectory();
        if (!PathUtility.IsPathInsideDirectory(verifiedPackage.AttemptDirectory, updatesRoot) ||
            !File.Exists(verifiedPackage.ArchivePath) ||
            !Directory.Exists(verifiedPackage.PayloadDirectory) ||
            !File.Exists(verifiedPackage.EntryExecutablePath) ||
            !PathUtility.IsPathInsideDirectory(verifiedPackage.EntryExecutablePath, verifiedPackage.PayloadDirectory))
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.InvalidStaging,
                "The verified update staging state changed before the installer could be launched.");
        }

        string expectedArchivePath = _stagingPathPolicy.GetArchivePath(verifiedPackage.AttemptDirectory);
        string expectedPayloadDirectory = _stagingPathPolicy.GetPayloadDirectory(verifiedPackage.AttemptDirectory);
        string expectedEntryExecutable = Path.GetFullPath(
            Path.Combine(
                verifiedPackage.PayloadDirectory,
                verifiedPackage.Package.EntryExecutable.Replace('/', Path.DirectorySeparatorChar)));

        if (!PathUtility.PathEquals(verifiedPackage.ArchivePath, expectedArchivePath) ||
            !PathUtility.PathEquals(verifiedPackage.PayloadDirectory, expectedPayloadDirectory) ||
            !PathUtility.PathEquals(verifiedPackage.EntryExecutablePath, expectedEntryExecutable))
        {
            throw new UpdateInstallationException(
                UpdateInstallationFailureCode.InvalidStaging,
                "The verified update package no longer matches the expected staging layout.");
        }
    }

    private static void CopyUpdaterRuntime(
        string installationDirectory,
        string updaterDirectory,
        CancellationToken cancellationToken)
    {
        foreach (string fileName in UpdateProtocolConstants.RequiredUpdaterRuntimeFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sourcePath = Path.Combine(installationDirectory, fileName);
            if (!File.Exists(sourcePath))
            {
                throw new UpdateInstallationException(
                    UpdateInstallationFailureCode.UpdaterRuntimeUnavailable,
                    $"The current installation is missing updater runtime file '{fileName}'.");
            }

            File.Copy(sourcePath, Path.Combine(updaterDirectory, fileName), overwrite: false);
        }
    }

    private async Task TryWriteLaunchFailureAsync(string receiptPath, UpdateInstallationReceipt receipt, string message)
    {
        try
        {
            await _protocolFileStore.WriteReceiptAsync(
                receiptPath,
                receipt with
                {
                    Status = UpdateInstallationStatus.Failed,
                    Message = $"Updater launch failed: {message}",
                    UpdatedAtUtc = DateTime.UtcNow
                });
        }
        catch
        {
            // The primary error is the process launch failure. Receipt update is best-effort.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Canceled preparation must not hide cancellation because staging cleanup failed.
        }
    }
}