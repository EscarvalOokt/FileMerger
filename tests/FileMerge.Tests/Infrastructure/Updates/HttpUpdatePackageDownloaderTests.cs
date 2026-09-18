using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using FileMerger.Application.Updates;
using FileMerger.Infrastructure.Updates;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class HttpUpdatePackageDownloaderTests
{
    private const string TrustedPackageUrl =
        "https://github.com/EscarvalOokt/FileMerger/releases/download/v1.2.4/FileMerger-1.2.4.zip";

    [Fact]
    public async Task DownloadAsync_Should_Write_Completed_Archive_And_Report_Progress()
    {
        using TemporaryDirectory temp = new();
        byte[] content = "package-bytes"u8.ToArray();
        RecordingHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            }));
        UpdateStagingPathPolicy staging = new(temp.Path);
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, staging);
        RecordingProgress progress = new();

        DownloadedUpdatePackage result = await downloader.DownloadAsync(CreatePackage(content.Length), progress);

        Assert.True(File.Exists(result.ArchivePath));
        Assert.Equal(content, await File.ReadAllBytesAsync(result.ArchivePath));
        Assert.False(File.Exists(staging.GetPartialArchivePath(result.AttemptDirectory)));
        Assert.NotEmpty(progress.Values);
        Assert.Equal(0, progress.Values[0].Current);
        Assert.Equal(content.Length, progress.Values[^1].Current);
        Assert.All(progress.Values, value => Assert.Equal(UpdatePackagePreparationStage.Downloading, value.Stage));
    }

    [Fact]
    public async Task DownloadAsync_Should_Keep_Only_Partial_Artifact_While_Transfer_Is_Incomplete()
    {
        using TemporaryDirectory temp = new();
        byte[] content = "package-bytes"u8.ToArray();
        BlockingReadStream stream = new(content);
        RecordingHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            }));
        UpdateStagingPathPolicy staging = new(temp.Path);
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, staging);

        Task<DownloadedUpdatePackage> downloadTask = downloader.DownloadAsync(CreatePackage(content.Length));
        await stream.FirstReadStarted;

        string attemptDirectory = Assert.Single(Directory.GetDirectories(staging.GetUpdatesRootDirectory()));
        Assert.True(File.Exists(staging.GetPartialArchivePath(attemptDirectory)));
        Assert.False(File.Exists(staging.GetArchivePath(attemptDirectory)));

        stream.Release();
        DownloadedUpdatePackage result = await downloadTask;

        Assert.True(File.Exists(result.ArchivePath));
        Assert.False(File.Exists(staging.GetPartialArchivePath(result.AttemptDirectory)));
    }

    [Fact]
    public async Task DownloadAsync_Should_Delete_Partial_Attempt_When_Canceled()
    {
        using TemporaryDirectory temp = new();
        byte[] content = "package-bytes"u8.ToArray();
        BlockingReadStream stream = new(content);
        RecordingHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            }));
        UpdateStagingPathPolicy staging = new(temp.Path);
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, staging);
        using CancellationTokenSource cancellation = new();

        Task<DownloadedUpdatePackage> downloadTask = downloader.DownloadAsync(
            CreatePackage(content.Length),
            cancellationToken: cancellation.Token);
        await stream.FirstReadStarted;

        // Synchronous cancellation is intentional: the test verifies immediate transfer cancellation and cleanup.
        // ReSharper disable once MethodHasAsyncOverload
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => downloadTask);
        Assert.Empty(Directory.GetDirectories(staging.GetUpdatesRootDirectory()));
    }

    [Fact]
    public async Task DownloadAsync_Should_Fail_And_Cleanup_For_NonSuccess_Status()
    {
        using TemporaryDirectory temp = new();
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        UpdateStagingPathPolicy staging = new(temp.Path);
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, staging);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() =>
                downloader.DownloadAsync(CreatePackage(10)));

        Assert.Equal(UpdatePackagePreparationFailureCode.DownloadUnavailable, ex.FailureCode);
        Assert.Empty(Directory.GetDirectories(staging.GetUpdatesRootDirectory()));
    }

    [Fact]
    public async Task DownloadAsync_Should_Fail_As_Incomplete_When_Response_Stream_Fails()
    {
        using TemporaryDirectory temp = new();
        ThrowingReadStream stream = new();
        RecordingHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            }));
        UpdateStagingPathPolicy staging = new(temp.Path);
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, staging);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() =>
                downloader.DownloadAsync(CreatePackage(10)));

        Assert.Equal(UpdatePackagePreparationFailureCode.DownloadIncomplete, ex.FailureCode);
        Assert.Empty(Directory.GetDirectories(staging.GetUpdatesRootDirectory()));
    }

    [Fact]
    public async Task DownloadAsync_Should_Follow_Trusted_GitHub_ReleaseAssets_Redirect()
    {
        using TemporaryDirectory temp = new();
        byte[] content = "package"u8.ToArray();
        Uri deliveryUri = new(
            "https://release-assets.githubusercontent.com/github-production-release-asset/123/filemerger.zip" +
            "?sp=r&sv=2026-01-01&sig=abc%2Fdef");
        int call = 0;
        RecordingHandler handler = new((_, _) =>
        {
            call++;
            if (call == 1)
            {
                HttpResponseMessage redirect = new(HttpStatusCode.Redirect);
                redirect.Headers.Location = deliveryUri;
                return Task.FromResult(redirect);
            }

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(content)
                });
        });
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, new UpdateStagingPathPolicy(temp.Path));

        await downloader.DownloadAsync(CreatePackage(content.Length));

        Assert.Equal(
            new[]
            {
                new Uri(TrustedPackageUrl),
                deliveryUri
            },
            handler.RequestUris);
    }

    [Theory]
    [InlineData("http://release-assets.githubusercontent.com/github-production-release-asset/123/filemerger.zip")]
    [InlineData("https://cdn.example.test/packages/filemerger.zip")]
    [InlineData("https://github.com/EscarvalOokt/FileMerger/releases/download/v1.2.4/other.zip")]
    public async Task DownloadAsync_Should_Reject_Untrusted_Redirect(string redirectUrl)
    {
        using TemporaryDirectory temp = new();
        RecordingHandler handler = new((_, _) =>
        {
            HttpResponseMessage redirect = new(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri(redirectUrl);
            return Task.FromResult(redirect);
        });
        UpdateStagingPathPolicy staging = new(temp.Path);
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, staging);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() =>
                downloader.DownloadAsync(CreatePackage(10)));

        Assert.Equal(UpdatePackagePreparationFailureCode.UntrustedOrigin, ex.FailureCode);
        Assert.Empty(Directory.GetDirectories(staging.GetUpdatesRootDirectory()));
    }

    [Theory]
    [InlineData("http://github.com/EscarvalOokt/FileMerger/releases/download/v1.2.4/FileMerger.zip")]
    [InlineData("https://github.com/OtherOwner/FileMerger/releases/download/v1.2.4/FileMerger.zip")]
    [InlineData("https://github.com/EscarvalOokt/OtherRepository/releases/download/v1.2.4/FileMerger.zip")]
    [InlineData("https://github.com/EscarvalOokt/FileMerger/releases/latest/download/FileMerger.zip")]
    [InlineData("https://release-assets.githubusercontent.com/github-production-release-asset/123/FileMerger.zip")]
    public async Task DownloadAsync_Should_Reject_Untrusted_Initial_Package_Uri_Before_Request(string packageUrl)
    {
        using TemporaryDirectory temp = new();
        RecordingHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        UpdateStagingPathPolicy staging = new(temp.Path);
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, staging);
        UpdatePackage package = CreatePackage(10, packageUrl);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() => downloader.DownloadAsync(package));

        Assert.Equal(UpdatePackagePreparationFailureCode.UntrustedOrigin, ex.FailureCode);
        Assert.Empty(handler.RequestUris);
    }

    [Fact]
    public async Task DownloadAsync_Should_Enforce_Redirect_Limit()
    {
        using TemporaryDirectory temp = new();
        RecordingHandler handler = new((request, _) =>
        {
            HttpResponseMessage redirect = new(HttpStatusCode.Redirect);
            redirect.Headers.Location =
                string.Equals(request.RequestUri!.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                    ? new Uri(
                        "https://release-assets.githubusercontent.com/github-production-release-asset/123/filemerger.zip")
                    : new Uri("next.zip", UriKind.Relative);
            return Task.FromResult(redirect);
        });
        UpdateStagingPathPolicy staging = new(temp.Path);
        HttpUpdatePackageDownloader downloader = CreateDownloader(handler, staging);

        UpdatePackagePreparationException ex =
            await Assert.ThrowsAsync<UpdatePackagePreparationException>(() =>
                downloader.DownloadAsync(CreatePackage(10)));

        Assert.Equal(UpdatePackagePreparationFailureCode.DownloadUnavailable, ex.FailureCode);
        Assert.Contains("redirect limit", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.GetDirectories(staging.GetUpdatesRootDirectory()));
    }

    private static HttpUpdatePackageDownloader CreateDownloader(
        RecordingHandler handler,
        UpdateStagingPathPolicy stagingPathPolicy)
    {
        HttpClient httpClient = new(handler);
        EntryAssemblyUpdateManifestUriProvider uriProvider = new(
            CreateAssemblyWithMetadata("https://updates.example.test/manifest.json"));
        UpdateHttpOriginPolicy originPolicy = new(uriProvider);

        return new HttpUpdatePackageDownloader(httpClient, originPolicy, stagingPathPolicy);
    }

    private static UpdatePackage CreatePackage(long sizeBytes, string? url = null)
    {
        return new UpdatePackage(
            "windows-any",
            SemanticVersion.Parse("1.2.4"),
            "windows",
            "any",
            "net10.0-windows",
            "frameworkDependent",
            "zip",
            new Uri(url ?? TrustedPackageUrl),
            sizeBytes,
            new string('a', 64),
            "FileMerger.Wpf.exe",
            minimumSourceVersion: null,
            minimumUpdaterVersion: null);
    }

    private static Assembly CreateAssemblyWithMetadata(string value)
    {
        AssemblyName name = new($"HttpPackageDownloader_{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        ConstructorInfo constructor =
            typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!;
        CustomAttributeBuilder attribute = new(
            constructor,
            [EntryAssemblyUpdateManifestUriProvider.MetadataKey, value]);
        assembly.SetCustomAttribute(attribute);

        return assembly;
    }

    private sealed class RecordingProgress : IProgress<UpdatePackagePreparationProgress>
    {
        public List<UpdatePackagePreparationProgress> Values { get; } = [];

        public void Report(UpdatePackagePreparationProgress value)
        {
            Values.Add(value);
        }
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        public List<Uri> RequestUris { get; } = [];

#pragma warning disable CS0628 // HttpMessageHandler requires this protected override even in a sealed test handler.
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);
            return handler(request, cancellationToken);
        }
#pragma warning restore CS0628
    }

    private sealed class BlockingReadStream(byte[] data) : Stream
    {
        private readonly TaskCompletionSource<bool> _firstReadStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _position;

        public Task FirstReadStarted => _firstReadStarted.Task;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => data.Length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public void Release()
        {
            _release.TrySetResult(true);
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_position >= data.Length)
                return 0;

            _firstReadStarted.TrySetResult(true);
            await _release.Task.WaitAsync(cancellationToken);

            int count = Math.Min(buffer.Length, data.Length - _position);
            data.AsMemory(_position, count).CopyTo(buffer);
            _position += count;
            return count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= data.Length)
                return 0;

            int read = Math.Min(count, data.Length - _position);
            Array.Copy(data, _position, buffer, offset, read);
            _position += read;
            return read;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class ThrowingReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 10;

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromException<int>(new HttpRequestException("connection interrupted"));
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new HttpRequestException("connection interrupted");
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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