using System.Net.Http;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.CheckForUpdates;

namespace FileMerger.Tests.Application.UseCases;

public sealed class CheckForUpdatesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Return_UpdateAvailable_For_Newer_Compatible_Stable_Release()
    {
        TestContext context = CreateContext("1.2.3", CreateManifest("1.2.4"));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.UpdateAvailable, result.Outcome);
        Assert.Equal("1.2.3", result.CurrentVersion?.ToString());
        Assert.Equal("1.2.4", result.ReleaseVersion?.ToString());
        Assert.Equal("windows-any", result.SelectedPackage?.Id);
        Assert.Equal(UpdateCheckFailureCode.None, result.FailureCode);
    }

    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.4", "1.2.3")]
    public async Task ExecuteAsync_Should_Return_NoUpdate_When_Release_Is_Not_Newer(
        string currentVersion,
        string releaseVersion)
    {
        TestContext context = CreateContext(currentVersion, CreateManifest(releaseVersion));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.NoUpdateAvailable, result.Outcome);
        Assert.Equal(releaseVersion, result.ReleaseVersion?.ToString());
        Assert.Null(result.SelectedPackage);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Not_Offer_Prerelease_To_Stable_Current_Build()
    {
        TestContext context = CreateContext("1.2.3", CreateManifest("1.3.0-alpha.1"));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.NoUpdateAvailable, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Offer_Newer_Prerelease_To_Prerelease_Current_Build()
    {
        TestContext context = CreateContext("1.3.0-alpha.1", CreateManifest("1.3.0-alpha.2"));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.UpdateAvailable, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Offer_Stable_Release_To_Prerelease_Current_Build()
    {
        TestContext context = CreateContext("1.3.0-alpha.2", CreateManifest("1.3.0"));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.UpdateAvailable, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_NoUpdate_When_No_Compatible_Package_Exists()
    {
        TestContext context = CreateContext("1.2.3", CreateManifest("1.2.4", os: "linux"));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.NoUpdateAvailable, result.Outcome);
        Assert.Null(result.SelectedPackage);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_NoUpdate_When_MinimumSourceVersion_Is_Higher()
    {
        TestContext context = CreateContext("1.2.3", CreateManifest("1.2.4", minimumSourceVersion: "1.2.4"));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.NoUpdateAvailable, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Offer_Package_When_MinimumUpdaterVersion_Is_Supported()
    {
        TestContext context = CreateContext(
            "1.2.3",
            CreateManifest("1.2.4", minimumUpdaterVersion: "1.0.0"),
            updaterVersion: "1.0.0");

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.UpdateAvailable, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_NoUpdate_When_MinimumUpdaterVersion_Is_Higher()
    {
        TestContext context = CreateContext(
            "1.2.3",
            CreateManifest("1.2.4", minimumUpdaterVersion: "1.0.1"),
            updaterVersion: "1.0.0");

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.NoUpdateAvailable, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_More_Than_One_Compatible_Package_Exists()
    {
        string firstPackage = CreatePackageJson("windows-any-1", "1.2.4");
        string secondPackage = CreatePackageJson("windows-any-2", "1.2.4");
        TestContext context = CreateContext("1.2.3", CreateManifestWithPackages("1.2.4", firstPackage, secondPackage));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.Failed, result.Outcome);
        Assert.Equal(UpdateCheckFailureCode.AmbiguousCompatiblePackage, result.FailureCode);
        Assert.Contains("more than one compatible", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Manifest_Source_Throws()
    {
        TestContext context = CreateContext("1.2.3", CreateManifest("1.2.4"));
        context.ManifestSource.Exception = new HttpRequestException("offline");

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.Failed, result.Outcome);
        Assert.Equal(UpdateCheckFailureCode.ReleaseSourceUnavailable, result.FailureCode);
        Assert.Equal("1.2.3", result.CurrentVersion?.ToString());
        Assert.Contains("offline", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Manifest_Is_Invalid()
    {
        TestContext context = CreateContext("1.2.3", "{ invalid-json");

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.Failed, result.Outcome);
        Assert.Equal(UpdateCheckFailureCode.ManifestInvalid, result.FailureCode);
        Assert.NotEqual(UpdateCheckOutcome.NoUpdateAvailable, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_Without_Reading_Source_When_Current_Version_Is_Invalid()
    {
        TestContext context = CreateContext("not-semver", CreateManifest("1.2.4"));

        CheckForUpdatesResult result = await context.UseCase.ExecuteAsync();

        Assert.Equal(UpdateCheckOutcome.Failed, result.Outcome);
        Assert.Equal(UpdateCheckFailureCode.CurrentVersionInvalid, result.FailureCode);
        Assert.Equal(0, context.ManifestSource.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Propagate_Cancellation()
    {
        TestContext context = CreateContext("1.2.3", CreateManifest("1.2.4"));
        using CancellationTokenSource cancellation = new();
        // Synchronous cancellation is intentional: the test needs the token canceled before awaiting the operation.
        // ReSharper disable once MethodHasAsyncOverload
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.UseCase.ExecuteAsync(cancellation.Token));
    }

    private static TestContext CreateContext(
        string currentVersion,
        string manifestJson,
        string updaterVersion = "1.0.0")
    {
        FakeApplicationVersionProvider versionProvider = new(currentVersion);
        FakeUpdaterVersionProvider updaterVersionProvider = new(updaterVersion);
        FakeReleaseManifestSource manifestSource = new(manifestJson);
        CheckForUpdatesUseCase useCase = new(
            versionProvider,
            updaterVersionProvider,
            manifestSource,
            new UpdateReleaseManifestParser(),
            new UpdateCompatibilityPolicy());

        return new TestContext(useCase, manifestSource);
    }

    private static string CreateManifest(
        string releaseVersion,
        string os = "windows",
        string architecture = "any",
        string framework = "net10.0-windows",
        string deployment = "frameworkDependent",
        string format = "zip",
        string? minimumSourceVersion = null,
        string? minimumUpdaterVersion = null)
    {
        string package = CreatePackageJson(
            "windows-any",
            releaseVersion,
            os,
            architecture,
            framework,
            deployment,
            format,
            minimumSourceVersion,
            minimumUpdaterVersion);

        return CreateManifestWithPackages(releaseVersion, package);
    }

    private static string CreateManifestWithPackages(string releaseVersion, params string[] packages)
    {
        string packageJson = string.Join(",\n", packages);

        return $$"""
                 {
                   "schemaVersion": 1,
                   "product": "FileMerger",
                   "release": {
                     "version": "{{releaseVersion}}",
                     "publishedAtUtc": "2026-09-19T00:00:00Z",
                     "releaseNotesUrl": "https://updates.example.test/releases/{{releaseVersion}}",
                     "packages": [
                 {{packageJson}}
                     ]
                   }
                 }
                 """;
    }

    private static string CreatePackageJson(
        string id,
        string version,
        string os = "windows",
        string architecture = "any",
        string framework = "net10.0-windows",
        string deployment = "frameworkDependent",
        string format = "zip",
        string? minimumSourceVersion = null,
        string? minimumUpdaterVersion = null)
    {
        string minimumSourceLine = minimumSourceVersion is null
            ? string.Empty
            : $",\n        \"minimumSourceVersion\": \"{minimumSourceVersion}\"";
        string minimumUpdaterLine = minimumUpdaterVersion is null
            ? string.Empty
            : $",\n        \"minimumUpdaterVersion\": \"{minimumUpdaterVersion}\"";

        return $$"""
                       {
                         "id": "{{id}}",
                         "version": "{{version}}",
                         "os": "{{os}}",
                         "architecture": "{{architecture}}",
                         "framework": "{{framework}}",
                         "deployment": "{{deployment}}",
                         "format": "{{format}}",
                         "url": "https://updates.example.test/packages/{{id}}.zip",
                         "sizeBytes": 123456,
                         "sha256": "{{new string('a', 64)}}",
                         "entryExecutable": "FileMerger.Wpf.exe"{{minimumSourceLine}}{{minimumUpdaterLine}}
                       }
                 """;
    }

    private sealed record TestContext(CheckForUpdatesUseCase UseCase, FakeReleaseManifestSource ManifestSource);

    private sealed class FakeApplicationVersionProvider(string version) : IApplicationVersionProvider
    {
        public string Version { get; set; } = version;

        public string GetCurrentVersion()
        {
            return Version;
        }
    }

    private sealed class FakeUpdaterVersionProvider(string version) : IUpdaterVersionProvider
    {
        public string Version { get; set; } = version;

        public string GetCurrentUpdaterVersion()
        {
            return Version;
        }
    }

    private sealed class FakeReleaseManifestSource(string json) : IReleaseManifestSource
    {
        public string Json { get; set; } = json;
        public Exception? Exception { get; set; }
        public int Calls { get; private set; }

        public Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;

            if (Exception is not null)
                throw Exception;

            return Task.FromResult(Json);
        }
    }
}