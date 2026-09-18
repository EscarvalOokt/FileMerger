using System.IO;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Updates;
using FileMerger.UpdateProtocol;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class UpdateInstallationPreflightServiceTests
{
    [Fact]
    public async Task ValidateAsync_Should_Accept_Writable_Installation_With_Updater_Runtime()
    {
        using TemporaryDirectory temp = new();
        TestContext context = CreateContext(temp.Path);

        await context.Service.ValidateAsync(context.VerifiedPackage);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Missing_Installation_Directory()
    {
        using TemporaryDirectory temp = new();
        TestContext context = CreateContext(temp.Path);
        Directory.Delete(context.InstallationDirectory, recursive: true);

        UpdateInstallationException ex = await Assert.ThrowsAsync<UpdateInstallationException>(() =>
            context.Service.ValidateAsync(context.VerifiedPackage));

        Assert.Equal(UpdateInstallationFailureCode.InstallationDirectoryUnavailable, ex.FailureCode);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Verified_Attempt_Outside_Updates_Root()
    {
        using TemporaryDirectory temp = new();
        TestContext context = CreateContext(temp.Path);
        string foreignAttempt = Path.Combine(temp.Path, "foreign-attempt");
        string foreignPayload = Path.Combine(foreignAttempt, "payload");
        Directory.CreateDirectory(foreignPayload);
        string foreignArchive = Path.Combine(foreignAttempt, "package.zip");
        await File.WriteAllTextAsync(foreignArchive, "archive");
        string foreignEntry = Path.Combine(foreignPayload, "FileMerger.Wpf.exe");
        await File.WriteAllTextAsync(foreignEntry, "exe");
        VerifiedUpdatePackage foreign = new(
            context.VerifiedPackage.Package,
            foreignAttempt,
            foreignArchive,
            foreignPayload,
            foreignEntry);

        UpdateInstallationException ex = await Assert.ThrowsAsync<UpdateInstallationException>(() =>
            context.Service.ValidateAsync(foreign));

        Assert.Equal(UpdateInstallationFailureCode.InvalidStaging, ex.FailureCode);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Missing_Updater_Runtime()
    {
        using TemporaryDirectory temp = new();
        TestContext context = CreateContext(temp.Path);
        File.Delete(Path.Combine(context.InstallationDirectory, UpdateProtocolConstants.UpdaterExecutableFileName));

        UpdateInstallationException ex = await Assert.ThrowsAsync<UpdateInstallationException>(() =>
            context.Service.ValidateAsync(context.VerifiedPackage));

        Assert.Equal(UpdateInstallationFailureCode.UpdaterRuntimeUnavailable, ex.FailureCode);
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Overlapping_Installation_And_Staging()
    {
        using TemporaryDirectory temp = new();
        UpdateStagingPathPolicy staging = new(temp.Path);
        string updatesRoot = staging.GetUpdatesRootDirectory();
        Directory.CreateDirectory(updatesRoot);
        string installation = Path.Combine(updatesRoot, "installed-app");
        Directory.CreateDirectory(installation);
        CreateUpdaterRuntime(installation);
        VerifiedUpdatePackage package = CreateVerifiedPackage(staging);
        UpdateInstallationPreflightService service = new(
            new ApplicationInstallationPathProvider(installation),
            staging);

        UpdateInstallationException ex = await Assert.ThrowsAsync<UpdateInstallationException>(() =>
            service.ValidateAsync(package));

        Assert.Equal(UpdateInstallationFailureCode.InvalidStaging, ex.FailureCode);
    }

    private static TestContext CreateContext(string root)
    {
        string installation = Path.Combine(root, "installed-app");
        Directory.CreateDirectory(installation);
        CreateUpdaterRuntime(installation);

        UpdateStagingPathPolicy staging = new(Path.Combine(root, "local-app-data"));
        VerifiedUpdatePackage package = CreateVerifiedPackage(staging);
        UpdateInstallationPreflightService service = new(
            new ApplicationInstallationPathProvider(installation),
            staging);

        return new TestContext(service, package, installation);
    }

    private static VerifiedUpdatePackage CreateVerifiedPackage(UpdateStagingPathPolicy staging)
    {
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

        return new VerifiedUpdatePackage(package, attempt, archive, payload, entry);
    }

    private static void CreateUpdaterRuntime(string installationDirectory)
    {
        foreach (string fileName in UpdateProtocolConstants.RequiredUpdaterRuntimeFiles)
            File.WriteAllText(Path.Combine(installationDirectory, fileName), fileName);
    }

    private sealed record TestContext(
        UpdateInstallationPreflightService Service,
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