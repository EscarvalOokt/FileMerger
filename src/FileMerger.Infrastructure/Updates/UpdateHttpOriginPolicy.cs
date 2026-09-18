using System.Net;

namespace FileMerger.Infrastructure.Updates;

public sealed class UpdateHttpOriginPolicy
{
    public const int MaximumRedirectCount = 5;

    private const string GitHubHost = "github.com";
    private const string GitHubReleaseAssetsHost = "release-assets.githubusercontent.com";
    private const string GitHubRepositoryOwner = "EscarvalOokt";
    private const string GitHubRepositoryName = "FileMerger";

    private readonly EntryAssemblyUpdateManifestUriProvider _manifestUriProvider;

    public UpdateHttpOriginPolicy(EntryAssemblyUpdateManifestUriProvider manifestUriProvider)
    {
        ArgumentNullException.ThrowIfNull(manifestUriProvider);
        _manifestUriProvider = manifestUriProvider;
    }

    public Uri GetTrustedManifestUri()
    {
        return _manifestUriProvider.GetManifestUri();
    }

    public void EnsureTrustedManifestUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        EnsureAbsoluteHttpsUri(uri);

        Uri manifestUri = GetTrustedManifestUri();
        if (!IsSameOrigin(manifestUri, uri))
            throw new HttpRequestException(
                "The update manifest request target is outside the configured trusted origin.");
    }

    public Uri ResolveTrustedManifestRedirect(Uri currentUri, Uri? location)
    {
        ArgumentNullException.ThrowIfNull(currentUri);

        Uri resolved = ResolveRedirect(currentUri, location);
        EnsureTrustedManifestUri(resolved);
        return resolved;
    }

    public void EnsureTrustedPackageUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        EnsureAbsoluteHttpsUri(uri);
        EnsureDefaultHttpsPort(uri);

        if (!string.Equals(uri.Host, GitHubHost, StringComparison.OrdinalIgnoreCase) ||
            !IsTrustedGitHubReleaseAssetPath(uri))
        {
            throw new HttpRequestException(
                $"The update package target must be a versioned GitHub Release asset for " +
                $"'{GitHubRepositoryOwner}/{GitHubRepositoryName}'.");
        }
    }

    public Uri ResolveTrustedPackageRedirect(Uri currentUri, Uri? location)
    {
        ArgumentNullException.ThrowIfNull(currentUri);

        Uri resolved = ResolveRedirect(currentUri, location);

        if (IsTrustedGitHubReleaseAssetUri(currentUri))
        {
            EnsureTrustedReleaseAssetsUri(resolved);
            return resolved;
        }

        if (IsTrustedReleaseAssetsUri(currentUri))
        {
            EnsureTrustedReleaseAssetsUri(resolved);
            return resolved;
        }

        throw new HttpRequestException("The update package redirect source is not trusted.");
    }

    public static bool IsRedirect(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
    }

    private static Uri ResolveRedirect(Uri currentUri, Uri? location)
    {
        if (location is null)
            throw new HttpRequestException("The update redirect did not include a Location header.");

        return location.IsAbsoluteUri ? location : new Uri(currentUri, location);
    }

    private static void EnsureAbsoluteHttpsUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new HttpRequestException("The update request target must be an absolute HTTPS URI.");
    }

    private static void EnsureDefaultHttpsPort(Uri uri)
    {
        if (!uri.IsDefaultPort)
            throw new HttpRequestException("The update request target must use the standard HTTPS port.");
    }

    private static void EnsureTrustedReleaseAssetsUri(Uri uri)
    {
        EnsureAbsoluteHttpsUri(uri);
        EnsureDefaultHttpsPort(uri);

        if (!string.Equals(uri.Host, GitHubReleaseAssetsHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpRequestException(
                $"The update package redirect target must use the trusted GitHub Release asset delivery host " +
                $"'{GitHubReleaseAssetsHost}'.");
        }
    }

    private static bool IsTrustedGitHubReleaseAssetUri(Uri uri)
    {
        return uri.IsAbsoluteUri &&
               string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
               uri.IsDefaultPort &&
               string.Equals(uri.Host, GitHubHost, StringComparison.OrdinalIgnoreCase) &&
               IsTrustedGitHubReleaseAssetPath(uri);
    }

    private static bool IsTrustedReleaseAssetsUri(Uri uri)
    {
        return uri.IsAbsoluteUri &&
               string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
               uri.IsDefaultPort &&
               string.Equals(uri.Host, GitHubReleaseAssetsHost, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTrustedGitHubReleaseAssetPath(Uri uri)
    {
        string[] segments = uri.AbsolutePath.Split('/');

        return segments is [{ Length: 0 }, _, _, _, _, _, _] &&
               string.Equals(segments[1], GitHubRepositoryOwner, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(segments[2], GitHubRepositoryName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(segments[3], "releases", StringComparison.Ordinal) &&
               string.Equals(segments[4], "download", StringComparison.Ordinal) &&
               segments[5].Length > 0 &&
               segments[6].Length > 0;
    }

    private static bool IsSameOrigin(Uri left, Uri right)
    {
        return string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase) &&
               left.Port == right.Port;
    }
}