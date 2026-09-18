using FileMerger.UpdateProtocol;

namespace FileMerger.Updater;

public sealed class UpdateInstallerEngine
{
    private static readonly TimeSpan _defaultMainProcessExitTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _defaultRestartVerificationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _defaultVerificationPollInterval = TimeSpan.FromMilliseconds(250);

    private readonly UpdateInstallationFileTransaction _fileTransaction;
    private readonly TimeSpan _mainProcessExitTimeout;
    private readonly IUpdaterProcessService _processService;
    private readonly UpdateProtocolFileStore _protocolFileStore;
    private readonly TimeSpan _restartVerificationTimeout;
    private readonly TimeSpan _verificationPollInterval;

    public UpdateInstallerEngine(
        UpdateProtocolFileStore protocolFileStore,
        IUpdaterProcessService processService,
        UpdateInstallationFileTransaction fileTransaction) : this(
        protocolFileStore,
        processService,
        fileTransaction,
        _defaultMainProcessExitTimeout,
        _defaultRestartVerificationTimeout,
        _defaultVerificationPollInterval)
    {
    }

    public UpdateInstallerEngine(
        UpdateProtocolFileStore protocolFileStore,
        IUpdaterProcessService processService,
        UpdateInstallationFileTransaction fileTransaction,
        TimeSpan mainProcessExitTimeout,
        TimeSpan restartVerificationTimeout,
        TimeSpan verificationPollInterval)
    {
        ArgumentNullException.ThrowIfNull(protocolFileStore);
        ArgumentNullException.ThrowIfNull(processService);
        ArgumentNullException.ThrowIfNull(fileTransaction);

        if (mainProcessExitTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(mainProcessExitTimeout));
        if (restartVerificationTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(restartVerificationTimeout));
        if (verificationPollInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(verificationPollInterval));

        _protocolFileStore = protocolFileStore;
        _processService = processService;
        _fileTransaction = fileTransaction;
        _mainProcessExitTimeout = mainProcessExitTimeout;
        _restartVerificationTimeout = restartVerificationTimeout;
        _verificationPollInterval = verificationPollInterval;
    }

    public async Task<bool> RunAsync(string requestPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestPath);

        UpdateInstallationRequest request = await _protocolFileStore.ReadRequestAsync(requestPath, cancellationToken);
        ValidateRequest(requestPath, request);

        UpdateInstallationReceipt receipt = await ReadOrCreateReceiptAsync(request, cancellationToken);

        bool mainExited = await _processService.WaitForProcessExitAsync(
            request.MainProcessId,
            _mainProcessExitTimeout,
            cancellationToken);

        if (!mainExited)
        {
            await WriteReceiptAsync(
                request,
                receipt,
                UpdateInstallationStatus.Failed,
                "The running FileMerger process did not exit before the installation timeout.",
                CancellationToken.None);
            return false;
        }

        int restartedProcessId = 0;
        bool backupCompleted = false;
        bool installationMutationStarted = false;

        try
        {
            receipt = await WriteReceiptAsync(
                request,
                receipt,
                UpdateInstallationStatus.Installing,
                "Creating rollback backup and replacing the application files.",
                cancellationToken);

            await _fileTransaction.CreateBackupAsync(
                request.InstallationDirectory,
                request.RollbackDirectory,
                cancellationToken);
            backupCompleted = true;

            installationMutationStarted = true;
            await _fileTransaction.ReplaceInstallationAsync(
                request.InstallationDirectory,
                request.PayloadDirectory,
                cancellationToken);

            TryDeleteFile(request.AcknowledgementPath);

            receipt = await WriteReceiptAsync(
                request,
                receipt,
                UpdateInstallationStatus.PendingVerification,
                "The new application files are installed and awaiting restart verification.",
                cancellationToken);

            string newEntryExecutable = UpdaterPathUtility.ResolveSafeRelativePath(
                request.InstallationDirectory,
                request.EntryExecutable);

            restartedProcessId = _processService.StartProcess(
                newEntryExecutable,
                [
                    UpdateProtocolConstants.VerificationReceiptArgument,
                    request.ReceiptPath,
                    UpdateProtocolConstants.VerificationTokenArgument,
                    request.VerificationToken
                ]);

            UpdateRestartVerificationAcknowledgement? acknowledgement =
                await WaitForAcknowledgementAsync(request, cancellationToken);

            if (IsSuccessfulAcknowledgement(request, acknowledgement))
            {
                await WriteReceiptAsync(
                    request,
                    receipt,
                    UpdateInstallationStatus.Installed,
                    $"Application version '{acknowledgement!.ActualApplicationVersion}' started and confirmed the update.",
                    CancellationToken.None);

                TryDeleteDirectory(request.RollbackDirectory);
                return true;
            }

            string verificationFailure = acknowledgement?.FailureMessage ??
                                         "The restarted application did not confirm the expected version before the timeout.";

            await _processService.TryTerminateProcessAsync(restartedProcessId, CancellationToken.None);
            return await RollBackAsync(request, receipt, verificationFailure, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await _processService.TryTerminateProcessAsync(restartedProcessId, CancellationToken.None);

            if (backupCompleted && installationMutationStarted)
            {
                await RollBackAsync(
                    request,
                    receipt,
                    "The updater operation was canceled after installation began.",
                    CancellationToken.None);
            }
            else
            {
                await FailBeforeReplacementAsync(
                    request,
                    receipt,
                    "The updater operation was canceled before the installation was replaced.");
            }

            throw;
        }
        catch (Exception ex)
        {
            await _processService.TryTerminateProcessAsync(restartedProcessId, CancellationToken.None);

            if (backupCompleted && installationMutationStarted)
                return await RollBackAsync(request, receipt, ex.Message, CancellationToken.None);

            await FailBeforeReplacementAsync(request, receipt, ex.Message);
            return false;
        }
    }

    private async Task FailBeforeReplacementAsync(
        UpdateInstallationRequest request,
        UpdateInstallationReceipt receipt,
        string failureMessage)
    {
        UpdateInstallationReceipt failedReceipt = receipt with
        {
            Status = UpdateInstallationStatus.Failed,
            Message = $"Update installation failed before the existing application was replaced: {failureMessage}",
            UpdatedAtUtc = DateTime.UtcNow
        };
        await TryWriteReceiptAsync(request, failedReceipt);

        try
        {
            string existingEntryExecutable = UpdaterPathUtility.ResolveSafeRelativePath(
                request.InstallationDirectory,
                request.EntryExecutable);
            _processService.StartProcess(existingEntryExecutable, []);
        }
        catch (Exception restartException)
        {
            failedReceipt = failedReceipt with
            {
                Message =
                $"The existing installation was left unchanged, but FileMerger could not be restarted automatically: {restartException.Message}",
                UpdatedAtUtc = DateTime.UtcNow
            };
            await TryWriteReceiptAsync(request, failedReceipt);
        }
    }

    private async Task<bool> RollBackAsync(
        UpdateInstallationRequest request,
        UpdateInstallationReceipt receipt,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _fileTransaction.RestoreBackupAsync(
                request.InstallationDirectory,
                request.RollbackDirectory,
                cancellationToken);
        }
        catch (Exception rollbackException)
        {
            await TryWriteReceiptAsync(
                request,
                receipt,
                UpdateInstallationStatus.Failed,
                $"Update installation failed and rollback also failed: {rollbackException.Message}");
            return false;
        }

        UpdateInstallationReceipt rolledBackReceipt = receipt with
        {
            Status = UpdateInstallationStatus.RolledBack,
            Message = $"The update was rolled back: {failureMessage}",
            UpdatedAtUtc = DateTime.UtcNow
        };
        await TryWriteReceiptAsync(request, rolledBackReceipt);

        try
        {
            string restoredEntryExecutable = UpdaterPathUtility.ResolveSafeRelativePath(
                request.InstallationDirectory,
                request.EntryExecutable);
            _processService.StartProcess(restoredEntryExecutable, []);
        }
        catch (Exception restartException)
        {
            rolledBackReceipt = rolledBackReceipt with
            {
                Message =
                $"The previous installation was restored, but FileMerger could not be restarted automatically: {restartException.Message}",
                UpdatedAtUtc = DateTime.UtcNow
            };
            await TryWriteReceiptAsync(request, rolledBackReceipt);
        }

        return false;
    }

    private async Task TryWriteReceiptAsync(UpdateInstallationRequest request, UpdateInstallationReceipt receipt)
    {
        try
        {
            await _protocolFileStore.WriteReceiptAsync(request.ReceiptPath, receipt, CancellationToken.None);
        }
        catch
        {
            // Recovery of the application tree takes precedence over diagnostic receipt persistence.
        }
    }

    private async Task TryWriteReceiptAsync(
        UpdateInstallationRequest request,
        UpdateInstallationReceipt previous,
        UpdateInstallationStatus status,
        string message)
    {
        await TryWriteReceiptAsync(
            request,
            previous with
            {
                Status = status,
                Message = message,
                UpdatedAtUtc = DateTime.UtcNow
            });
    }

    private async Task<UpdateRestartVerificationAcknowledgement?> WaitForAcknowledgementAsync(
        UpdateInstallationRequest request,
        CancellationToken cancellationToken)
    {
        DateTime deadlineUtc = DateTime.UtcNow + _restartVerificationTimeout;

        while (DateTime.UtcNow < deadlineUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(request.AcknowledgementPath))
            {
                try
                {
                    return await _protocolFileStore.ReadAcknowledgementAsync(
                        request.AcknowledgementPath,
                        cancellationToken);
                }
                catch (IOException)
                {
                    // Atomic writes should prevent partial documents, but a transient file-system race can be retried.
                }
                catch (InvalidDataException)
                {
                    return null;
                }
            }

            await Task.Delay(_verificationPollInterval, cancellationToken);
        }

        return null;
    }

    private static bool IsSuccessfulAcknowledgement(
        UpdateInstallationRequest request,
        UpdateRestartVerificationAcknowledgement? acknowledgement)
    {
        return acknowledgement is not null &&
               acknowledgement is
                   { SchemaVersion: UpdateProtocolConstants.SchemaVersion, ExpectedVersionMatches: true } &&
               string.Equals(acknowledgement.AttemptId, request.AttemptId, StringComparison.Ordinal) &&
               string.Equals(acknowledgement.VerificationToken, request.VerificationToken, StringComparison.Ordinal);
    }

    private async Task<UpdateInstallationReceipt> ReadOrCreateReceiptAsync(
        UpdateInstallationRequest request,
        CancellationToken cancellationToken)
    {
        if (File.Exists(request.ReceiptPath))
            return await _protocolFileStore.ReadReceiptAsync(request.ReceiptPath, cancellationToken);

        UpdateInstallationReceipt receipt = new()
        {
            AttemptId = request.AttemptId,
            PackageId = request.PackageId,
            ExpectedApplicationVersion = request.ExpectedApplicationVersion,
            InstallationDirectory = request.InstallationDirectory,
            RollbackDirectory = request.RollbackDirectory,
            AcknowledgementPath = request.AcknowledgementPath,
            VerificationToken = request.VerificationToken,
            Status = UpdateInstallationStatus.Pending,
            Message = "Update installer started."
        };

        await _protocolFileStore.WriteReceiptAsync(request.ReceiptPath, receipt, cancellationToken);
        return receipt;
    }

    private async Task<UpdateInstallationReceipt> WriteReceiptAsync(
        UpdateInstallationRequest request,
        UpdateInstallationReceipt previous,
        UpdateInstallationStatus status,
        string message,
        CancellationToken cancellationToken)
    {
        UpdateInstallationReceipt updated = previous with
        {
            Status = status,
            Message = message,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _protocolFileStore.WriteReceiptAsync(request.ReceiptPath, updated, cancellationToken);
        return updated;
    }

    private static void ValidateRequest(string requestPath, UpdateInstallationRequest request)
    {
        if (request.SchemaVersion != UpdateProtocolConstants.SchemaVersion)
            throw new InvalidDataException($"Unsupported update request schema version '{request.SchemaVersion}'.");

        if (string.IsNullOrWhiteSpace(request.AttemptId) ||
            request.MainProcessId <= 0 ||
            string.IsNullOrWhiteSpace(request.PackageId) ||
            string.IsNullOrWhiteSpace(request.ExpectedApplicationVersion) ||
            string.IsNullOrWhiteSpace(request.VerificationToken))
        {
            throw new InvalidDataException("The update request is missing required identity fields.");
        }

        string[] absolutePaths =
        [
            requestPath,
            request.InstallationDirectory,
            request.PayloadDirectory,
            request.RollbackDirectory,
            request.ReceiptPath,
            request.AcknowledgementPath
        ];

        if (absolutePaths.Any(path => string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)))
            throw new InvalidDataException("The update request contains a non-absolute path.");

        if (!Directory.Exists(request.InstallationDirectory) || !Directory.Exists(request.PayloadDirectory))
            throw new InvalidDataException("The installation or verified payload directory does not exist.");

        if (UpdaterPathUtility.PathsOverlap(request.InstallationDirectory, request.PayloadDirectory) ||
            UpdaterPathUtility.PathsOverlap(request.InstallationDirectory, request.RollbackDirectory) ||
            UpdaterPathUtility.PathsOverlap(request.PayloadDirectory, request.RollbackDirectory))
        {
            throw new InvalidDataException("Installation, payload, and rollback directories must not overlap.");
        }

        string installationAttemptDirectory = Path.GetDirectoryName(Path.GetFullPath(requestPath)) ??
                                              throw new InvalidDataException(
                                                  "The update request has no parent directory.");

        if (!UpdaterPathUtility.IsPathInsideDirectory(request.ReceiptPath, installationAttemptDirectory) ||
            !UpdaterPathUtility.IsPathInsideDirectory(request.AcknowledgementPath, installationAttemptDirectory) ||
            !UpdaterPathUtility.IsPathInsideDirectory(request.RollbackDirectory, installationAttemptDirectory))
        {
            throw new InvalidDataException(
                "Update protocol artifacts must remain inside the installation attempt directory.");
        }

        string payloadEntryExecutable = UpdaterPathUtility.ResolveSafeRelativePath(
            request.PayloadDirectory,
            request.EntryExecutable);
        if (!File.Exists(payloadEntryExecutable))
            throw new InvalidDataException("The verified payload does not contain the declared entry executable.");
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // A stale acknowledgement will be rejected by token/attempt validation even if cleanup fails.
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
            // Successful verification is already recorded. Rollback cleanup is best-effort.
        }
    }
}