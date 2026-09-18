using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using FileMerger.Infrastructure.Updates;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class UpdateHttpOriginPolicyTests
{
    private const string TrustedPackageUrl =
        "https://github.com/EscarvalOokt/FileMerger/releases/download/v1.2.4/FileMerger-1.2.4.zip";

    [Fact]
    public void EnsureTrustedManifestUri_Should_Accept_SameOrigin_Https_Uri()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        policy.EnsureTrustedManifestUri(new Uri("https://updates.example.test/manifests/v2.json"));
    }

    [Fact]
    public void EnsureTrustedManifestUri_Should_Reject_Http()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() =>
            policy.EnsureTrustedManifestUri(new Uri("http://updates.example.test/manifest.json")));

        Assert.Contains("HTTPS", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureTrustedManifestUri_Should_Reject_CrossOrigin_Uri()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() =>
            policy.EnsureTrustedManifestUri(new Uri("https://cdn.example.test/manifest.json")));

        Assert.Contains("trusted origin", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTrustedManifestRedirect_Should_Resolve_Relative_SameOrigin_Uri()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        Uri result = policy.ResolveTrustedManifestRedirect(
            new Uri("https://updates.example.test/manifest.json"),
            new Uri("manifest-v2.json", UriKind.Relative));

        Assert.Equal(new Uri("https://updates.example.test/manifest-v2.json"), result);
    }

    [Fact]
    public void ResolveTrustedManifestRedirect_Should_Reject_Missing_Location()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() => policy.ResolveTrustedManifestRedirect(
            new Uri("https://updates.example.test/manifest.json"),
            location: null));

        Assert.Contains("Location", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureTrustedPackageUri_Should_Accept_Versioned_FileMerger_GitHub_Release_Asset()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        policy.EnsureTrustedPackageUri(new Uri(TrustedPackageUrl));
    }

    [Fact]
    public void EnsureTrustedPackageUri_Should_Reject_Http()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() =>
            policy.EnsureTrustedPackageUri(
                new Uri("http://github.com/EscarvalOokt/FileMerger/releases/download/v1.2.4/FileMerger.zip")));

        Assert.Contains("HTTPS", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureTrustedPackageUri_Should_Reject_Custom_Https_Port()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() => policy.EnsureTrustedPackageUri(
            new Uri("https://github.com:8443/EscarvalOokt/FileMerger/releases/download/v1.2.4/FileMerger.zip")));

        Assert.Contains("standard HTTPS port", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://github.com/OtherOwner/FileMerger/releases/download/v1.2.4/FileMerger.zip")]
    [InlineData("https://github.com/EscarvalOokt/OtherRepository/releases/download/v1.2.4/FileMerger.zip")]
    [InlineData("https://github.com/EscarvalOokt/FileMerger/releases/latest/download/FileMerger.zip")]
    [InlineData("https://github.com/EscarvalOokt/FileMerger/releases/tag/v1.2.4")]
    [InlineData("https://github.com/EscarvalOokt/FileMerger/releases/download/v1.2.4/FileMerger.zip/extra")]
    [InlineData("https://release-assets.githubusercontent.com/github-production-release-asset/123/asset.zip")]
    [InlineData("https://cdn.example.test/FileMerger.zip")]
    public void EnsureTrustedPackageUri_Should_Reject_NonCanonical_Initial_Uri(string packageUrl)
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() =>
            policy.EnsureTrustedPackageUri(new Uri(packageUrl)));

        Assert.Contains("versioned GitHub Release asset", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTrustedPackageRedirect_Should_Accept_GitHub_To_ReleaseAssets_And_Preserve_Signed_Query()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();
        Uri expected = new(
            "https://release-assets.githubusercontent.com/github-production-release-asset/123/asset.zip" +
            "?sp=r&sv=2026-01-01&sig=abc%2Fdef");

        Uri result = policy.ResolveTrustedPackageRedirect(new Uri(TrustedPackageUrl), expected);

        Assert.Equal(expected, result);
        Assert.Equal(expected.Query, result.Query);
    }

    [Fact]
    public void ResolveTrustedPackageRedirect_Should_Accept_SameHost_ReleaseAssets_Redirect()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();
        Uri current = new(
            "https://release-assets.githubusercontent.com/github-production-release-asset/123/asset.zip?sig=first");

        Uri result = policy.ResolveTrustedPackageRedirect(
            current,
            new Uri("asset-v2.zip?sig=second", UriKind.Relative));

        Assert.Equal(
            new Uri(
                "https://release-assets.githubusercontent.com/github-production-release-asset/123/asset-v2.zip?sig=second"),
            result);
    }

    [Theory]
    [InlineData("http://release-assets.githubusercontent.com/github-production-release-asset/123/asset.zip")]
    [InlineData("https://cdn.example.test/asset.zip")]
    [InlineData("https://github.com/EscarvalOokt/FileMerger/releases/download/v1.2.4/other.zip")]
    public void ResolveTrustedPackageRedirect_Should_Reject_Untrusted_Target_From_GitHub(string redirectUrl)
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() =>
            policy.ResolveTrustedPackageRedirect(new Uri(TrustedPackageUrl), new Uri(redirectUrl)));

        Assert.Contains("target", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTrustedPackageRedirect_Should_Reject_DeliveryHost_Redirect_To_Other_Host()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();
        Uri current = new(
            "https://release-assets.githubusercontent.com/github-production-release-asset/123/asset.zip?sig=first");

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() =>
            policy.ResolveTrustedPackageRedirect(current, new Uri("https://objects.githubusercontent.com/asset.zip")));

        Assert.Contains("redirect target", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTrustedPackageRedirect_Should_Reject_Untrusted_Source()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() => policy.ResolveTrustedPackageRedirect(
            new Uri("https://cdn.example.test/asset.zip"),
            new Uri("https://release-assets.githubusercontent.com/asset.zip")));

        Assert.Contains("redirect source", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTrustedPackageRedirect_Should_Reject_Missing_Location()
    {
        UpdateHttpOriginPolicy policy = CreatePolicy();

        HttpRequestException ex = Assert.Throws<HttpRequestException>(() => policy.ResolveTrustedPackageRedirect(
            new Uri(TrustedPackageUrl),
            location: null));

        Assert.Contains("Location", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static UpdateHttpOriginPolicy CreatePolicy()
    {
        EntryAssemblyUpdateManifestUriProvider provider = new(
            CreateAssemblyWithMetadata("https://updates.example.test/manifest.json"));
        return new UpdateHttpOriginPolicy(provider);
    }

    private static Assembly CreateAssemblyWithMetadata(string value)
    {
        AssemblyName name = new($"UpdateOriginPolicy_{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        ConstructorInfo constructor =
            typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!;
        CustomAttributeBuilder attribute = new(
            constructor,
            [EntryAssemblyUpdateManifestUriProvider.MetadataKey, value]);
        assembly.SetCustomAttribute(attribute);

        return assembly;
    }
}