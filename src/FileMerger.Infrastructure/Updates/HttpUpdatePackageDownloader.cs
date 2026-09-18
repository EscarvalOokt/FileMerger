using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;

namespace FileMerger.Infrastructure.Updates;

public sealed class HttpUpdatePackageDownloader : IUpdatePackageDownloader
{
    private const int BufferSize = 81920;

    private readonly HttpClient _httpClient;
    private readonly UpdateHttpOriginPolicy _originPolicy;
    private readonly UpdateStagingPathPolicy _stagingPathPolicy;

    public HttpUpdatePackageDownloader(
        HttpClient httpClient,
        UpdateHttpOriginPolicy originPolicy,
        UpdateStagingPathPolicy stagingPathPolicy)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(originPolicy);
        ArgumentNullException.ThrowIfNull(stagingPathPolicy);

        _httpClient = httpClient;
        _originPolicy = originPolicy;
        _stagingPathPolicy = stagingPathPolicy;
    }

    public async Task<DownloadedUpdatePackage> DownloadAsync(
        UpdatePackage package,
        IProgress<UpdatePackagePreparationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _originPolicy.EnsureTrustedPackageUri(package.Url);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.UntrustedOrigin,
                $"Package '{package.Id}' does not use a trusted GitHub Release asset URL: {ex.Message}",
                ex);
        }

        string attemptDirectory = _stagingPathPolicy.CreateAttemptDirectoryPath();
        string partialArchivePath = _stagingPathPolicy.GetPartialArchivePath(attemptDirectory);
        string archivePath = _stagingPathPolicy.GetArchivePath(attemptDirectory);

        try
        {
            Directory.CreateDirectory(attemptDirectory);

            await DownloadCoreAsync(package, partialArchivePath, progress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            File.Move(partialArchivePath, archivePath);

            return new DownloadedUpdatePackage(package, attemptDirectory, archivePath);
        }
        catch (OperationCanceledException)
        {
            TryDeleteDirectory(attemptDirectory);
            throw;
        }
        catch (UpdatePackagePreparationException)
        {
            TryDeleteDirectory(attemptDirectory);
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDeleteDirectory(attemptDirectory);
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.StagingFailed,
                $"Failed to stage package '{package.Id}': {ex.Message}",
                ex);
        }
    }

    private async Task DownloadCoreAsync(
        UpdatePackage package,
        string partialArchivePath,
        IProgress<UpdatePackagePreparationProgress>? progress,
        CancellationToken cancellationToken)
    {
        Uri requestUri = package.Url;

        for (int redirectCount = 0; redirectCount <= UpdateHttpOriginPolicy.MaximumRedirectCount; redirectCount++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                throw new UpdatePackagePreparationException(
                    UpdatePackagePreparationFailureCode.DownloadUnavailable,
                    $"Failed to download package '{package.Id}': {ex.Message}",
                    ex);
            }

            using (response)
            {
                if (UpdateHttpOriginPolicy.IsRedirect(response.StatusCode))
                {
                    if (redirectCount == UpdateHttpOriginPolicy.MaximumRedirectCount)
                    {
                        throw new UpdatePackagePreparationException(
                            UpdatePackagePreparationFailureCode.DownloadUnavailable,
                            $"Package '{package.Id}' download exceeded the redirect limit.");
                    }

                    try
                    {
                        requestUri = _originPolicy.ResolveTrustedPackageRedirect(requestUri, response.Headers.Location);
                    }
                    catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
                    {
                        throw new UpdatePackagePreparationException(
                            UpdatePackagePreparationFailureCode.UntrustedOrigin,
                            $"Package '{package.Id}' redirect is not trusted: {ex.Message}",
                            ex);
                    }

                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new UpdatePackagePreparationException(
                        UpdatePackagePreparationFailureCode.DownloadUnavailable,
                        $"Package '{package.Id}' download returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
                }

                try
                {
                    await WriteResponseToPartialFileAsync(
                        response,
                        package,
                        partialArchivePath,
                        progress,
                        cancellationToken);
                    return;
                }
                catch (HttpRequestException ex)
                {
                    throw new UpdatePackagePreparationException(
                        UpdatePackagePreparationFailureCode.DownloadIncomplete,
                        $"Package '{package.Id}' download did not complete: {ex.Message}",
                        ex);
                }
            }
        }

        throw new UpdatePackagePreparationException(
            UpdatePackagePreparationFailureCode.DownloadUnavailable,
            $"Package '{package.Id}' download did not complete.");
    }

    private static async Task WriteResponseToPartialFileAsync(
        HttpResponseMessage response,
        UpdatePackage package,
        string partialArchivePath,
        IProgress<UpdatePackagePreparationProgress>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(
            new UpdatePackagePreparationProgress(
                UpdatePackagePreparationStage.Downloading,
                Current: 0,
                Total: package.SizeBytes,
                Message: "Downloading update package..."));

        Stream input;
        try
        {
            input = await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new UpdatePackagePreparationException(
                UpdatePackagePreparationFailureCode.DownloadIncomplete,
                $"Package '{package.Id}' response stream could not be opened: {ex.Message}",
                ex);
        }

        await using Stream inputStream = input;
        await using FileStream output = new(
            partialArchivePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        byte[] buffer = new byte[BufferSize];
        long received = 0;

        while (true)
        {
            int read;
            try
            {
                read = await inputStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                throw new UpdatePackagePreparationException(
                    UpdatePackagePreparationFailureCode.DownloadIncomplete,
                    $"Package '{package.Id}' download did not complete: {ex.Message}",
                    ex);
            }

            if (read == 0)
                break;

            try
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new UpdatePackagePreparationException(
                    UpdatePackagePreparationFailureCode.StagingFailed,
                    $"Failed to write package '{package.Id}' to staging: {ex.Message}",
                    ex);
            }

            received += read;

            progress?.Report(
                new UpdatePackagePreparationProgress(
                    UpdatePackagePreparationStage.Downloading,
                    received,
                    package.SizeBytes,
                    "Downloading update package..."));
        }

        await output.FlushAsync(cancellationToken);
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
            // Cleanup is best-effort. The failed/canceled attempt is never returned as a valid artifact.
        }
    }
}