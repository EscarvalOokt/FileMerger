using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using FileMerger.Infrastructure.Updates;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class HttpReleaseManifestSourceTests
{
    [Fact]
    public async Task GetManifestJsonAsync_Should_Return_Success_Response_Body()
    {
        RecordingHandler handler = new((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"schemaVersion\":1}")
            }));
        HttpReleaseManifestSource source = CreateSource(handler);

        string json = await source.GetManifestJsonAsync();

        Assert.Equal("{\"schemaVersion\":1}", json);
        Assert.Equal(new Uri("https://updates.example.test/manifest.json"), Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task GetManifestJsonAsync_Should_Fail_For_NonSuccess_Status()
    {
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        HttpReleaseManifestSource source = CreateSource(handler);

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetManifestJsonAsync());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
    }

    [Fact]
    public async Task GetManifestJsonAsync_Should_Propagate_Cancellation()
    {
        RecordingHandler handler = new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        HttpReleaseManifestSource source = CreateSource(handler);
        using CancellationTokenSource cancellation = new();
        // Synchronous cancellation is intentional: the test needs the token canceled before awaiting the operation.
        // ReSharper disable once MethodHasAsyncOverload
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetManifestJsonAsync(cancellation.Token));
    }

    [Fact]
    public async Task GetManifestJsonAsync_Should_Follow_SameOrigin_Https_Redirect()
    {
        int call = 0;
        RecordingHandler handler = new((_, _) =>
        {
            call++;
            if (call == 1)
            {
                HttpResponseMessage redirect = new(HttpStatusCode.Redirect);
                redirect.Headers.Location = new Uri("/manifest-v2.json", UriKind.Relative);
                return Task.FromResult(redirect);
            }

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("manifest-v2")
                });
        });
        HttpReleaseManifestSource source = CreateSource(handler);

        string result = await source.GetManifestJsonAsync();

        Assert.Equal("manifest-v2", result);
        Assert.Equal(
            new[]
            {
                new Uri("https://updates.example.test/manifest.json"),
                new Uri("https://updates.example.test/manifest-v2.json")
            },
            handler.RequestUris);
    }

    [Fact]
    public async Task GetManifestJsonAsync_Should_Reject_Http_Redirect()
    {
        RecordingHandler handler = new((_, _) =>
        {
            HttpResponseMessage redirect = new(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri("http://updates.example.test/manifest-v2.json");
            return Task.FromResult(redirect);
        });
        HttpReleaseManifestSource source = CreateSource(handler);

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetManifestJsonAsync());

        Assert.Contains("HTTPS", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetManifestJsonAsync_Should_Reject_CrossOrigin_Redirect()
    {
        RecordingHandler handler = new((_, _) =>
        {
            HttpResponseMessage redirect = new(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri("https://cdn.example.test/manifest.json");
            return Task.FromResult(redirect);
        });
        HttpReleaseManifestSource source = CreateSource(handler);

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetManifestJsonAsync());

        Assert.Contains("trusted origin", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetManifestJsonAsync_Should_Reject_ReleaseAssets_Redirect()
    {
        RecordingHandler handler = new((_, _) =>
        {
            HttpResponseMessage redirect = new(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri(
                "https://release-assets.githubusercontent.com/github-production-release-asset/123/manifest.json");
            return Task.FromResult(redirect);
        });
        HttpReleaseManifestSource source = CreateSource(handler);

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetManifestJsonAsync());

        Assert.Contains("trusted origin", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetManifestJsonAsync_Should_Fail_When_Redirect_Limit_Is_Exceeded()
    {
        RecordingHandler handler = new((_, _) =>
        {
            HttpResponseMessage redirect = new(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri("manifest-next.json", UriKind.Relative);
            return Task.FromResult(redirect);
        });
        HttpReleaseManifestSource source = CreateSource(handler);

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetManifestJsonAsync());

        Assert.Contains("redirect limit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetManifestJsonAsync_Should_Fail_When_Update_Source_Is_Not_Configured()
    {
        RecordingHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using HttpClient httpClient = new(handler);
        EntryAssemblyUpdateManifestUriProvider uriProvider = new(CreateAssemblyWithoutMetadata());
        HttpReleaseManifestSource source = new(httpClient, new UpdateHttpOriginPolicy(uriProvider));

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => source.GetManifestJsonAsync());

        Assert.Contains("not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(handler.RequestUris);
    }

    private static HttpReleaseManifestSource CreateSource(RecordingHandler handler)
    {
        HttpClient httpClient = new(handler);
        EntryAssemblyUpdateManifestUriProvider uriProvider = new(
            CreateAssemblyWithMetadata("https://updates.example.test/manifest.json"));

        return new HttpReleaseManifestSource(httpClient, new UpdateHttpOriginPolicy(uriProvider));
    }

    private static Assembly CreateAssemblyWithMetadata(string value)
    {
        AssemblyName name = new($"HttpManifestSource_{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        ConstructorInfo constructor =
            typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!;
        CustomAttributeBuilder attribute = new(
            constructor,
            [EntryAssemblyUpdateManifestUriProvider.MetadataKey, value]);
        assembly.SetCustomAttribute(attribute);

        return assembly;
    }

    private static Assembly CreateAssemblyWithoutMetadata()
    {
        AssemblyName name = new($"HttpManifestSourceNoMetadata_{Guid.NewGuid():N}");
        return AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
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
}