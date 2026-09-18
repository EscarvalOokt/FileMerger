using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Common;
using FileMerger.UpdateProtocol;

namespace FileMerger.Infrastructure.Updates;

public sealed class UpdateRestartVerificationService : IUpdateRestartVerificationService
{
    private readonly IApplicationVersionProvider _applicationVersionProvider;
    private readonly UpdateProtocolFileStore _protocolFileStore;
    private readonly UpdateStagingPathPolicy _stagingPathPolicy;

    public UpdateRestartVerificationService(
        IApplicationVersionProvider applicationVersionProvider,
        UpdateProtocolFileStore protocolFileStore,
        UpdateStagingPathPolicy stagingPathPolicy)
    {
        ArgumentNullException.ThrowIfNull(applicationVersionProvider);
        ArgumentNullException.ThrowIfNull(protocolFileStore);
        ArgumentNullException.ThrowIfNull(stagingPathPolicy);

        _applicationVersionProvider = applicationVersionProvider;
        _protocolFileStore = protocolFileStore;
        _stagingPathPolicy = stagingPathPolicy;
    }

    public Task<UpdateRestartVerificationOutcome> PrepareStartupVerificationAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        return VerifyStartupAsync(arguments, completeStartup: false, cancellationToken: cancellationToken);
    }

    public Task<UpdateRestartVerificationOutcome> CompleteStartupVerificationAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        return VerifyStartupAsync(arguments, completeStartup: true, cancellationToken: cancellationToken);
    }

    private async Task<UpdateRestartVerificationOutcome> VerifyStartupAsync(
        IReadOnlyList<string> arguments,
        bool completeStartup,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        string? receiptPath = GetArgumentValue(arguments, UpdateProtocolConstants.VerificationReceiptArgument);
        if (receiptPath is null)
            return UpdateRestartVerificationOutcome.NotRequested;

        string? suppliedToken = GetArgumentValue(arguments, UpdateProtocolConstants.VerificationTokenArgument);
        if (string.IsNullOrWhiteSpace(suppliedToken))
            return UpdateRestartVerificationOutcome.Rejected;

        if (!Path.IsPathFullyQualified(receiptPath) ||
            !PathUtility.IsPathInsideDirectory(receiptPath, _stagingPathPolicy.GetUpdatesRootDirectory()))
        {
            return UpdateRestartVerificationOutcome.Rejected;
        }

        UpdateInstallationReceipt? receipt = null;
        try
        {
            receipt = await _protocolFileStore.ReadReceiptAsync(receiptPath, cancellationToken);

            if (receipt.SchemaVersion != UpdateProtocolConstants.SchemaVersion ||
                receipt.Status != UpdateInstallationStatus.PendingVerification ||
                string.IsNullOrWhiteSpace(receipt.AttemptId) ||
                !string.Equals(receipt.VerificationToken, suppliedToken, StringComparison.Ordinal))
            {
                await TryWriteNegativeAcknowledgementAsync(
                    receipt,
                    "The update verification receipt is invalid for this restart.",
                    cancellationToken);
                return UpdateRestartVerificationOutcome.Rejected;
            }

            string? receiptDirectory = Path.GetDirectoryName(Path.GetFullPath(receiptPath));
            if (string.IsNullOrWhiteSpace(receiptDirectory) ||
                !PathUtility.IsPathInsideDirectory(receipt.AcknowledgementPath, receiptDirectory))
            {
                return UpdateRestartVerificationOutcome.Rejected;
            }

            string actualVersionText = _applicationVersionProvider.GetCurrentVersion();
            if (!SemanticVersion.TryParse(actualVersionText, out SemanticVersion? actualVersion) ||
                !SemanticVersion.TryParse(receipt.ExpectedApplicationVersion, out SemanticVersion? expectedVersion))
            {
                await TryWriteNegativeAcknowledgementAsync(
                    receipt,
                    "The actual or expected application version is not a valid semantic version.",
                    cancellationToken,
                    actualVersionText);
                return UpdateRestartVerificationOutcome.Rejected;
            }

            if (actualVersion.CompareTo(expectedVersion) != 0)
            {
                await TryWriteNegativeAcknowledgementAsync(
                    receipt,
                    $"Expected application version '{receipt.ExpectedApplicationVersion}' but started '{actualVersionText}'.",
                    cancellationToken,
                    actualVersionText);
                return UpdateRestartVerificationOutcome.Rejected;
            }

            if (!completeStartup)
                return UpdateRestartVerificationOutcome.Pending;

            UpdateRestartVerificationAcknowledgement acknowledgement = new()
            {
                AttemptId = receipt.AttemptId,
                VerificationToken = receipt.VerificationToken,
                ActualApplicationVersion = actualVersionText,
                ExpectedVersionMatches = true,
                FailureMessage = null
            };

            await _protocolFileStore.WriteAcknowledgementAsync(
                receipt.AcknowledgementPath,
                acknowledgement,
                cancellationToken);

            return UpdateRestartVerificationOutcome.Accepted;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (receipt is not null)
            {
                await TryWriteNegativeAcknowledgementAsync(
                    receipt,
                    $"Restart verification failed: {ex.Message}",
                    CancellationToken.None);
            }

            return UpdateRestartVerificationOutcome.Rejected;
        }
    }

    private async Task TryWriteNegativeAcknowledgementAsync(
        UpdateInstallationReceipt receipt,
        string message,
        CancellationToken cancellationToken,
        string actualVersion = "unknown")
    {
        try
        {
            UpdateRestartVerificationAcknowledgement acknowledgement = new()
            {
                AttemptId = receipt.AttemptId,
                VerificationToken = receipt.VerificationToken,
                ActualApplicationVersion = actualVersion,
                ExpectedVersionMatches = false,
                FailureMessage = message
            };

            await _protocolFileStore.WriteAcknowledgementAsync(
                receipt.AcknowledgementPath,
                acknowledgement,
                cancellationToken);
        }
        catch
        {
            // The helper also has a bounded timeout. A failed negative acknowledgement is non-fatal here.
        }
    }

    private static string? GetArgumentValue(IReadOnlyList<string> arguments, string name)
    {
        for (int index = 0; index < arguments.Count; index++)
        {
            if (!string.Equals(arguments[index], name, StringComparison.Ordinal))
                continue;

            if (index + 1 >= arguments.Count)
                return string.Empty;

            return arguments[index + 1];
        }

        return null;
    }
}