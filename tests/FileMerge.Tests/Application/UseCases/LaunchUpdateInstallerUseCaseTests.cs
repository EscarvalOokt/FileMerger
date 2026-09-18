using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.LaunchUpdateInstaller;

namespace FileMerger.Tests.Application.UseCases;

public sealed class LaunchUpdateInstallerUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Return_Started_When_Launcher_Succeeds()
    {
        FakeLauncher launcher = new();
        LaunchUpdateInstallerUseCase useCase = new(launcher);
        VerifiedUpdatePackage package = CreateVerifiedPackage();

        LaunchUpdateInstallerResult result = await useCase.ExecuteAsync(package);

        Assert.Equal(LaunchUpdateInstallerOutcome.Started, result.Outcome);
        Assert.Same(package, launcher.LastPackage);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Typed_Failure()
    {
        FakeLauncher launcher = new()
        {
            Exception = new UpdateInstallationException(
                UpdateInstallationFailureCode.HelperLaunchFailed,
                "could not start")
        };
        LaunchUpdateInstallerUseCase useCase = new(launcher);

        LaunchUpdateInstallerResult result = await useCase.ExecuteAsync(CreateVerifiedPackage());

        Assert.Equal(LaunchUpdateInstallerOutcome.Failed, result.Outcome);
        Assert.Equal(UpdateInstallationFailureCode.HelperLaunchFailed, result.FailureCode);
        Assert.Contains("could not start", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Propagate_Cancellation()
    {
        FakeLauncher launcher = new()
        {
            Handler = static (_, _) => Task.FromCanceled(new CancellationToken(canceled: true))
        };
        LaunchUpdateInstallerUseCase useCase = new(launcher);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(CreateVerifiedPackage()));
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

    private sealed class FakeLauncher : IUpdateInstallerLauncher
    {
        public Exception? Exception { get; init; }
        public Func<VerifiedUpdatePackage, CancellationToken, Task>? Handler { get; init; }
        public VerifiedUpdatePackage? LastPackage { get; private set; }

        public Task StartAsync(VerifiedUpdatePackage verifiedPackage, CancellationToken cancellationToken = default)
        {
            LastPackage = verifiedPackage;

            if (Exception is not null)
                throw Exception;

            return Handler?.Invoke(verifiedPackage, cancellationToken) ?? Task.CompletedTask;
        }
    }
}