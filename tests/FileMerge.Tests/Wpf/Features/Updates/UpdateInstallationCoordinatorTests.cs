using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.LaunchUpdateInstaller;
using FileMerger.Application.UseCases.PrepareUpdateInstallation;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Updates.Services;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Shared.Integration;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Tests.Wpf.Features.Updates;

public sealed class UpdateInstallationCoordinatorTests
{
    [Fact]
    public async Task PrepareAndLaunchAsync_Should_Stop_At_Preflight_Failure_Before_Close_Guards()
    {
        FakePreflightService preflight = new()
        {
            Exception = new UpdateInstallationException(
                UpdateInstallationFailureCode.InstallationDirectoryNotWritable,
                "read-only")
        };
        FakeCloseGuardService closeGuards = new();
        FakeInstallerLauncher launcher = new();
        FakeShutdownService shutdown = new();
        UpdateInstallationCoordinator coordinator = CreateCoordinator(
            preflight,
            closeGuards,
            launcher,
            shutdown,
            out _);

        UpdateInstallationCoordinationResult result = await coordinator.PrepareAndLaunchAsync(CreateVerifiedPackage());

        Assert.Equal(UpdateInstallationCoordinationOutcome.PreflightFailed, result.Outcome);
        Assert.Equal(0, closeGuards.PrepareCalls);
        Assert.Equal(0, launcher.Calls);
        Assert.Equal(0, shutdown.Calls);
    }

    [Fact]
    public async Task PrepareAndLaunchAsync_Should_Stop_When_Any_Workspace_Is_Busy()
    {
        FakeCloseGuardService closeGuards = new();
        FakeInstallerLauncher launcher = new();
        UpdateInstallationCoordinator coordinator = CreateCoordinator(
            new FakePreflightService(),
            closeGuards,
            launcher,
            new FakeShutdownService(),
            out WorkspaceTabManagerViewModel workspaceTabs);
        workspaceTabs.ActiveDocument.OperationStatus.IsBusy = true;

        UpdateInstallationCoordinationResult result = await coordinator.PrepareAndLaunchAsync(CreateVerifiedPackage());

        Assert.Equal(UpdateInstallationCoordinationOutcome.ActiveOperation, result.Outcome);
        Assert.Equal(0, closeGuards.PrepareCalls);
        Assert.Equal(0, launcher.Calls);
    }

    [Fact]
    public async Task PrepareAndLaunchAsync_Should_Delegate_Dirty_Workspace_Guarding_To_Window_Close_Guards()
    {
        FakeCloseGuardService closeGuards = new();
        FakeInstallerLauncher launcher = new();
        UpdateInstallationCoordinator coordinator = CreateCoordinator(
            new FakePreflightService(),
            closeGuards,
            launcher,
            new FakeShutdownService(),
            out WorkspaceTabManagerViewModel workspaceTabs);

        workspaceTabs.ActiveDocument.SessionSettings.OutputPath = @"D:\Output\dirty.txt";
        WorkspaceDocumentTestFactory.RefreshWorkspaceDirtyState(workspaceTabs.ActiveDocument);

        Assert.True(workspaceTabs.ActiveDocument.IsWorkspaceDirty);

        UpdateInstallationCoordinationResult result = await coordinator.PrepareAndLaunchAsync(CreateVerifiedPackage());

        Assert.Equal(UpdateInstallationCoordinationOutcome.ReadyToShutdown, result.Outcome);
        Assert.Equal(1, closeGuards.PrepareCalls);
        Assert.Equal(1, launcher.Calls);
    }

    [Fact]
    public async Task PrepareAndLaunchAsync_Should_Stop_When_Window_Guard_Rejects()
    {
        FakeCloseGuardService closeGuards = new() { PrepareResult = false };
        FakeInstallerLauncher launcher = new();
        UpdateInstallationCoordinator coordinator = CreateCoordinator(
            new FakePreflightService(),
            closeGuards,
            launcher,
            new FakeShutdownService(),
            out _);

        UpdateInstallationCoordinationResult result = await coordinator.PrepareAndLaunchAsync(CreateVerifiedPackage());

        Assert.Equal(UpdateInstallationCoordinationOutcome.GuardCanceled, result.Outcome);
        Assert.Equal(1, closeGuards.PrepareCalls);
        Assert.Equal(2, closeGuards.ResetCalls);
        Assert.Equal(0, launcher.Calls);
    }

    [Fact]
    public async Task PrepareAndLaunchAsync_Should_Reset_Guards_When_Helper_Launch_Fails()
    {
        FakeCloseGuardService closeGuards = new();
        FakeInstallerLauncher launcher = new()
        {
            Exception = new UpdateInstallationException(
                UpdateInstallationFailureCode.HelperLaunchFailed,
                "start failed")
        };
        UpdateInstallationCoordinator coordinator = CreateCoordinator(
            new FakePreflightService(),
            closeGuards,
            launcher,
            new FakeShutdownService(),
            out _);

        UpdateInstallationCoordinationResult result = await coordinator.PrepareAndLaunchAsync(CreateVerifiedPackage());

        Assert.Equal(UpdateInstallationCoordinationOutcome.HelperLaunchFailed, result.Outcome);
        Assert.True(closeGuards.ResetCalls >= 2);
        Assert.Equal(1, launcher.Calls);
    }

    [Fact]
    public async Task CommitShutdown_Should_Approve_Guards_Then_Shutdown_After_Helper_Start()
    {
        List<string> sequence = [];
        FakeCloseGuardService closeGuards = new(sequence);
        FakeInstallerLauncher launcher = new(sequence);
        FakeShutdownService shutdown = new(sequence);
        UpdateInstallationCoordinator coordinator = CreateCoordinator(
            new FakePreflightService(),
            closeGuards,
            launcher,
            shutdown,
            out _);

        UpdateInstallationCoordinationResult result = await coordinator.PrepareAndLaunchAsync(CreateVerifiedPackage());

        Assert.Equal(UpdateInstallationCoordinationOutcome.ReadyToShutdown, result.Outcome);
        Assert.Equal(0, shutdown.Calls);

        coordinator.CommitShutdown();

        Assert.Equal(1, closeGuards.CommitCalls);
        Assert.Equal(1, shutdown.Calls);
        Assert.Equal(new[] { "guard-prepare", "helper-start", "guard-commit", "shutdown" }, sequence);
    }

    private static UpdateInstallationCoordinator CreateCoordinator(
        FakePreflightService preflight,
        FakeCloseGuardService closeGuards,
        FakeInstallerLauncher launcher,
        FakeShutdownService shutdown,
        out WorkspaceTabManagerViewModel workspaceTabs)
    {
        FakeValidator validator = new();
        PrepareUpdateInstallationUseCase prepareUseCase = new(
            new FakeApplicationVersionProvider("1.2.3"),
            new FakeUpdaterVersionProvider("1.0.0"),
            new UpdateCompatibilityPolicy(),
            validator,
            preflight);
        LaunchUpdateInstallerUseCase launchUseCase = new(launcher);
        workspaceTabs = new WorkspaceTabManagerViewModel(new FakeWorkspaceDocumentFactory());

        return new UpdateInstallationCoordinator(prepareUseCase, launchUseCase, workspaceTabs, closeGuards, shutdown);
    }

    private static VerifiedUpdatePackage CreateVerifiedPackage()
    {
        UpdatePackage package = new(
            "windows-any",
            SemanticVersion.Parse("1.2.4"),
            "windows",
            "any",
            "net10.0-windows",
            "frameworkDependent",
            "zip",
            new Uri("https://updates.example.test/filemerger.zip"),
            123,
            new string('a', 64),
            "FileMerger.Wpf.exe",
            null,
            null);

        return new VerifiedUpdatePackage(
            package,
            "attempt",
            "attempt/package.zip",
            "attempt/payload",
            "attempt/payload/FileMerger.Wpf.exe");
    }

    private sealed class FakeApplicationVersionProvider(string version) : IApplicationVersionProvider
    {
        public string GetCurrentVersion() => version;
    }

    private sealed class FakeUpdaterVersionProvider(string version) : IUpdaterVersionProvider
    {
        public string GetCurrentUpdaterVersion() => version;
    }

    private sealed class FakeValidator : IUpdatePackageValidator
    {
        public Task<VerifiedUpdatePackage> ValidateAsync(
            DownloadedUpdatePackage downloadedPackage,
            IProgress<UpdatePackagePreparationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            VerifiedUpdatePackage verified = new(
                downloadedPackage.Package,
                downloadedPackage.AttemptDirectory,
                downloadedPackage.ArchivePath,
                downloadedPackage.AttemptDirectory + "/payload",
                downloadedPackage.AttemptDirectory + "/payload/FileMerger.Wpf.exe");
            return Task.FromResult(verified);
        }
    }

    private sealed class FakePreflightService : IUpdateInstallationPreflightService
    {
        public Exception? Exception { get; init; }

        public Task ValidateAsync(VerifiedUpdatePackage verifiedPackage, CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
                throw Exception;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeInstallerLauncher(List<string>? sequence = null) : IUpdateInstallerLauncher
    {
        public Exception? Exception { get; init; }
        public int Calls { get; private set; }

        public Task StartAsync(VerifiedUpdatePackage verifiedPackage, CancellationToken cancellationToken = default)
        {
            Calls++;
            sequence?.Add("helper-start");

            if (Exception is not null)
                throw Exception;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeCloseGuardService(List<string>? sequence = null) : IApplicationWindowCloseGuardService
    {
        public bool PrepareResult { get; init; } = true;
        public int PrepareCalls { get; private set; }
        public int CommitCalls { get; private set; }
        public int ResetCalls { get; private set; }

        public Task<bool> TryPrepareAsync(CancellationToken cancellationToken = default)
        {
            PrepareCalls++;
            sequence?.Add("guard-prepare");
            return Task.FromResult(PrepareResult);
        }

        public void CommitPreparedShutdown()
        {
            CommitCalls++;
            sequence?.Add("guard-commit");
        }

        public void ResetPreparedShutdown()
        {
            ResetCalls++;
        }
    }

    private sealed class FakeShutdownService(List<string>? sequence = null) : IApplicationShutdownService
    {
        public int Calls { get; private set; }

        public void Shutdown()
        {
            Calls++;
            sequence?.Add("shutdown");
        }
    }
}