using System.Net.Http;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.CheckForUpdates;
using FileMerger.Application.UseCases.DownloadAndValidateUpdatePackage;
using FileMerger.Wpf.Features.Updates.Services;
using FileMerger.Wpf.Features.Updates.ViewModels;
using FileMerger.Wpf.Shared.Status;

namespace FileMerger.Tests.Wpf.Features.Updates;

public sealed class UpdateCheckDialogViewModelTests
{
    [Fact]
    public void Constructor_Should_Initialize_Idle_State()
    {
        ViewModelContext context = CreateContext("1.2.3", new FakeReleaseManifestSource(CreateManifest("1.2.4")));

        Assert.Equal("Checking...", context.ViewModel.CurrentVersionText);
        Assert.Equal("Not checked", context.ViewModel.AvailableVersionText);
        Assert.Null(context.ViewModel.CurrentVersionFullText);
        Assert.Null(context.ViewModel.AvailableVersionFullText);
        Assert.False(context.ViewModel.OperationStatus.IsBusy);
        Assert.False(context.ViewModel.OperationStatus.IsProgressVisible);
        Assert.Equal(StatusSeverity.Info, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.True(context.ViewModel.CheckAgainCommand.CanExecute(null));
        Assert.False(context.ViewModel.DownloadCommand.CanExecute(null));
        Assert.False(context.ViewModel.InstallCommand.CanExecute(null));
        Assert.False(context.ViewModel.IsPackageVerified);
        Assert.Null(context.ViewModel.VerifiedPackage);
    }

    [Fact]
    public async Task CheckAsync_Should_Show_Available_Version_And_Enable_Download()
    {
        ViewModelContext context = CreateContext("1.2.3", new FakeReleaseManifestSource(CreateManifest("1.2.4")));

        await context.ViewModel.CheckAsync();

        Assert.Equal("1.2.3", context.ViewModel.CurrentVersionText);
        Assert.Equal("1.2.3", context.ViewModel.CurrentVersionFullText);
        Assert.Equal("1.2.4", context.ViewModel.AvailableVersionText);
        Assert.Equal("1.2.4", context.ViewModel.AvailableVersionFullText);
        Assert.Equal(StatusSeverity.Success, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains(
            "newer compatible version",
            context.ViewModel.OperationStatus.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
        Assert.False(context.ViewModel.OperationStatus.IsBusy);
        Assert.False(context.ViewModel.OperationStatus.IsProgressVisible);
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAsync_Should_Show_Concise_Current_Version_And_Expose_Full_Build_Identifier()
    {
        ViewModelContext context = CreateContext(
            "1.2.3+branch.main.sha.abcdef123",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")));

        await context.ViewModel.CheckAsync();

        Assert.Equal("1.2.3", context.ViewModel.CurrentVersionText);
        Assert.Equal("1.2.3+branch.main.sha.abcdef123", context.ViewModel.CurrentVersionFullText);
        Assert.Equal("1.2.4", context.ViewModel.AvailableVersionText);
        Assert.Equal("1.2.4", context.ViewModel.AvailableVersionFullText);
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAsync_Should_Preserve_Prerelease_In_Concise_Version_And_Expose_Full_Identifiers()
    {
        ViewModelContext context = CreateContext(
            "0.2.0-alpha.3+sha.abcdef",
            new FakeReleaseManifestSource(CreateManifest("0.2.0-alpha.4+build.release")));

        await context.ViewModel.CheckAsync();

        Assert.Equal("0.2.0-alpha.3", context.ViewModel.CurrentVersionText);
        Assert.Equal("0.2.0-alpha.3+sha.abcdef", context.ViewModel.CurrentVersionFullText);
        Assert.Equal("0.2.0-alpha.4", context.ViewModel.AvailableVersionText);
        Assert.Equal("0.2.0-alpha.4+build.release", context.ViewModel.AvailableVersionFullText);
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAsync_Should_Show_NoUpdate_As_Info_And_Keep_Download_Disabled()
    {
        ViewModelContext context = CreateContext("1.2.4", new FakeReleaseManifestSource(CreateManifest("1.2.4")));

        await context.ViewModel.CheckAsync();

        Assert.Equal("1.2.4", context.ViewModel.CurrentVersionText);
        Assert.Equal("1.2.4", context.ViewModel.CurrentVersionFullText);
        Assert.Equal("1.2.4", context.ViewModel.AvailableVersionText);
        Assert.Equal("1.2.4", context.ViewModel.AvailableVersionFullText);
        Assert.Equal(StatusSeverity.Info, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains(
            "No newer compatible update",
            context.ViewModel.OperationStatus.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
        Assert.False(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAsync_Should_Show_Failure_Distinct_From_NoUpdate_And_Keep_Download_Disabled()
    {
        FakeReleaseManifestSource source = new(CreateManifest("1.2.4"))
        {
            Exception = new HttpRequestException("offline")
        };
        ViewModelContext context = CreateContext("1.2.3", source);

        await context.ViewModel.CheckAsync();

        Assert.Equal("1.2.3", context.ViewModel.CurrentVersionText);
        Assert.Equal("1.2.3", context.ViewModel.CurrentVersionFullText);
        Assert.Equal("Unavailable", context.ViewModel.AvailableVersionText);
        Assert.Null(context.ViewModel.AvailableVersionFullText);
        Assert.Equal(StatusSeverity.Error, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains("failed", context.ViewModel.OperationStatus.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("offline", context.ViewModel.OperationStatus.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAsync_Should_Clear_Stale_Result_While_Rechecking()
    {
        TaskCompletionSource<string> secondCheck = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int call = 0;
        FakeReleaseManifestSource source = new(CreateManifest("1.2.4"))
        {
            Handler = _ =>
            {
                call++;
                return call == 1 ? Task.FromResult(CreateManifest("1.2.4")) : secondCheck.Task;
            }
        };
        ViewModelContext context = CreateContext("1.2.3", source);

        await context.ViewModel.CheckAsync();
        Assert.Equal("1.2.3", context.ViewModel.CurrentVersionFullText);
        Assert.Equal("1.2.4", context.ViewModel.AvailableVersionText);
        Assert.Equal("1.2.4", context.ViewModel.AvailableVersionFullText);
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));

        Task recheckTask = context.ViewModel.CheckAsync();

        Assert.Equal("Checking...", context.ViewModel.CurrentVersionText);
        Assert.Equal("Not checked", context.ViewModel.AvailableVersionText);
        Assert.Null(context.ViewModel.CurrentVersionFullText);
        Assert.Null(context.ViewModel.AvailableVersionFullText);
        Assert.True(context.ViewModel.OperationStatus.IsBusy);
        Assert.True(context.ViewModel.OperationStatus.IsProgressVisible);
        Assert.False(context.ViewModel.CheckAgainCommand.CanExecute(null));
        Assert.False(context.ViewModel.DownloadCommand.CanExecute(null));

        secondCheck.SetResult(CreateManifest("1.2.5"));
        await recheckTask;

        Assert.Equal("1.2.5", context.ViewModel.AvailableVersionText);
        Assert.Equal("1.2.5", context.ViewModel.AvailableVersionFullText);
        Assert.True(context.ViewModel.CheckAgainCommand.CanExecute(null));
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAsync_Should_Restore_Command_After_Failure()
    {
        FakeReleaseManifestSource source = new(CreateManifest("1.2.4"))
        {
            Exception = new HttpRequestException("offline")
        };
        ViewModelContext context = CreateContext("1.2.3", source);

        await context.ViewModel.CheckAsync();

        Assert.False(context.ViewModel.OperationStatus.IsBusy);
        Assert.True(context.ViewModel.CheckAgainCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAsync_Should_Not_Start_A_Second_Check_While_Busy()
    {
        TaskCompletionSource<string> manifest = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeReleaseManifestSource source = new(CreateManifest("1.2.4"))
        {
            Handler = _ => manifest.Task
        };
        ViewModelContext context = CreateContext("1.2.3", source);

        Task firstCheck = context.ViewModel.CheckAsync();
        Task secondCheck = context.ViewModel.CheckAsync();

        Assert.Equal(1, source.Calls);
        Assert.True(secondCheck.IsCompletedSuccessfully);

        manifest.SetResult(CreateManifest("1.2.4"));
        await firstCheck;
    }

    [Fact]
    public async Task DownloadAsync_Should_Verify_Selected_Package()
    {
        ViewModelContext context = CreateContext("1.2.3", new FakeReleaseManifestSource(CreateManifest("1.2.4")));
        await context.ViewModel.CheckAsync();

        await context.ViewModel.DownloadAsync();

        Assert.True(context.ViewModel.IsPackageVerified);
        Assert.NotNull(context.ViewModel.VerifiedPackage);
        Assert.Equal("1.2.4", context.ViewModel.VerifiedPackage!.Package.Version.ToString());
        Assert.Equal(StatusSeverity.Success, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains(
            "verified",
            context.ViewModel.OperationStatus.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
        Assert.False(context.ViewModel.DownloadCommand.CanExecute(null));
        Assert.False(context.ViewModel.OperationStatus.IsCancelable);
        Assert.False(context.ViewModel.OperationStatus.IsProgressVisible);
    }

    [Fact]
    public async Task DownloadAsync_Should_Show_Failure_And_Allow_Retry()
    {
        FakeUpdatePackageDownloader downloader = new()
        {
            Exception = new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.DownloadUnavailable,
                "offline")
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            downloader);
        await context.ViewModel.CheckAsync();

        await context.ViewModel.DownloadAsync();

        Assert.False(context.ViewModel.IsPackageVerified);
        Assert.Null(context.ViewModel.VerifiedPackage);
        Assert.Equal(StatusSeverity.Error, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains("offline", context.ViewModel.OperationStatus.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task DownloadAsync_Should_Show_Validation_Failure_Without_Verified_State()
    {
        FakeUpdatePackageValidator validator = new()
        {
            Exception = new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.HashMismatch,
                "hash mismatch")
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            validator: validator);
        await context.ViewModel.CheckAsync();

        await context.ViewModel.DownloadAsync();

        Assert.False(context.ViewModel.IsPackageVerified);
        Assert.Null(context.ViewModel.VerifiedPackage);
        Assert.Equal(StatusSeverity.Error, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains(
            "hash mismatch",
            context.ViewModel.OperationStatus.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CancelCommand_Should_Cancel_Active_Download()
    {
        TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeUpdatePackageDownloader downloader = new()
        {
            Handler = async (package, _, cancellationToken) =>
            {
                started.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return CreateDownloaded(package);
            }
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            downloader);
        await context.ViewModel.CheckAsync();

        Task downloadTask = context.ViewModel.DownloadAsync();
        await started.Task;

        Assert.True(context.ViewModel.OperationStatus.IsBusy);
        Assert.True(context.ViewModel.OperationStatus.IsCancelable);
        Assert.True(context.ViewModel.OperationStatus.CancelCommand.CanExecute(null));

        context.ViewModel.OperationStatus.CancelCommand.Execute(null);
        await downloadTask;

        Assert.False(context.ViewModel.IsPackageVerified);
        Assert.Equal(StatusSeverity.Info, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains(
            "canceled",
            context.ViewModel.OperationStatus.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadAsync_Should_Show_Determinate_Byte_Progress()
    {
        TaskCompletionSource<bool> progressReported = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeUpdatePackageDownloader downloader = new()
        {
            Handler = async (package, progress, cancellationToken) =>
            {
                progress!.Report(
                    new UpdatePackagePreparationProgress(
                        UpdatePackagePreparationStage.Downloading,
                        50,
                        100,
                        "Downloading update package..."));
                progressReported.TrySetResult(true);
                await release.Task.WaitAsync(cancellationToken);
                return CreateDownloaded(package);
            }
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            downloader);
        await context.ViewModel.CheckAsync();

        Task downloadTask = context.ViewModel.DownloadAsync();
        await progressReported.Task;
        await WaitUntilAsync(() =>
            context.ViewModel.OperationStatus is { IsProgressIndeterminate: false, ProgressValue: 50 } &&
            context.ViewModel.OperationStatus.ProgressMessage.Contains(
                "Downloading",
                StringComparison.OrdinalIgnoreCase));

        Assert.Equal(50, context.ViewModel.OperationStatus.ProgressValue);
        Assert.Contains(
            "Downloading",
            context.ViewModel.OperationStatus.ProgressMessage,
            StringComparison.OrdinalIgnoreCase);

        release.TrySetResult(true);
        await downloadTask;
    }

    [Fact]
    public async Task DownloadAsync_Should_Show_Indeterminate_Validation_Progress()
    {
        TaskCompletionSource<bool> validationReported = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeUpdatePackageValidator validator = new()
        {
            Handler = async (downloaded, progress, cancellationToken) =>
            {
                progress!.Report(
                    new UpdatePackagePreparationProgress(
                        UpdatePackagePreparationStage.ValidatingHash,
                        0,
                        0,
                        "Verifying update package integrity..."));
                validationReported.TrySetResult(true);
                await release.Task.WaitAsync(cancellationToken);
                return CreateVerified(downloaded);
            }
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            validator: validator);
        await context.ViewModel.CheckAsync();

        Task downloadTask = context.ViewModel.DownloadAsync();
        await validationReported.Task;
        await WaitUntilAsync(() =>
            context.ViewModel.OperationStatus.IsProgressIndeterminate &&
            context.ViewModel.OperationStatus.ProgressMessage.Contains(
                "integrity",
                StringComparison.OrdinalIgnoreCase));

        Assert.True(context.ViewModel.OperationStatus.IsProgressVisible);
        Assert.True(context.ViewModel.OperationStatus.IsProgressIndeterminate);

        release.TrySetResult(true);
        await downloadTask;
    }

    [Fact]
    public async Task CheckAgain_Should_Clear_Previously_Verified_Package()
    {
        FakeReleaseManifestSource source = new(CreateManifest("1.2.4"));
        ViewModelContext context = CreateContext("1.2.3", source);
        await context.ViewModel.CheckAsync();
        await context.ViewModel.DownloadAsync();
        Assert.True(context.ViewModel.IsPackageVerified);

        source.Json = CreateManifest("1.2.5");
        await context.ViewModel.CheckAsync();

        Assert.False(context.ViewModel.IsPackageVerified);
        Assert.Null(context.ViewModel.VerifiedPackage);
        Assert.Equal("1.2.5", context.ViewModel.AvailableVersionText);
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task InstallCommand_Should_Be_Enabled_Only_After_Verification()
    {
        ViewModelContext context = CreateContext("1.2.3", new FakeReleaseManifestSource(CreateManifest("1.2.4")));
        await context.ViewModel.CheckAsync();

        Assert.False(context.ViewModel.InstallCommand.CanExecute(null));

        await context.ViewModel.DownloadAsync();

        Assert.True(context.ViewModel.InstallCommand.CanExecute(null));
    }

    [Fact]
    public async Task InstallAsync_Should_Pass_Verified_Package_And_Commit_Shutdown_When_Ready()
    {
        ViewModelContext context = CreateContext("1.2.3", new FakeReleaseManifestSource(CreateManifest("1.2.4")));
        await context.ViewModel.CheckAsync();
        await context.ViewModel.DownloadAsync();
        VerifiedUpdatePackage verified = Assert.IsType<VerifiedUpdatePackage>(context.ViewModel.VerifiedPackage);

        await context.ViewModel.InstallAsync();

        Assert.Same(verified, context.InstallationCoordinator.LastPackage);
        Assert.Equal(1, context.InstallationCoordinator.PrepareCalls);
        Assert.Equal(1, context.InstallationCoordinator.CommitCalls);
        Assert.False(context.ViewModel.OperationStatus.IsCancelable);
    }

    [Fact]
    public async Task InstallAsync_Should_Keep_Verified_Package_When_Guard_Is_Canceled()
    {
        FakeUpdateInstallationCoordinator coordinator = new()
        {
            Result = UpdateInstallationCoordinationResult.GuardCanceled("canceled by guard")
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            installationCoordinator: coordinator);
        await context.ViewModel.CheckAsync();
        await context.ViewModel.DownloadAsync();

        await context.ViewModel.InstallAsync();

        Assert.NotNull(context.ViewModel.VerifiedPackage);
        Assert.True(context.ViewModel.InstallCommand.CanExecute(null));
        Assert.Equal(0, coordinator.CommitCalls);
        Assert.Equal(StatusSeverity.Info, context.ViewModel.OperationStatus.StatusSeverity);
    }

    [Fact]
    public async Task InstallAsync_Should_Show_ActiveOperation_As_Warning()
    {
        FakeUpdateInstallationCoordinator coordinator = new()
        {
            Result = UpdateInstallationCoordinationResult.ActiveOperation("workspace is busy")
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            installationCoordinator: coordinator);
        await context.ViewModel.CheckAsync();
        await context.ViewModel.DownloadAsync();

        await context.ViewModel.InstallAsync();

        Assert.NotNull(context.ViewModel.VerifiedPackage);
        Assert.Equal(StatusSeverity.Warning, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains("busy", context.ViewModel.OperationStatus.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, coordinator.CommitCalls);
    }

    [Fact]
    public async Task InstallAsync_Should_Show_PreflightFailure_And_Allow_Retry()
    {
        FakeUpdateInstallationCoordinator coordinator = new()
        {
            Result = UpdateInstallationCoordinationResult.PreflightFailed("installation directory is read-only")
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            installationCoordinator: coordinator);
        await context.ViewModel.CheckAsync();
        await context.ViewModel.DownloadAsync();

        await context.ViewModel.InstallAsync();

        Assert.NotNull(context.ViewModel.VerifiedPackage);
        Assert.True(context.ViewModel.InstallCommand.CanExecute(null));
        Assert.Equal(StatusSeverity.Error, context.ViewModel.OperationStatus.StatusSeverity);
        Assert.Contains(
            "read-only",
            context.ViewModel.OperationStatus.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, coordinator.CommitCalls);
    }


    [Fact]
    public async Task InstallAsync_Should_Clear_Verified_Package_When_Preflight_Invalidates_Staging()
    {
        FakeUpdateInstallationCoordinator coordinator = new()
        {
            Result = UpdateInstallationCoordinationResult.PreflightFailed(
                "verified package changed",
                UpdateInstallationFailureCode.PackageNoLongerValid)
        };
        ViewModelContext context = CreateContext(
            "1.2.3",
            new FakeReleaseManifestSource(CreateManifest("1.2.4")),
            installationCoordinator: coordinator);
        await context.ViewModel.CheckAsync();
        await context.ViewModel.DownloadAsync();

        await context.ViewModel.InstallAsync();

        Assert.Null(context.ViewModel.VerifiedPackage);
        Assert.False(context.ViewModel.InstallCommand.CanExecute(null));
        Assert.True(context.ViewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckAgain_Should_Clear_Installability()
    {
        FakeReleaseManifestSource source = new(CreateManifest("1.2.4"));
        ViewModelContext context = CreateContext("1.2.3", source);
        await context.ViewModel.CheckAsync();
        await context.ViewModel.DownloadAsync();
        Assert.True(context.ViewModel.InstallCommand.CanExecute(null));

        source.Json = CreateManifest("1.2.5");
        await context.ViewModel.CheckAsync();

        Assert.Null(context.ViewModel.VerifiedPackage);
        Assert.False(context.ViewModel.InstallCommand.CanExecute(null));
    }

    [Fact]
    public async Task Commands_Should_Not_Start_Parallel_Operations_While_Downloading()
    {
        TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeUpdatePackageDownloader downloader = new()
        {
            Handler = async (package, _, cancellationToken) =>
            {
                started.TrySetResult(true);
                await release.Task.WaitAsync(cancellationToken);
                return CreateDownloaded(package);
            }
        };
        FakeReleaseManifestSource source = new(CreateManifest("1.2.4"));
        ViewModelContext context = CreateContext("1.2.3", source, downloader);
        await context.ViewModel.CheckAsync();

        Task downloadTask = context.ViewModel.DownloadAsync();
        await started.Task;

        Assert.False(context.ViewModel.CheckAgainCommand.CanExecute(null));
        Assert.False(context.ViewModel.DownloadCommand.CanExecute(null));

        Task ignoredCheck = context.ViewModel.CheckAsync();
        Task ignoredDownload = context.ViewModel.DownloadAsync();
        Assert.True(ignoredCheck.IsCompletedSuccessfully);
        Assert.True(ignoredDownload.IsCompletedSuccessfully);
        Assert.Equal(1, source.Calls);
        Assert.Equal(1, downloader.Calls);

        release.TrySetResult(true);
        await downloadTask;
    }

    private static ViewModelContext CreateContext(
        string currentVersion,
        FakeReleaseManifestSource manifestSource,
        FakeUpdatePackageDownloader? downloader = null,
        FakeUpdatePackageValidator? validator = null,
        FakeUpdateInstallationCoordinator? installationCoordinator = null)
    {
        UpdateCompatibilityPolicy compatibilityPolicy = new();
        FakeUpdaterVersionProvider updaterVersionProvider = new("1.0.0");
        CheckForUpdatesUseCase checkUseCase = new(
            new FakeApplicationVersionProvider(currentVersion),
            updaterVersionProvider,
            manifestSource,
            new UpdateReleaseManifestParser(),
            compatibilityPolicy);

        downloader ??= new FakeUpdatePackageDownloader();
        validator ??= new FakeUpdatePackageValidator();
        installationCoordinator ??= new FakeUpdateInstallationCoordinator();

        DownloadAndValidateUpdatePackageUseCase downloadUseCase = new(
            compatibilityPolicy,
            updaterVersionProvider,
            downloader,
            validator);

        UpdateCheckDialogViewModel viewModel = new(checkUseCase, downloadUseCase, installationCoordinator);

        return new ViewModelContext(viewModel, installationCoordinator);
    }

    private static string CreateManifest(string releaseVersion)
    {
        return $$"""
                 {
                   "schemaVersion": 1,
                   "product": "FileMerger",
                   "release": {
                     "version": "{{releaseVersion}}",
                     "publishedAtUtc": "2026-09-19T00:00:00Z",
                     "packages": [
                       {
                         "id": "windows-any",
                         "version": "{{releaseVersion}}",
                         "os": "windows",
                         "architecture": "any",
                         "framework": "net10.0-windows",
                         "deployment": "frameworkDependent",
                         "format": "zip",
                         "url": "https://updates.example.test/packages/filemerger.zip",
                         "sizeBytes": 123456,
                         "sha256": "{{new string('a', 64)}}",
                         "entryExecutable": "FileMerger.Wpf.exe"
                       }
                     ]
                   }
                 }
                 """;
    }

    private static DownloadedUpdatePackage CreateDownloaded(UpdatePackage package)
    {
        return new DownloadedUpdatePackage(package, "attempt", "attempt/package.zip");
    }

    private static VerifiedUpdatePackage CreateVerified(DownloadedUpdatePackage downloaded)
    {
        return new VerifiedUpdatePackage(
            downloaded.Package,
            downloaded.AttemptDirectory,
            downloaded.ArchivePath,
            "attempt/payload",
            "attempt/payload/FileMerger.Wpf.exe");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("The expected view-model state was not observed.");

            await Task.Delay(10);
        }
    }

    private sealed record ViewModelContext(
        UpdateCheckDialogViewModel ViewModel,
        FakeUpdateInstallationCoordinator InstallationCoordinator);

    private sealed class FakeApplicationVersionProvider(string version) : IApplicationVersionProvider
    {
        public string GetCurrentVersion()
        {
            return version;
        }
    }

    private sealed class FakeUpdaterVersionProvider(string version) : IUpdaterVersionProvider
    {
        public string GetCurrentUpdaterVersion()
        {
            return version;
        }
    }

    private sealed class FakeReleaseManifestSource(string json) : IReleaseManifestSource
    {
        public string Json { get; set; } = json;
        public Exception? Exception { get; init; }
        public Func<CancellationToken, Task<string>>? Handler { get; init; }
        public int Calls { get; private set; }

        public Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;

            if (Exception is not null)
                throw Exception;

            return Handler?.Invoke(cancellationToken) ?? Task.FromResult(Json);
        }
    }

    private sealed class FakeUpdateInstallationCoordinator : IUpdateInstallationCoordinator
    {
        public UpdateInstallationCoordinationResult Result { get; init; } =
            UpdateInstallationCoordinationResult.ReadyToShutdown("ready");

        public int PrepareCalls { get; private set; }
        public int CommitCalls { get; private set; }
        public VerifiedUpdatePackage? LastPackage { get; private set; }

        public Task<UpdateInstallationCoordinationResult> PrepareAndLaunchAsync(
            VerifiedUpdatePackage verifiedPackage,
            IProgress<UpdatePackagePreparationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            PrepareCalls++;
            LastPackage = verifiedPackage;

            return Task.FromResult(Result);
        }

        public void CommitShutdown()
        {
            CommitCalls++;
        }
    }

    private sealed class FakeUpdatePackageDownloader : IUpdatePackageDownloader
    {
        public Exception? Exception { get; init; }

        public Func<UpdatePackage, IProgress<UpdatePackagePreparationProgress>?, CancellationToken,
            Task<DownloadedUpdatePackage>>? Handler { get; init; }

        public int Calls { get; private set; }

        public Task<DownloadedUpdatePackage> DownloadAsync(
            UpdatePackage package,
            IProgress<UpdatePackagePreparationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;

            if (Exception is not null)
                throw Exception;

            if (Handler is not null)
                return Handler(package, progress, cancellationToken);

            return Task.FromResult(CreateDownloaded(package));
        }
    }

    private sealed class FakeUpdatePackageValidator : IUpdatePackageValidator
    {
        public Exception? Exception { get; init; }

        public Func<DownloadedUpdatePackage, IProgress<UpdatePackagePreparationProgress>?, CancellationToken,
            Task<VerifiedUpdatePackage>>? Handler { get; init; }

        public Task<VerifiedUpdatePackage> ValidateAsync(
            DownloadedUpdatePackage downloadedPackage,
            IProgress<UpdatePackagePreparationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Exception is not null)
                throw Exception;

            if (Handler is not null)
                return Handler(downloadedPackage, progress, cancellationToken);

            return Task.FromResult(CreateVerified(downloadedPackage));
        }
    }
}