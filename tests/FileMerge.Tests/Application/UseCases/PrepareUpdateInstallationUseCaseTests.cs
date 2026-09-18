using System.IO;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.PrepareUpdateInstallation;

namespace FileMerger.Tests.Application.UseCases;

public sealed class PrepareUpdateInstallationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Revalidate_And_Run_Preflight()
    {
        VerifiedUpdatePackage original = CreateVerifiedPackage();
        VerifiedUpdatePackage fresh = CreateVerifiedPackage(payloadDirectory: "attempt/fresh-payload");
        FakeValidator validator = new() { Result = fresh };
        FakePreflightService preflight = new();
        PrepareUpdateInstallationUseCase useCase = CreateUseCase(validator, preflight);

        PrepareUpdateInstallationResult result = await useCase.ExecuteAsync(original);

        Assert.Equal(PrepareUpdateInstallationOutcome.Ready, result.Outcome);
        Assert.Same(fresh, result.VerifiedPackage);
        Assert.Equal(1, validator.Calls);
        Assert.Same(fresh, preflight.LastPackage);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Revalidation_Fails_Without_Running_Preflight()
    {
        FakeValidator validator = new()
        {
            Exception = new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.HashMismatch,
                "hash mismatch")
        };
        FakePreflightService preflight = new();
        PrepareUpdateInstallationUseCase useCase = CreateUseCase(validator, preflight);

        PrepareUpdateInstallationResult result = await useCase.ExecuteAsync(CreateVerifiedPackage());

        Assert.Equal(PrepareUpdateInstallationOutcome.Failed, result.Outcome);
        Assert.Equal(UpdateInstallationFailureCode.PackageNoLongerValid, result.FailureCode);
        Assert.Contains("hash mismatch", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, preflight.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Reject_Package_Requiring_Newer_Updater_Before_Revalidation()
    {
        VerifiedUpdatePackage package = CreateVerifiedPackage(minimumUpdaterVersion: "2.0.0");
        FakeValidator validator = new();
        FakePreflightService preflight = new();
        PrepareUpdateInstallationUseCase useCase = CreateUseCase(validator, preflight, updaterVersion: "1.0.0");

        PrepareUpdateInstallationResult result = await useCase.ExecuteAsync(package);

        Assert.Equal(PrepareUpdateInstallationOutcome.Failed, result.Outcome);
        Assert.Equal(UpdateInstallationFailureCode.IncompatiblePackage, result.FailureCode);
        Assert.Equal(0, validator.Calls);
        Assert.Equal(0, preflight.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Map_Preflight_Failure()
    {
        VerifiedUpdatePackage fresh = CreateVerifiedPackage(payloadDirectory: "attempt/fresh-payload");
        FakeValidator validator = new() { Result = fresh };
        FakePreflightService preflight = new()
        {
            Exception = new UpdateInstallationException(
                UpdateInstallationFailureCode.InstallationDirectoryNotWritable,
                "read-only")
        };
        PrepareUpdateInstallationUseCase useCase = CreateUseCase(validator, preflight);

        PrepareUpdateInstallationResult result = await useCase.ExecuteAsync(CreateVerifiedPackage());

        Assert.Equal(PrepareUpdateInstallationOutcome.Failed, result.Outcome);
        Assert.Equal(UpdateInstallationFailureCode.InstallationDirectoryNotWritable, result.FailureCode);
        Assert.Contains("read-only", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Propagate_Cancellation()
    {
        FakeValidator validator = new()
        {
            Handler = static (_, _, _) =>
                Task.FromCanceled<VerifiedUpdatePackage>(new CancellationToken(canceled: true))
        };
        PrepareUpdateInstallationUseCase useCase = CreateUseCase(validator, new FakePreflightService());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(CreateVerifiedPackage()));
    }

    private static PrepareUpdateInstallationUseCase CreateUseCase(
        FakeValidator validator,
        FakePreflightService preflight,
        string updaterVersion = "1.0.0")
    {
        return new PrepareUpdateInstallationUseCase(
            new FakeApplicationVersionProvider("1.2.3"),
            new FakeUpdaterVersionProvider(updaterVersion),
            new UpdateCompatibilityPolicy(),
            validator,
            preflight);
    }

    private static VerifiedUpdatePackage CreateVerifiedPackage(
        string payloadDirectory = "attempt/payload",
        string? minimumUpdaterVersion = null)
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
            minimumSourceVersion: null,
            minimumUpdaterVersion: minimumUpdaterVersion is null ? null : SemanticVersion.Parse(minimumUpdaterVersion));

        return new VerifiedUpdatePackage(
            package,
            "attempt",
            "attempt/package.zip",
            payloadDirectory,
            Path.Combine(payloadDirectory, "FileMerger.Wpf.exe"));
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
        public VerifiedUpdatePackage? Result { get; init; }
        public Exception? Exception { get; init; }

        public Func<DownloadedUpdatePackage, IProgress<UpdatePackagePreparationProgress>?, CancellationToken,
            Task<VerifiedUpdatePackage>>? Handler { get; init; }

        public int Calls { get; private set; }

        public Task<VerifiedUpdatePackage> ValidateAsync(
            DownloadedUpdatePackage downloadedPackage,
            IProgress<UpdatePackagePreparationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;

            if (Exception is not null)
                throw Exception;

            if (Handler is not null)
                return Handler(downloadedPackage, progress, cancellationToken);

            return Task.FromResult(Result ?? CreateVerifiedPackage());
        }
    }

    private sealed class FakePreflightService : IUpdateInstallationPreflightService
    {
        public Exception? Exception { get; init; }
        public int Calls { get; private set; }
        public VerifiedUpdatePackage? LastPackage { get; private set; }

        public Task ValidateAsync(VerifiedUpdatePackage verifiedPackage, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastPackage = verifiedPackage;

            if (Exception is not null)
                throw Exception;

            return Task.CompletedTask;
        }
    }
}