using System.IO;
using FileMerger.UpdateProtocol;
using FileMerger.Updater;

namespace FileMerger.Tests.Updater;

public sealed class UpdateInstallerEngineTests
{
    // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
    [Fact]
    public async Task RunAsync_Should_Install_And_Require_Positive_Restart_Acknowledgement()
    {
        using TemporaryDirectory temp = new();
        TestContext context = await CreateContextAsync(temp.Path);
        FakeProcessService processes = new()
        {
            OnStart = (executable, arguments) =>
            {
                Assert.Equal(context.NewEntryExecutable, executable);
                string receiptPath = arguments[1];
                string token = arguments[3];
                UpdateInstallationReceipt receipt = context.ProtocolStore
                    .ReadReceiptAsync(receiptPath)
                    .GetAwaiter()
                    .GetResult();
                context.ProtocolStore.WriteAcknowledgementAsync(
                        receipt.AcknowledgementPath,
                        new UpdateRestartVerificationAcknowledgement
                        {
                            AttemptId = receipt.AttemptId,
                            VerificationToken = token,
                            ActualApplicationVersion = receipt.ExpectedApplicationVersion,
                            ExpectedVersionMatches = true
                        })
                    .GetAwaiter()
                    .GetResult();
                return 200;
            }
        };
        UpdateInstallerEngine engine = new(context.ProtocolStore, processes, new UpdateInstallationFileTransaction());

        bool result = await engine.RunAsync(context.RequestPath);

        Assert.True(result);
        Assert.Equal("new", await File.ReadAllTextAsync(Path.Combine(context.InstallationDirectory, "version.txt")));
        Assert.False(Directory.Exists(context.Request.RollbackDirectory));
        UpdateInstallationReceipt finalReceipt =
            await context.ProtocolStore.ReadReceiptAsync(context.Request.ReceiptPath);
        Assert.Equal(UpdateInstallationStatus.Installed, finalReceipt.Status);
        Assert.True(processes.WaitedForMainProcess);
    }

    // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local

    [Fact]
    public async Task RunAsync_Should_Roll_Back_When_New_Version_Is_Rejected()
    {
        using TemporaryDirectory temp = new();
        TestContext context = await CreateContextAsync(temp.Path);
        int startCall = 0;
        FakeProcessService processes = new()
        {
            OnStart = (_, arguments) =>
            {
                startCall++;
                if (startCall == 1)
                {
                    string receiptPath = arguments[1];
                    UpdateInstallationReceipt receipt = context.ProtocolStore
                        .ReadReceiptAsync(receiptPath)
                        .GetAwaiter()
                        .GetResult();
                    context.ProtocolStore.WriteAcknowledgementAsync(
                            receipt.AcknowledgementPath,
                            new UpdateRestartVerificationAcknowledgement
                            {
                                AttemptId = receipt.AttemptId,
                                VerificationToken = receipt.VerificationToken,
                                ActualApplicationVersion = "9.9.9",
                                ExpectedVersionMatches = false,
                                FailureMessage = "wrong version"
                            })
                        .GetAwaiter()
                        .GetResult();
                    return 201;
                }

                return 202;
            }
        };
        UpdateInstallerEngine engine = new(context.ProtocolStore, processes, new UpdateInstallationFileTransaction());

        bool result = await engine.RunAsync(context.RequestPath);

        Assert.False(result);
        Assert.Equal("old", await File.ReadAllTextAsync(Path.Combine(context.InstallationDirectory, "version.txt")));
        UpdateInstallationReceipt finalReceipt =
            await context.ProtocolStore.ReadReceiptAsync(context.Request.ReceiptPath);
        Assert.Equal(UpdateInstallationStatus.RolledBack, finalReceipt.Status);
        Assert.Contains(201, processes.TerminatedProcessIds);
        Assert.Equal(2, processes.StartCalls);
    }


    [Fact]
    public async Task RunAsync_Should_Roll_Back_When_Restart_Verification_Times_Out()
    {
        using TemporaryDirectory temp = new();
        TestContext context = await CreateContextAsync(temp.Path);
        int startCall = 0;
        FakeProcessService processes = new()
        {
            OnStart = (_, _) =>
            {
                startCall++;
                return startCall == 1 ? 301 : 302;
            }
        };
        UpdateInstallerEngine engine = CreateFastEngine(context.ProtocolStore, processes);

        bool result = await engine.RunAsync(context.RequestPath);

        Assert.False(result);
        Assert.Equal("old", await File.ReadAllTextAsync(Path.Combine(context.InstallationDirectory, "version.txt")));
        Assert.Contains(301, processes.TerminatedProcessIds);
        Assert.Equal(2, processes.StartCalls);
        UpdateInstallationReceipt finalReceipt =
            await context.ProtocolStore.ReadReceiptAsync(context.Request.ReceiptPath);
        Assert.Equal(UpdateInstallationStatus.RolledBack, finalReceipt.Status);
    }

    [Fact]
    public async Task RunAsync_Should_Roll_Back_When_New_Process_Cannot_Start()
    {
        using TemporaryDirectory temp = new();
        TestContext context = await CreateContextAsync(temp.Path);
        int startCall = 0;
        FakeProcessService processes = new()
        {
            OnStart = (_, _) =>
            {
                startCall++;
                if (startCall == 1)
                    throw new InvalidOperationException("restart failed");

                return 402;
            }
        };
        UpdateInstallerEngine engine = CreateFastEngine(context.ProtocolStore, processes);

        bool result = await engine.RunAsync(context.RequestPath);

        Assert.False(result);
        Assert.Equal("old", await File.ReadAllTextAsync(Path.Combine(context.InstallationDirectory, "version.txt")));
        Assert.Equal(2, processes.StartCalls);
        UpdateInstallationReceipt finalReceipt =
            await context.ProtocolStore.ReadReceiptAsync(context.Request.ReceiptPath);
        Assert.Equal(UpdateInstallationStatus.RolledBack, finalReceipt.Status);
        Assert.Contains("restart failed", finalReceipt.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_Should_Report_Failed_When_Rollback_Cannot_Be_Performed()
    {
        using TemporaryDirectory temp = new();
        TestContext context = await CreateContextAsync(temp.Path);
        FakeProcessService processes = new()
        {
            OnStart = (_, _) =>
            {
                Directory.Delete(context.Request.RollbackDirectory, recursive: true);
                return 501;
            }
        };
        UpdateInstallerEngine engine = CreateFastEngine(context.ProtocolStore, processes);

        bool result = await engine.RunAsync(context.RequestPath);

        Assert.False(result);
        Assert.Contains(501, processes.TerminatedProcessIds);
        UpdateInstallationReceipt finalReceipt =
            await context.ProtocolStore.ReadReceiptAsync(context.Request.ReceiptPath);
        Assert.Equal(UpdateInstallationStatus.Failed, finalReceipt.Status);
        Assert.Contains("rollback", finalReceipt.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task RunAsync_Should_Leave_Existing_Installation_Intact_When_Backup_Cannot_Be_Created()
    {
        using TemporaryDirectory temp = new();
        TestContext context = await CreateContextAsync(temp.Path);
        Directory.CreateDirectory(context.Request.RollbackDirectory);
        FakeProcessService processes = new();
        UpdateInstallerEngine engine = CreateFastEngine(context.ProtocolStore, processes);

        bool result = await engine.RunAsync(context.RequestPath);

        Assert.False(result);
        Assert.Equal("old", await File.ReadAllTextAsync(Path.Combine(context.InstallationDirectory, "version.txt")));
        Assert.Equal(1, processes.StartCalls);
        UpdateInstallationReceipt finalReceipt =
            await context.ProtocolStore.ReadReceiptAsync(context.Request.ReceiptPath);
        Assert.Equal(UpdateInstallationStatus.Failed, finalReceipt.Status);
        Assert.Contains(
            "before the existing application was replaced",
            finalReceipt.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_Should_Fail_Without_Replacement_When_Main_Process_Does_Not_Exit()
    {
        using TemporaryDirectory temp = new();
        TestContext context = await CreateContextAsync(temp.Path);
        FakeProcessService processes = new()
        {
            WaitForExitResult = false
        };
        UpdateInstallerEngine engine = new(context.ProtocolStore, processes, new UpdateInstallationFileTransaction());

        bool result = await engine.RunAsync(context.RequestPath);

        Assert.False(result);
        Assert.Equal("old", await File.ReadAllTextAsync(Path.Combine(context.InstallationDirectory, "version.txt")));
        Assert.Equal(0, processes.StartCalls);
        UpdateInstallationReceipt finalReceipt =
            await context.ProtocolStore.ReadReceiptAsync(context.Request.ReceiptPath);
        Assert.Equal(UpdateInstallationStatus.Failed, finalReceipt.Status);
    }


    private static UpdateInstallerEngine CreateFastEngine(
        UpdateProtocolFileStore protocolStore,
        IUpdaterProcessService processService)
    {
        return new UpdateInstallerEngine(
            protocolStore,
            processService,
            new UpdateInstallationFileTransaction(),
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(10));
    }

    private static async Task<TestContext> CreateContextAsync(string root)
    {
        string installation = Path.Combine(root, "installation");
        string payload = Path.Combine(root, "payload");
        string attempt = Path.Combine(root, "staging", "install", "attempt");
        string rollback = Path.Combine(attempt, "rollback");
        string requestPath = Path.Combine(attempt, "install-request.json");
        string receiptPath = Path.Combine(attempt, "install-receipt.json");
        string acknowledgementPath = Path.Combine(attempt, "restart-verification.json");
        Directory.CreateDirectory(installation);
        Directory.CreateDirectory(payload);
        Directory.CreateDirectory(attempt);

        await File.WriteAllTextAsync(Path.Combine(installation, "FileMerger.Wpf.exe"), "old-exe");
        await File.WriteAllTextAsync(Path.Combine(installation, "version.txt"), "old");
        await File.WriteAllTextAsync(Path.Combine(payload, "FileMerger.Wpf.exe"), "new-exe");
        await File.WriteAllTextAsync(Path.Combine(payload, "version.txt"), "new");

        UpdateInstallationRequest request = new()
        {
            AttemptId = "attempt",
            MainProcessId = 123,
            InstallationDirectory = installation,
            PayloadDirectory = payload,
            EntryExecutable = "FileMerger.Wpf.exe",
            ExpectedApplicationVersion = "1.2.4",
            PackageId = "windows-any",
            RollbackDirectory = rollback,
            ReceiptPath = receiptPath,
            AcknowledgementPath = acknowledgementPath,
            VerificationToken = "token"
        };
        UpdateInstallationReceipt receipt = new()
        {
            AttemptId = request.AttemptId,
            PackageId = request.PackageId,
            ExpectedApplicationVersion = request.ExpectedApplicationVersion,
            InstallationDirectory = request.InstallationDirectory,
            RollbackDirectory = request.RollbackDirectory,
            AcknowledgementPath = request.AcknowledgementPath,
            VerificationToken = request.VerificationToken,
            Status = UpdateInstallationStatus.Pending
        };

        UpdateProtocolFileStore store = new();
        await store.WriteRequestAsync(requestPath, request);
        await store.WriteReceiptAsync(receiptPath, receipt);

        return new TestContext(
            store,
            request,
            requestPath,
            installation,
            Path.Combine(installation, "FileMerger.Wpf.exe"));
    }

    private sealed record TestContext(
        UpdateProtocolFileStore ProtocolStore,
        UpdateInstallationRequest Request,
        string RequestPath,
        string InstallationDirectory,
        string NewEntryExecutable);

    private sealed class FakeProcessService : IUpdaterProcessService
    {
        public bool WaitForExitResult { get; init; } = true;
        public Func<string, IReadOnlyList<string>, int>? OnStart { get; init; }
        public bool WaitedForMainProcess { get; private set; }
        public int StartCalls { get; private set; }
        public List<int> TerminatedProcessIds { get; } = [];

        public Task<bool> WaitForProcessExitAsync(
            int processId,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            WaitedForMainProcess = true;
            return Task.FromResult(WaitForExitResult);
        }

        public int StartProcess(string executablePath, IReadOnlyList<string> arguments)
        {
            StartCalls++;
            return OnStart?.Invoke(executablePath, arguments) ?? 200 + StartCalls;
        }

        public Task TryTerminateProcessAsync(int processId, CancellationToken cancellationToken = default)
        {
            TerminatedProcessIds.Add(processId);
            return Task.CompletedTask;
        }
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