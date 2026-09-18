using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Common;
using FileMerger.Infrastructure.Updates;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class ZipUpdatePackageValidatorTests
{
    private static readonly string[] _requiredFiles =
    [
        "FileMerger.Wpf.exe",
        "FileMerger.Wpf.dll",
        "FileMerger.Wpf.deps.json",
        "FileMerger.Wpf.runtimeconfig.json",
        "FileMerger.Application.dll",
        "FileMerger.Domain.dll",
        "FileMerger.Infrastructure.dll",
        "Escarval.Wpf.Windowing.dll",
        "Microsoft.Extensions.DependencyInjection.dll",
        "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
        "FileMerger.UpdateProtocol.dll",
        "FileMerger.Updater.exe",
        "FileMerger.Updater.dll",
        "FileMerger.Updater.deps.json",
        "FileMerger.Updater.runtimeconfig.json"
    ];

    public static TheoryData<string> RequiredFiles
    {
        get
        {
            TheoryData<string> data = new();
            foreach (string file in _requiredFiles)
                data.Add(file);

            return data;
        }
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Verified_Package_For_Valid_Archive()
    {
        using TemporaryDirectory temp = new();
        TestPackageContext context = CreatePackageContext(temp.Path);
        ZipUpdatePackageValidator validator = new(context.StagingPolicy);

        VerifiedUpdatePackage result = await validator.ValidateAsync(context.DownloadedPackage);

        Assert.True(File.Exists(result.ArchivePath));
        Assert.True(Directory.Exists(result.PayloadDirectory));
        Assert.True(File.Exists(result.EntryExecutablePath));
        Assert.True(PathUtility.IsPathInsideDirectory(result.EntryExecutablePath, result.PayloadDirectory));
        Assert.Equal(context.DownloadedPackage.Package, result.Package);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public async Task ValidateAsync_Should_Fail_And_Invalidate_Attempt_For_Size_Mismatch(long sizeAdjustment)
    {
        using TemporaryDirectory temp = new();
        TestPackageContext context = CreatePackageContext(temp.Path, sizeAdjustment: sizeAdjustment);
        ZipUpdatePackageValidator validator = new(context.StagingPolicy);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() =>
                validator.ValidateAsync(context.DownloadedPackage));

        Assert.Equal(UpdatePackagePreparationFailureCode.SizeMismatch, ex.FailureCode);
        Assert.False(Directory.Exists(context.DownloadedPackage.AttemptDirectory));
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_And_Invalidate_Attempt_For_Hash_Mismatch()
    {
        using TemporaryDirectory temp = new();
        TestPackageContext context = CreatePackageContext(temp.Path, sha256Override: new string('0', 64));
        ZipUpdatePackageValidator validator = new(context.StagingPolicy);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() =>
                validator.ValidateAsync(context.DownloadedPackage));

        Assert.Equal(UpdatePackagePreparationFailureCode.HashMismatch, ex.FailureCode);
        Assert.False(Directory.Exists(context.DownloadedPackage.AttemptDirectory));
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_When_Completed_Archive_Is_Missing()
    {
        using TemporaryDirectory temp = new();
        UpdateStagingPathPolicy staging = new(temp.Path);
        string attempt = staging.CreateAttemptDirectoryPath();
        Directory.CreateDirectory(attempt);
        string archivePath = staging.GetArchivePath(attempt);
        UpdatePackage package = CreatePackage(10, new string('a', 64));
        DownloadedUpdatePackage downloaded = new(package, attempt, archivePath);
        ZipUpdatePackageValidator validator = new(staging);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() => validator.ValidateAsync(downloaded));

        Assert.Equal(UpdatePackagePreparationFailureCode.DownloadIncomplete, ex.FailureCode);
        Assert.False(Directory.Exists(attempt));
    }

    [Fact]
    public async Task ValidateAsync_Should_Fail_For_Malformed_Zip()
    {
        using TemporaryDirectory temp = new();
        UpdateStagingPathPolicy staging = new(temp.Path);
        string attempt = staging.CreateAttemptDirectoryPath();
        Directory.CreateDirectory(attempt);
        string archivePath = staging.GetArchivePath(attempt);
        await File.WriteAllBytesAsync(archivePath, "not-a-zip"u8.ToArray());
        UpdatePackage package = CreatePackage(new FileInfo(archivePath).Length, ComputeSha256(archivePath));
        DownloadedUpdatePackage downloaded = new(package, attempt, archivePath);
        ZipUpdatePackageValidator validator = new(staging);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() => validator.ValidateAsync(downloaded));

        Assert.Equal(UpdatePackagePreparationFailureCode.InvalidArchive, ex.FailureCode);
        Assert.False(Directory.Exists(attempt));
    }

    [Theory]
    [InlineData("../evil.txt")]
    [InlineData("..\\evil.txt")]
    [InlineData("folder/../../evil.txt")]
    [InlineData("/absolute.txt")]
    [InlineData("C:/windows-rooted.txt")]
    [InlineData("folder//duplicate-separator.txt")]
    public async Task ValidateAsync_Should_Reject_Unsafe_Archive_Entry(string unsafeEntry)
    {
        using TemporaryDirectory temp = new();
        TestPackageContext context = CreatePackageContext(temp.Path, additionalEntry: unsafeEntry);
        ZipUpdatePackageValidator validator = new(context.StagingPolicy);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() =>
                validator.ValidateAsync(context.DownloadedPackage));

        Assert.Equal(UpdatePackagePreparationFailureCode.UnsafeArchiveEntry, ex.FailureCode);
        Assert.False(Directory.Exists(context.DownloadedPackage.AttemptDirectory));
    }

    [Theory]
    [MemberData(nameof(RequiredFiles))]
    public async Task ValidateAsync_Should_Reject_Missing_Required_Payload(string missingFile)
    {
        using TemporaryDirectory temp = new();
        TestPackageContext context = CreatePackageContext(temp.Path, omittedFile: missingFile);
        ZipUpdatePackageValidator validator = new(context.StagingPolicy);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() =>
                validator.ValidateAsync(context.DownloadedPackage));

        Assert.Equal(UpdatePackagePreparationFailureCode.RequiredPayloadMissing, ex.FailureCode);
        Assert.Contains(missingFile, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_Should_Clear_Stale_Payload_Before_Extraction()
    {
        using TemporaryDirectory temp = new();
        TestPackageContext context = CreatePackageContext(temp.Path);
        string payloadDirectory = context.StagingPolicy.GetPayloadDirectory(context.DownloadedPackage.AttemptDirectory);
        Directory.CreateDirectory(payloadDirectory);
        string stalePath = Path.Combine(payloadDirectory, "stale.txt");
        await File.WriteAllTextAsync(stalePath, "stale");
        ZipUpdatePackageValidator validator = new(context.StagingPolicy);

        VerifiedUpdatePackage result = await validator.ValidateAsync(context.DownloadedPackage);

        Assert.False(File.Exists(stalePath));
        Assert.True(File.Exists(result.EntryExecutablePath));
    }

    [Theory]
    [InlineData(UpdatePackagePreparationStage.ValidatingHash)]
    [InlineData(UpdatePackagePreparationStage.Extracting)]
    public async Task ValidateAsync_Should_Propagate_Cancellation_Without_Verified_Artifact(
        UpdatePackagePreparationStage stage)
    {
        using TemporaryDirectory temp = new();
        TestPackageContext context = CreatePackageContext(temp.Path);
        ZipUpdatePackageValidator validator = new(context.StagingPolicy);
        using CancellationTokenSource cancellation = new();
        CancelAtStageProgress progress = new(stage, cancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            validator.ValidateAsync(context.DownloadedPackage, progress, cancellation.Token));

        Assert.False(Directory.Exists(context.DownloadedPackage.AttemptDirectory));
    }

    [Fact]
    public async Task ValidateAsync_Should_Reject_Archive_Path_Outside_Staging_Layout()
    {
        using TemporaryDirectory temp = new();
        UpdateStagingPathPolicy staging = new(temp.Path);
        string attempt = staging.CreateAttemptDirectoryPath();
        Directory.CreateDirectory(attempt);
        string unexpectedArchivePath = Path.Combine(attempt, "unexpected.zip");
        await File.WriteAllBytesAsync(unexpectedArchivePath, "data"u8.ToArray());
        UpdatePackage package = CreatePackage(
            new FileInfo(unexpectedArchivePath).Length,
            ComputeSha256(unexpectedArchivePath));
        DownloadedUpdatePackage downloaded = new(package, attempt, unexpectedArchivePath);
        ZipUpdatePackageValidator validator = new(staging);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() => validator.ValidateAsync(downloaded));

        Assert.Equal(UpdatePackagePreparationFailureCode.StagingFailed, ex.FailureCode);
        Assert.True(Directory.Exists(attempt));
    }

    private static TestPackageContext CreatePackageContext(
        string localAppDataRoot,
        string? omittedFile = null,
        string? additionalEntry = null,
        long sizeAdjustment = 0,
        string? sha256Override = null)
    {
        UpdateStagingPathPolicy staging = new(localAppDataRoot);
        string attempt = staging.CreateAttemptDirectoryPath();
        Directory.CreateDirectory(attempt);
        string archivePath = staging.GetArchivePath(attempt);

        using (FileStream stream = new(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create))
        {
            foreach (string requiredFile in _requiredFiles)
            {
                if (string.Equals(requiredFile, omittedFile, StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteEntry(archive, requiredFile, $"content:{requiredFile}");
            }

            if (additionalEntry is not null)
                WriteEntry(archive, additionalEntry, "unsafe");
        }

        long size = new FileInfo(archivePath).Length + sizeAdjustment;
        string sha256 = sha256Override ?? ComputeSha256(archivePath);
        UpdatePackage package = CreatePackage(size, sha256);

        return new TestPackageContext(staging, new DownloadedUpdatePackage(package, attempt, archivePath));
    }

    private static UpdatePackage CreatePackage(long sizeBytes, string sha256)
    {
        return new UpdatePackage(
            "windows-any",
            SemanticVersion.Parse("1.2.4"),
            "windows",
            "any",
            "net10.0-windows",
            "frameworkDependent",
            "zip",
            new Uri("https://updates.example.test/packages/filemerger.zip"),
            sizeBytes,
            sha256,
            "FileMerger.Wpf.exe",
            minimumSourceVersion: null,
            minimumUpdaterVersion: null);
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
        using StreamWriter writer = new(entry.Open());
        writer.Write(content);
    }

    private static string ComputeSha256(string path)
    {
        byte[] hash = SHA256.HashData(File.ReadAllBytes(path));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record TestPackageContext(
        UpdateStagingPathPolicy StagingPolicy,
        DownloadedUpdatePackage DownloadedPackage);

    private sealed class CancelAtStageProgress(
        UpdatePackagePreparationStage stage,
        CancellationTokenSource cancellation) : IProgress<UpdatePackagePreparationProgress>
    {
        public void Report(UpdatePackagePreparationProgress value)
        {
            if (value.Stage != stage)
                return;

            // Immediate cancellation is intentional so the validator observes it at the stage boundary.
            // ReSharper disable once MethodHasAsyncOverload
            cancellation.Cancel();
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
            catch
            {
                // Test cleanup is best-effort.
            }
        }
    }
}