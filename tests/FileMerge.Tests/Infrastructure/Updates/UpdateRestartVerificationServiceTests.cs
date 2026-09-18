using System.IO;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Updates;
using FileMerger.UpdateProtocol;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class UpdateRestartVerificationServiceTests
{
    [Fact]
    public async Task PrepareStartupVerificationAsync_Should_Return_NotRequested_Without_Verification_Arguments()
    {
        using TemporaryDirectory temp = new();
        UpdateRestartVerificationService service = CreateService(temp.Path, "1.2.4", out _, out _);

        UpdateRestartVerificationOutcome result = await service.PrepareStartupVerificationAsync([]);

        Assert.Equal(UpdateRestartVerificationOutcome.NotRequested, result);
    }

    [Fact]
    public async Task PrepareStartupVerificationAsync_Should_Return_Pending_Without_Writing_Acknowledgement()
    {
        using TemporaryDirectory temp = new();
        UpdateRestartVerificationService service = CreateService(
            temp.Path,
            "1.2.4+build.7",
            out UpdateInstallationReceipt receipt,
            out string receiptPath);

        UpdateRestartVerificationOutcome result = await service.PrepareStartupVerificationAsync(
            CreateArguments(receiptPath, receipt.VerificationToken));

        Assert.Equal(UpdateRestartVerificationOutcome.Pending, result);
        Assert.False(File.Exists(receipt.AcknowledgementPath));
    }

    [Fact]
    public async Task CompleteStartupVerificationAsync_Should_Accept_Expected_Version_And_Write_Acknowledgement()
    {
        using TemporaryDirectory temp = new();
        UpdateRestartVerificationService service = CreateService(
            temp.Path,
            "1.2.4+build.7",
            out UpdateInstallationReceipt receipt,
            out string receiptPath);
        string[] arguments = CreateArguments(receiptPath, receipt.VerificationToken);

        UpdateRestartVerificationOutcome prepareResult = await service.PrepareStartupVerificationAsync(arguments);
        UpdateRestartVerificationOutcome completeResult = await service.CompleteStartupVerificationAsync(arguments);

        Assert.Equal(UpdateRestartVerificationOutcome.Pending, prepareResult);
        Assert.Equal(UpdateRestartVerificationOutcome.Accepted, completeResult);

        UpdateRestartVerificationAcknowledgement acknowledgement =
            await new UpdateProtocolFileStore().ReadAcknowledgementAsync(receipt.AcknowledgementPath);
        Assert.True(acknowledgement.ExpectedVersionMatches);
        Assert.Equal(receipt.AttemptId, acknowledgement.AttemptId);
        Assert.Equal(receipt.VerificationToken, acknowledgement.VerificationToken);
        Assert.Equal("1.2.4+build.7", acknowledgement.ActualApplicationVersion);
    }

    [Fact]
    public async Task PrepareStartupVerificationAsync_Should_Reject_Wrong_Application_Version()
    {
        using TemporaryDirectory temp = new();
        UpdateRestartVerificationService service = CreateService(
            temp.Path,
            "1.2.5",
            out UpdateInstallationReceipt receipt,
            out string receiptPath);

        UpdateRestartVerificationOutcome result = await service.PrepareStartupVerificationAsync(
            CreateArguments(receiptPath, receipt.VerificationToken));

        Assert.Equal(UpdateRestartVerificationOutcome.Rejected, result);
        UpdateRestartVerificationAcknowledgement acknowledgement =
            await new UpdateProtocolFileStore().ReadAcknowledgementAsync(receipt.AcknowledgementPath);
        Assert.False(acknowledgement.ExpectedVersionMatches);
    }

    [Fact]
    public async Task PrepareStartupVerificationAsync_Should_Reject_Wrong_Token()
    {
        using TemporaryDirectory temp = new();
        UpdateRestartVerificationService service = CreateService(temp.Path, "1.2.4", out _, out string receiptPath);

        UpdateRestartVerificationOutcome result = await service.PrepareStartupVerificationAsync(
            CreateArguments(receiptPath, "wrong-token"));

        Assert.Equal(UpdateRestartVerificationOutcome.Rejected, result);
    }

    [Fact]
    public async Task PrepareStartupVerificationAsync_Should_Reject_Receipt_Outside_Updates_Root()
    {
        using TemporaryDirectory temp = new();
        UpdateStagingPathPolicy staging = new(Path.Combine(temp.Path, "local-app-data"));
        UpdateRestartVerificationService service = new(
            new FakeApplicationVersionProvider("1.2.4"),
            new UpdateProtocolFileStore(),
            staging);
        string foreignReceipt = Path.Combine(temp.Path, "foreign-receipt.json");

        UpdateRestartVerificationOutcome result = await service.PrepareStartupVerificationAsync(
            CreateArguments(foreignReceipt, "token"));

        Assert.Equal(UpdateRestartVerificationOutcome.Rejected, result);
    }

    [Fact]
    public async Task CompleteStartupVerificationAsync_Should_Revalidate_Receipt_Before_Writing_Acknowledgement()
    {
        using TemporaryDirectory temp = new();
        UpdateRestartVerificationService service = CreateService(
            temp.Path,
            "1.2.4",
            out UpdateInstallationReceipt receipt,
            out string receiptPath);
        string[] arguments = CreateArguments(receiptPath, receipt.VerificationToken);

        UpdateRestartVerificationOutcome prepareResult = await service.PrepareStartupVerificationAsync(arguments);
        Assert.Equal(UpdateRestartVerificationOutcome.Pending, prepareResult);
        Assert.False(File.Exists(receipt.AcknowledgementPath));

        UpdateProtocolFileStore store = new();
        await store.WriteReceiptAsync(
            receiptPath,
            receipt with
            {
                Status = UpdateInstallationStatus.Failed,
                Message = "changed after prepare"
            });

        UpdateRestartVerificationOutcome completeResult = await service.CompleteStartupVerificationAsync(arguments);

        Assert.Equal(UpdateRestartVerificationOutcome.Rejected, completeResult);
        UpdateRestartVerificationAcknowledgement acknowledgement =
            await store.ReadAcknowledgementAsync(receipt.AcknowledgementPath);
        Assert.False(acknowledgement.ExpectedVersionMatches);
    }

    private static UpdateRestartVerificationService CreateService(
        string root,
        string actualVersion,
        out UpdateInstallationReceipt receipt,
        out string receiptPath)
    {
        UpdateStagingPathPolicy staging = new(Path.Combine(root, "local-app-data"));
        string verifiedAttempt = staging.CreateAttemptDirectoryPath();
        string installAttempt = staging.CreateInstallationAttemptDirectoryPath(verifiedAttempt);
        Directory.CreateDirectory(installAttempt);
        receiptPath = staging.GetInstallReceiptPath(installAttempt);
        string acknowledgementPath = staging.GetRestartVerificationPath(installAttempt);

        receipt = new UpdateInstallationReceipt
        {
            AttemptId = Path.GetFileName(installAttempt),
            PackageId = "windows-any",
            ExpectedApplicationVersion = "1.2.4",
            InstallationDirectory = Path.Combine(root, "app"),
            RollbackDirectory = staging.GetRollbackDirectory(installAttempt),
            AcknowledgementPath = acknowledgementPath,
            VerificationToken = "verification-token",
            Status = UpdateInstallationStatus.PendingVerification,
            Message = "waiting"
        };

        UpdateProtocolFileStore store = new();
        store.WriteReceiptAsync(receiptPath, receipt).GetAwaiter().GetResult();

        return new UpdateRestartVerificationService(new FakeApplicationVersionProvider(actualVersion), store, staging);
    }

    private static string[] CreateArguments(string receiptPath, string token)
    {
        return
        [
            UpdateProtocolConstants.VerificationReceiptArgument,
            receiptPath,
            UpdateProtocolConstants.VerificationTokenArgument,
            token
        ];
    }

    private sealed class FakeApplicationVersionProvider(string version) : IApplicationVersionProvider
    {
        public string GetCurrentVersion() => version;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"FileMergerTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Test cleanup is best-effort.
            }
            catch (UnauthorizedAccessException)
            {
                // Test cleanup is best-effort.
            }
        }
    }
}