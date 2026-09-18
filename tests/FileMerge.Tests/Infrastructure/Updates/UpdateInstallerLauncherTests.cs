using System.Diagnostics;
using System.IO;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Updates;
using FileMerger.UpdateProtocol;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class UpdateInstallerLauncherTests
{
    [Fact]
    public async Task StartAsync_Should_Stage_Updater_Write_Protocol_And_Start_From_Staging()
    {
        using TemporaryDirectory temp = new();
        TestContext context = CreateContext(temp.Path);
        ProcessStartInfo? capturedStartInfo = null;
        UpdateInstallerLauncher launcher = new(
            context.InstallationPathProvider,
            context.StagingPolicy,
            context.ProtocolStore,
            startInfo =>
            {
                capturedStartInfo = startInfo;
                return Process.GetCurrentProcess();
            });

        await launcher.StartAsync(context.VerifiedPackage);

        Assert.NotNull(capturedStartInfo);
        Assert.EndsWith(UpdateProtocolConstants.UpdaterExecutableFileName, capturedStartInfo!.FileName);
        Assert.Contains(UpdateProtocolConstants.RequestArgument, capturedStartInfo.ArgumentList);

        string requestPath = capturedStartInfo.ArgumentList[1];
        UpdateInstallationRequest request = await context.ProtocolStore.ReadRequestAsync(requestPath);
        UpdateInstallationReceipt receipt = await context.ProtocolStore.ReadReceiptAsync(request.ReceiptPath);

        Assert.Equal(Environment.ProcessId, request.MainProcessId);
        Assert.Equal(context.InstallationDirectory, request.InstallationDirectory);
        Assert.Equal(context.VerifiedPackage.PayloadDirectory, request.PayloadDirectory);
        Assert.Equal(context.VerifiedPackage.Package.Version.ToString(), request.ExpectedApplicationVersion);
        Assert.Equal(UpdateInstallationStatus.Pending, receipt.Status);
        Assert.True(
            File.Exists(
                Path.Combine(capturedStartInfo.WorkingDirectory, UpdateProtocolConstants.ProtocolAssemblyFileName)));
    }


    [Fact]
    public async Task StartAsync_Should_Reject_Staging_That_Changed_After_Preflight()
    {
        using TemporaryDirectory temp = new();
        TestContext context = CreateContext(temp.Path);
        File.Delete(context.VerifiedPackage.EntryExecutablePath);
        int processStarts = 0;
        UpdateInstallerLauncher launcher = new(
            context.InstallationPathProvider,
            context.StagingPolicy,
            context.ProtocolStore,
            _ =>
            {
                processStarts++;
                return Process.GetCurrentProcess();
            });

        UpdateInstallationException ex = await Assert.ThrowsAsync<UpdateInstallationException>(() =>
            launcher.StartAsync(context.VerifiedPackage));

        Assert.Equal(UpdateInstallationFailureCode.InvalidStaging, ex.FailureCode);
        Assert.Equal(0, processStarts);
    }

    [Fact]
    public async Task StartAsync_Should_Record_Failure_When_Process_Cannot_Be_Created()
    {
        using TemporaryDirectory temp = new();
        TestContext context = CreateContext(temp.Path);
        UpdateInstallerLauncher launcher = new(
            context.InstallationPathProvider,
            context.StagingPolicy,
            context.ProtocolStore,
            _ => null);

        UpdateInstallationException ex = await Assert.ThrowsAsync<UpdateInstallationException>(() =>
            launcher.StartAsync(context.VerifiedPackage));

        Assert.Equal(UpdateInstallationFailureCode.HelperLaunchFailed, ex.FailureCode);

        string installRoot = Path.Combine(context.VerifiedPackage.AttemptDirectory, "install");
        string receiptPath = Directory.EnumerateFiles(installRoot, "install-receipt.json", SearchOption.AllDirectories)
            .Single();
        UpdateInstallationReceipt receipt = await context.ProtocolStore.ReadReceiptAsync(receiptPath);
        Assert.Equal(UpdateInstallationStatus.Failed, receipt.Status);
    }

    [Fact]
    public async Task StartAsync_Should_Create_New_Installation_Attempt_For_Retry()
    {
        using TemporaryDirectory temp = new();
        TestContext context = CreateContext(temp.Path);
        UpdateInstallerLauncher launcher = new(
            context.InstallationPathProvider,
            context.StagingPolicy,
            context.ProtocolStore,
            _ => Process.GetCurrentProcess());

        await launcher.StartAsync(context.VerifiedPackage);
        await launcher.StartAsync(context.VerifiedPackage);

        string installRoot = Path.Combine(context.VerifiedPackage.AttemptDirectory, "install");
        Assert.Equal(2, Directory.EnumerateDirectories(installRoot).Count());
    }

    private static TestContext CreateContext(string root)
    {
        string installation = Path.Combine(root, "app");
        Directory.CreateDirectory(installation);
        foreach (string fileName in UpdateProtocolConstants.RequiredUpdaterRuntimeFiles)
            File.WriteAllText(Path.Combine(installation, fileName), fileName);

        UpdateStagingPathPolicy staging = new(Path.Combine(root, "local-app-data"));
        string attempt = staging.CreateAttemptDirectoryPath();
        Directory.CreateDirectory(attempt);
        string archive = staging.GetArchivePath(attempt);
        File.WriteAllText(archive, "archive");
        string payload = staging.GetPayloadDirectory(attempt);
        Directory.CreateDirectory(payload);
        string entry = Path.Combine(payload, "FileMerger.Wpf.exe");
        File.WriteAllText(entry, "exe");

        UpdatePackage package = new(
            "windows-any",
            SemanticVersion.Parse("1.2.4"),
            "windows",
            "any",
            "net10.0-windows",
            "frameworkDependent",
            "zip",
            new Uri("https://updates.example.test/filemerger.zip"),
            7,
            new string('a', 64),
            "FileMerger.Wpf.exe",
            null,
            null);

        VerifiedUpdatePackage verified = new(package, attempt, archive, payload, entry);
        return new TestContext(
            staging,
            new ApplicationInstallationPathProvider(installation),
            new UpdateProtocolFileStore(),
            verified,
            Path.GetFullPath(installation));
    }

    private sealed record TestContext(
        UpdateStagingPathPolicy StagingPolicy,
        ApplicationInstallationPathProvider InstallationPathProvider,
        UpdateProtocolFileStore ProtocolStore,
        VerifiedUpdatePackage VerifiedPackage,
        string InstallationDirectory);

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