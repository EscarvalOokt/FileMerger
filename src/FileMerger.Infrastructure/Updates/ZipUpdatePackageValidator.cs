using System.IO.Compression;
using System.Security.Cryptography;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Common;
using FileMerger.UpdateProtocol;

namespace FileMerger.Infrastructure.Updates;

public sealed class ZipUpdatePackageValidator : IUpdatePackageValidator
{
    private static readonly string[] _requiredDependencyFileNames =
    [
        "FileMerger.Application.dll",
        "FileMerger.Domain.dll",
        "FileMerger.Infrastructure.dll",
        "Escarval.Wpf.Windowing.dll",
        "Microsoft.Extensions.DependencyInjection.dll",
        "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
        UpdateProtocolConstants.ProtocolAssemblyFileName,
        UpdateProtocolConstants.UpdaterExecutableFileName,
        UpdateProtocolConstants.UpdaterAssemblyFileName,
        UpdateProtocolConstants.UpdaterDependenciesFileName,
        UpdateProtocolConstants.UpdaterRuntimeConfigFileName
    ];

    private readonly UpdateStagingPathPolicy _stagingPathPolicy;

    public ZipUpdatePackageValidator(UpdateStagingPathPolicy stagingPathPolicy)
    {
        ArgumentNullException.ThrowIfNull(stagingPathPolicy);
        _stagingPathPolicy = stagingPathPolicy;
    }

    public async Task<VerifiedUpdatePackage> ValidateAsync(
        DownloadedUpdatePackage downloadedPackage,
        IProgress<UpdatePackagePreparationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(downloadedPackage);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureOwnedStagingLayout(downloadedPackage);

        try
        {
            return await ValidateCoreAsync(downloadedPackage, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryDeleteDirectory(downloadedPackage.AttemptDirectory);
            throw;
        }
        catch (UpdatePackagePreparationException)
        {
            TryDeleteDirectory(downloadedPackage.AttemptDirectory);
            throw;
        }
        catch (InvalidDataException ex)
        {
            TryDeleteDirectory(downloadedPackage.AttemptDirectory);
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.InvalidArchive,
                $"Package '{downloadedPackage.Package.Id}' is not a valid ZIP archive: {ex.Message}",
                ex);
        }
        catch (Exception ex) when (ex is IOException
                                       or UnauthorizedAccessException
                                       or CryptographicException
                                       or ArgumentException
                                       or NotSupportedException)
        {
            TryDeleteDirectory(downloadedPackage.AttemptDirectory);
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.StagingFailed,
                $"Failed to validate package '{downloadedPackage.Package.Id}': {ex.Message}",
                ex);
        }
    }

    private async Task<VerifiedUpdatePackage> ValidateCoreAsync(
        DownloadedUpdatePackage downloadedPackage,
        IProgress<UpdatePackagePreparationProgress>? progress,
        CancellationToken cancellationToken)
    {
        UpdatePackage package = downloadedPackage.Package;
        string archivePath = downloadedPackage.ArchivePath;

        progress?.Report(
            new UpdatePackagePreparationProgress(
                UpdatePackagePreparationStage.ValidatingSize,
                Current: 0,
                Total: 0,
                Message: "Checking downloaded package size..."));
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(archivePath))
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.DownloadIncomplete,
                $"Downloaded package '{package.Id}' is missing from staging.");
        }

        long actualSize = new FileInfo(archivePath).Length;
        if (actualSize != package.SizeBytes)
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.SizeMismatch,
                $"Package '{package.Id}' size mismatch. Expected {package.SizeBytes} bytes but found {actualSize} bytes.");
        }

        progress?.Report(
            new UpdatePackagePreparationProgress(
                UpdatePackagePreparationStage.ValidatingHash,
                Current: 0,
                Total: 0,
                Message: "Verifying update package integrity..."));
        cancellationToken.ThrowIfCancellationRequested();

        string actualHash = await ComputeSha256Async(archivePath, cancellationToken);
        if (!string.Equals(actualHash, package.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.HashMismatch,
                $"Package '{package.Id}' SHA-256 digest does not match the release manifest.");
        }

        progress?.Report(
            new UpdatePackagePreparationProgress(
                UpdatePackagePreparationStage.InspectingArchive,
                Current: 0,
                Total: 0,
                Message: "Inspecting update package contents..."));
        cancellationToken.ThrowIfCancellationRequested();

        string payloadDirectory = _stagingPathPolicy.GetPayloadDirectory(downloadedPackage.AttemptDirectory);

        await using FileStream archiveStream = new(
            archivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using ZipArchive archive = new(archiveStream, ZipArchiveMode.Read, leaveOpen: false);

        List<ValidatedArchiveEntry> validatedEntries = InspectArchiveEntries(archive, payloadDirectory, package);
        EnsureRequiredPayload(validatedEntries, package);

        cancellationToken.ThrowIfCancellationRequested();

        if (Directory.Exists(payloadDirectory))
            Directory.Delete(payloadDirectory, recursive: true);
        Directory.CreateDirectory(payloadDirectory);

        progress?.Report(
            new UpdatePackagePreparationProgress(
                UpdatePackagePreparationStage.Extracting,
                Current: 0,
                Total: 0,
                Message: "Preparing verified update files..."));
        cancellationToken.ThrowIfCancellationRequested();

        foreach (ValidatedArchiveEntry validatedEntry in validatedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (validatedEntry.IsDirectory)
            {
                Directory.CreateDirectory(validatedEntry.DestinationPath);
                continue;
            }

            string? parentDirectory = Path.GetDirectoryName(validatedEntry.DestinationPath);
            if (!string.IsNullOrEmpty(parentDirectory))
                Directory.CreateDirectory(parentDirectory);

            await using Stream input = await validatedEntry.Entry.OpenAsync(cancellationToken);
            await using FileStream output = new(
                validatedEntry.DestinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await input.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        string entryExecutablePath = ResolveDestinationPath(payloadDirectory, package.EntryExecutable);
        if (!File.Exists(entryExecutablePath) ||
            !PathUtility.IsPathInsideDirectory(entryExecutablePath, payloadDirectory))
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.RequiredPayloadMissing,
                $"Package '{package.Id}' did not produce the declared entry executable after extraction.");
        }

        progress?.Report(
            new UpdatePackagePreparationProgress(
                UpdatePackagePreparationStage.Completed,
                Current: 1,
                Total: 1,
                Message: "Update package verified."));

        return new VerifiedUpdatePackage(
            package,
            downloadedPackage.AttemptDirectory,
            archivePath,
            payloadDirectory,
            entryExecutablePath);
    }

    private void EnsureOwnedStagingLayout(DownloadedUpdatePackage downloadedPackage)
    {
        string updatesRoot = _stagingPathPolicy.GetUpdatesRootDirectory();
        if (!PathUtility.IsPathInsideDirectory(downloadedPackage.AttemptDirectory, updatesRoot))
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.StagingFailed,
                "The downloaded update package is outside the FileMerger update staging root.");
        }

        string expectedArchivePath = _stagingPathPolicy.GetArchivePath(downloadedPackage.AttemptDirectory);
        if (!PathUtility.PathEquals(downloadedPackage.ArchivePath, expectedArchivePath))
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.StagingFailed,
                "The downloaded update archive path does not match the expected staging layout.");
        }
    }

    private static List<ValidatedArchiveEntry> InspectArchiveEntries(
        ZipArchive archive,
        string payloadDirectory,
        UpdatePackage package)
    {
        List<ValidatedArchiveEntry> result = [];
        HashSet<string> destinations = new(StringComparer.OrdinalIgnoreCase);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string relativePath = NormalizeArchiveEntryPath(entry, package);
            string destinationPath = ResolveDestinationPath(payloadDirectory, relativePath);

            if (!PathUtility.IsPathInsideDirectory(destinationPath, payloadDirectory))
            {
                throw new UpdatePackagePreparationException(
                    UpdatePackagePreparationFailureCode.UnsafeArchiveEntry,
                    $"Package '{package.Id}' contains an archive entry outside the extraction root: '{entry.FullName}'.");
            }

            if (!destinations.Add(destinationPath))
            {
                throw new UpdatePackagePreparationException(
                    UpdatePackagePreparationFailureCode.InvalidArchive,
                    $"Package '{package.Id}' contains duplicate archive path '{relativePath}'.");
            }

            result.Add(new ValidatedArchiveEntry(entry, relativePath, destinationPath, IsDirectoryEntry(entry)));
        }

        return result;
    }

    private static string NormalizeArchiveEntryPath(ZipArchiveEntry entry, UpdatePackage package)
    {
        string normalized = entry.FullName.Replace('\\', '/');
        bool isDirectory = normalized.EndsWith('/');
        if (isDirectory)
            normalized = normalized[..^1];

        if (normalized.Length == 0 || normalized.StartsWith('/') || normalized.Contains(':'))
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.UnsafeArchiveEntry,
                $"Package '{package.Id}' contains an unsafe archive entry '{entry.FullName}'.");
        }

        string[] segments = normalized.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.UnsafeArchiveEntry,
                $"Package '{package.Id}' contains an unsafe archive entry '{entry.FullName}'.");
        }

        return string.Join('/', segments);
    }

    private static void EnsureRequiredPayload(IReadOnlyCollection<ValidatedArchiveEntry> entries, UpdatePackage package)
    {
        HashSet<string> filePaths = new(
            entries.Where(entry => !entry.IsDirectory).Select(entry => entry.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        foreach (string requiredPath in GetRequiredPayloadPaths(package))
        {
            if (!filePaths.Contains(requiredPath))
            {
                throw new UpdatePackagePreparationException(
                    UpdatePackagePreparationFailureCode.RequiredPayloadMissing,
                    $"Package '{package.Id}' is missing required application file '{requiredPath}'.");
            }
        }
    }

    private static IReadOnlyCollection<string> GetRequiredPayloadPaths(UpdatePackage package)
    {
        string entryExecutable = package.EntryExecutable.Replace('\\', '/');
        int separatorIndex = entryExecutable.LastIndexOf('/');
        string directoryPrefix = separatorIndex >= 0 ? entryExecutable[..(separatorIndex + 1)] : string.Empty;
        string entryFileName = separatorIndex >= 0 ? entryExecutable[(separatorIndex + 1)..] : entryExecutable;
        string entryBaseName = Path.GetFileNameWithoutExtension(entryFileName);

        List<string> paths =
        [
            entryExecutable,
            directoryPrefix + entryBaseName + ".dll",
            directoryPrefix + entryBaseName + ".deps.json",
            directoryPrefix + entryBaseName + ".runtimeconfig.json"
        ];

        paths.AddRange(_requiredDependencyFileNames.Select(fileName => directoryPrefix + fileName));
        return paths;
    }

    private static string ResolveDestinationPath(string payloadDirectory, string relativePath)
    {
        string platformRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(payloadDirectory, platformRelativePath));
    }

    private static bool IsDirectoryEntry(ZipArchiveEntry entry)
    {
        return entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Validation has already failed or been canceled. Cleanup is best-effort and does not
            // turn the invalid attempt into a verified artifact.
        }
    }

    private sealed record ValidatedArchiveEntry(
        ZipArchiveEntry Entry,
        string RelativePath,
        string DestinationPath,
        bool IsDirectory);
}