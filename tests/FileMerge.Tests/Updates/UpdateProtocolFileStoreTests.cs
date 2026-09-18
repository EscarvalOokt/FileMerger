using System.IO;
using FileMerger.UpdateProtocol;

namespace FileMerger.Tests.Updates;

public sealed class UpdateProtocolFileStoreTests
{
    [Fact]
    public async Task Request_Should_RoundTrip()
    {
        using TemporaryDirectory temp = new();
        UpdateProtocolFileStore store = new();
        string path = Path.Combine(temp.Path, "request.json");
        UpdateInstallationRequest request = CreateRequest(temp.Path);

        await store.WriteRequestAsync(path, request);
        UpdateInstallationRequest loaded = await store.ReadRequestAsync(path);

        Assert.Equal(request, loaded);
    }

    [Fact]
    public async Task Receipt_Should_RoundTrip_With_String_Enum()
    {
        using TemporaryDirectory temp = new();
        UpdateProtocolFileStore store = new();
        string path = Path.Combine(temp.Path, "receipt.json");
        UpdateInstallationReceipt receipt = new()
        {
            AttemptId = "attempt",
            PackageId = "windows-any",
            ExpectedApplicationVersion = "1.2.4",
            InstallationDirectory = Path.Combine(temp.Path, "app"),
            RollbackDirectory = Path.Combine(temp.Path, "rollback"),
            AcknowledgementPath = Path.Combine(temp.Path, "ack.json"),
            VerificationToken = "token",
            Status = UpdateInstallationStatus.PendingVerification,
            Message = "waiting"
        };

        await store.WriteReceiptAsync(path, receipt);
        string json = await File.ReadAllTextAsync(path);
        UpdateInstallationReceipt loaded = await store.ReadReceiptAsync(path);

        Assert.Contains("\"status\": \"PendingVerification\"", json, StringComparison.Ordinal);
        Assert.Equal(receipt, loaded);
    }

    [Fact]
    public async Task Acknowledgement_Should_RoundTrip()
    {
        using TemporaryDirectory temp = new();
        UpdateProtocolFileStore store = new();
        string path = Path.Combine(temp.Path, "ack.json");
        UpdateRestartVerificationAcknowledgement acknowledgement = new()
        {
            AttemptId = "attempt",
            VerificationToken = "token",
            ActualApplicationVersion = "1.2.4+build.7",
            ExpectedVersionMatches = true
        };

        await store.WriteAcknowledgementAsync(path, acknowledgement);
        UpdateRestartVerificationAcknowledgement loaded = await store.ReadAcknowledgementAsync(path);

        Assert.Equal(acknowledgement, loaded);
    }

    [Fact]
    public async Task WriteReceiptAsync_Should_Replace_Previous_Document()
    {
        using TemporaryDirectory temp = new();
        UpdateProtocolFileStore store = new();
        string path = Path.Combine(temp.Path, "receipt.json");
        UpdateInstallationReceipt first = new()
        {
            AttemptId = "attempt",
            PackageId = "package",
            ExpectedApplicationVersion = "1.0.0",
            InstallationDirectory = temp.Path,
            RollbackDirectory = Path.Combine(temp.Path, "rollback"),
            AcknowledgementPath = Path.Combine(temp.Path, "ack.json"),
            VerificationToken = "token",
            Status = UpdateInstallationStatus.Pending
        };
        UpdateInstallationReceipt second = first with
        {
            Status = UpdateInstallationStatus.Installed,
            Message = "done"
        };

        await store.WriteReceiptAsync(path, first);
        await store.WriteReceiptAsync(path, second);

        UpdateInstallationReceipt loaded = await store.ReadReceiptAsync(path);
        Assert.Equal(UpdateInstallationStatus.Installed, loaded.Status);
        Assert.Equal("done", loaded.Message);
        Assert.Empty(Directory.EnumerateFiles(temp.Path, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ReadRequestAsync_Should_Reject_Malformed_Json()
    {
        using TemporaryDirectory temp = new();
        UpdateProtocolFileStore store = new();
        string path = Path.Combine(temp.Path, "request.json");
        await File.WriteAllTextAsync(path, "{ invalid");

        await Assert.ThrowsAnyAsync<Exception>(() => store.ReadRequestAsync(path));
    }

    private static UpdateInstallationRequest CreateRequest(string root)
    {
        return new UpdateInstallationRequest
        {
            AttemptId = "attempt",
            MainProcessId = 123,
            InstallationDirectory = Path.Combine(root, "app"),
            PayloadDirectory = Path.Combine(root, "payload"),
            EntryExecutable = "FileMerger.Wpf.exe",
            ExpectedApplicationVersion = "1.2.4",
            PackageId = "windows-any",
            RollbackDirectory = Path.Combine(root, "rollback"),
            ReceiptPath = Path.Combine(root, "receipt.json"),
            AcknowledgementPath = Path.Combine(root, "ack.json"),
            VerificationToken = "token"
        };
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