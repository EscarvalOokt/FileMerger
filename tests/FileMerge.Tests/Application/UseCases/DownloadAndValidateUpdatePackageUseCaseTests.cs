using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.DownloadAndValidateUpdatePackage;

namespace FileMerger.Tests.Application.UseCases;

public sealed class DownloadAndValidateUpdatePackageUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Download_Validate_And_Return_Verified_Package()
    {
        UpdatePackage package = CreatePackage();
        DownloadedUpdatePackage downloaded = new(package, "attempt", "attempt/package.zip");
        VerifiedUpdatePackage verified = new(
            package,
            "attempt",
            "attempt/package.zip",
            "attempt/payload",
            "attempt/payload/FileMerger.Wpf.exe");
        FakeDownloader downloader = new() { Result = downloaded };
        FakeValidator validator = new() { Result = verified };
        DownloadAndValidateUpdatePackageUseCase useCase = CreateUseCase(downloader, validator);

        DownloadAndValidateUpdatePackageResult result = await useCase.ExecuteAsync(
            SemanticVersion.Parse("1.2.3"),
            SemanticVersion.Parse("1.2.4"),
            package);

        Assert.Equal(DownloadAndValidateUpdatePackageOutcome.Verified, result.Outcome);
        Assert.Same(verified, result.VerifiedPackage);
        Assert.Equal(1, downloader.Calls);
        Assert.Equal(1, validator.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_Before_Download_When_Package_Version_Differs_From_Release()
    {
        UpdatePackage package = CreatePackage("1.2.5");
        FakeDownloader downloader = new();
        FakeValidator validator = new();
        DownloadAndValidateUpdatePackageUseCase useCase = CreateUseCase(downloader, validator);

        DownloadAndValidateUpdatePackageResult result = await useCase.ExecuteAsync(
            SemanticVersion.Parse("1.2.3"),
            SemanticVersion.Parse("1.2.4"),
            package);

        Assert.Equal(DownloadAndValidateUpdatePackageOutcome.Failed, result.Outcome);
        Assert.Equal(UpdatePackagePreparationFailureCode.CandidateInvalid, result.FailureCode);
        Assert.Equal(0, downloader.Calls);
        Assert.Equal(0, validator.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_Before_Download_When_Candidate_Is_Incompatible()
    {
        UpdatePackage package = CreatePackage(os: "linux");
        FakeDownloader downloader = new();
        FakeValidator validator = new();
        DownloadAndValidateUpdatePackageUseCase useCase = CreateUseCase(downloader, validator);

        DownloadAndValidateUpdatePackageResult result = await useCase.ExecuteAsync(
            SemanticVersion.Parse("1.2.3"),
            SemanticVersion.Parse("1.2.4"),
            package);

        Assert.Equal(UpdatePackagePreparationFailureCode.CandidateInvalid, result.FailureCode);
        Assert.Equal(0, downloader.Calls);
        Assert.Equal(0, validator.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Not_Invoke_Validator_When_Download_Fails()
    {
        UpdatePackage package = CreatePackage();
        FakeDownloader downloader = new()
        {
            Exception = new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.DownloadUnavailable,
                "offline")
        };
        FakeValidator validator = new();
        DownloadAndValidateUpdatePackageUseCase useCase = CreateUseCase(downloader, validator);

        DownloadAndValidateUpdatePackageResult result = await useCase.ExecuteAsync(
            SemanticVersion.Parse("1.2.3"),
            SemanticVersion.Parse("1.2.4"),
            package);

        Assert.Equal(DownloadAndValidateUpdatePackageOutcome.Failed, result.Outcome);
        Assert.Equal(UpdatePackagePreparationFailureCode.DownloadUnavailable, result.FailureCode);
        Assert.Contains("offline", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, validator.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Failed_When_Validation_Fails()
    {
        UpdatePackage package = CreatePackage();
        FakeDownloader downloader = new()
        {
            Result = new DownloadedUpdatePackage(package, "attempt", "attempt/package.zip")
        };
        FakeValidator validator = new()
        {
            Exception = new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.HashMismatch,
                "hash mismatch")
        };
        DownloadAndValidateUpdatePackageUseCase useCase = CreateUseCase(downloader, validator);

        DownloadAndValidateUpdatePackageResult result = await useCase.ExecuteAsync(
            SemanticVersion.Parse("1.2.3"),
            SemanticVersion.Parse("1.2.4"),
            package);

        Assert.Equal(DownloadAndValidateUpdatePackageOutcome.Failed, result.Outcome);
        Assert.Equal(UpdatePackagePreparationFailureCode.HashMismatch, result.FailureCode);
        Assert.Null(result.VerifiedPackage);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Propagate_Cancellation()
    {
        UpdatePackage package = CreatePackage();
        FakeDownloader downloader = new()
        {
            Handler = static (_, _, _) =>
                Task.FromCanceled<DownloadedUpdatePackage>(new CancellationToken(canceled: true))
        };
        FakeValidator validator = new();
        DownloadAndValidateUpdatePackageUseCase useCase = CreateUseCase(downloader, validator);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(
            SemanticVersion.Parse("1.2.3"),
            SemanticVersion.Parse("1.2.4"),
            package));
    }

    private static DownloadAndValidateUpdatePackageUseCase CreateUseCase(
        FakeDownloader downloader,
        FakeValidator validator)
    {
        return new DownloadAndValidateUpdatePackageUseCase(
            new UpdateCompatibilityPolicy(),
            new FakeUpdaterVersionProvider("1.0.0"),
            downloader,
            validator);
    }

    private static UpdatePackage CreatePackage(string version = "1.2.4", string os = "windows")
    {
        return new UpdatePackage(
            "windows-any",
            SemanticVersion.Parse(version),
            os,
            "any",
            "net10.0-windows",
            "frameworkDependent",
            "zip",
            new Uri("https://updates.example.test/packages/filemerger.zip"),
            123,
            new string('a', 64),
            "FileMerger.Wpf.exe",
            minimumSourceVersion: null,
            minimumUpdaterVersion: null);
    }

    private sealed class FakeUpdaterVersionProvider(string version) : IUpdaterVersionProvider
    {
        public string Version { get; set; } = version;

        public string GetCurrentUpdaterVersion()
        {
            return Version;
        }
    }

    private sealed class FakeDownloader : IUpdatePackageDownloader
    {
        public DownloadedUpdatePackage? Result { get; init; }
        public Exception? Exception { get; init; }

        public Func<UpdatePackage, IProgress<UpdatePackagePreparationProgress>?, CancellationToken,
            Task<DownloadedUpdatePackage>>? Handler { get; init; }

        public int Calls { get; private set; }

        public Task<DownloadedUpdatePackage> DownloadAsync(
            UpdatePackage package,
            IProgress<UpdatePackagePreparationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;

            if (Exception is not null)
                throw Exception;

            if (Handler is not null)
                return Handler(package, progress, cancellationToken);

            return Task.FromResult(
                Result ?? throw new InvalidOperationException("No fake download result configured."));
        }
    }

    private sealed class FakeValidator : IUpdatePackageValidator
    {
        public VerifiedUpdatePackage? Result { get; init; }
        public Exception? Exception { get; init; }
        public int Calls { get; private set; }

        public Task<VerifiedUpdatePackage> ValidateAsync(
            DownloadedUpdatePackage downloadedPackage,
            IProgress<UpdatePackagePreparationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;

            if (Exception is not null)
                throw Exception;

            return Task.FromResult(
                Result ?? throw new InvalidOperationException("No fake validation result configured."));
        }
    }
}