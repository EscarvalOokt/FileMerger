using FileMerger.Application.Abstractions.Services;

namespace FileMerger.Infrastructure.Updates;

public sealed class HttpReleaseManifestSource : IReleaseManifestSource
{
    private readonly HttpClient _httpClient;
    private readonly UpdateHttpOriginPolicy _originPolicy;

    public HttpReleaseManifestSource(HttpClient httpClient, UpdateHttpOriginPolicy originPolicy)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(originPolicy);

        _httpClient = httpClient;
        _originPolicy = originPolicy;
    }

    public async Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default)
    {
        Uri requestUri = _originPolicy.GetTrustedManifestUri();
        _originPolicy.EnsureTrustedManifestUri(requestUri);

        for (int redirectCount = 0; redirectCount <= UpdateHttpOriginPolicy.MaximumRedirectCount; redirectCount++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (UpdateHttpOriginPolicy.IsRedirect(response.StatusCode))
            {
                if (redirectCount == UpdateHttpOriginPolicy.MaximumRedirectCount)
                    throw new HttpRequestException("The update manifest request exceeded the redirect limit.");

                requestUri = _originPolicy.ResolveTrustedManifestRedirect(requestUri, response.Headers.Location);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"The update manifest request returned HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                    inner: null,
                    statusCode: response.StatusCode);
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        throw new InvalidOperationException("The update manifest request did not complete.");
    }
}