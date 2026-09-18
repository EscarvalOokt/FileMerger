using FileMerger.Application.Updates;

namespace FileMerger.Tests.Application.Updates;

public sealed class UpdateCompatibilityPolicyTests
{
    private readonly UpdateCompatibilityPolicy _policy = new();
    private readonly SemanticVersion _updaterVersion = SemanticVersion.Parse("1.0.0");

    [Theory]
    [InlineData("1.2.3", "1.2.4", true)]
    [InlineData("1.2.3", "1.2.3", false)]
    [InlineData("1.2.4", "1.2.3", false)]
    [InlineData("1.2.3", "1.3.0-alpha.1", false)]
    [InlineData("1.3.0-alpha.1", "1.3.0-alpha.2", true)]
    [InlineData("1.3.0-alpha.2", "1.3.0", true)]
    public void IsReleaseEligible_Should_Follow_SemVer_And_Prerelease_Rules(
        string currentVersion,
        string releaseVersion,
        bool expected)
    {
        bool result = _policy.IsReleaseEligible(
            SemanticVersion.Parse(currentVersion),
            SemanticVersion.Parse(releaseVersion));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("windows", "any", "net10.0-windows", "frameworkDependent", "zip", true)]
    [InlineData("linux", "any", "net10.0-windows", "frameworkDependent", "zip", false)]
    [InlineData("windows", "x64", "net10.0-windows", "frameworkDependent", "zip", false)]
    [InlineData("windows", "any", "net9.0-windows", "frameworkDependent", "zip", false)]
    [InlineData("windows", "any", "net10.0-windows", "selfContained", "zip", false)]
    [InlineData("windows", "any", "net10.0-windows", "frameworkDependent", "msi", false)]
    public void IsPackageCompatible_Should_Require_Supported_Distribution(
        string os,
        string architecture,
        string framework,
        string deployment,
        string format,
        bool expected)
    {
        UpdatePackage package = CreatePackage(
            os: os,
            architecture: architecture,
            framework: framework,
            deployment: deployment,
            format: format);

        bool result = _policy.IsPackageCompatible(package, SemanticVersion.Parse("1.2.3"), _updaterVersion);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPackageCompatible_Should_Reject_MinimumSourceVersion_Above_Current()
    {
        UpdatePackage package = CreatePackage(minimumSourceVersion: SemanticVersion.Parse("1.2.4"));

        Assert.False(_policy.IsPackageCompatible(package, SemanticVersion.Parse("1.2.3"), _updaterVersion));
    }

    [Fact]
    public void IsPackageCompatible_Should_Accept_MinimumSourceVersion_At_Current()
    {
        UpdatePackage package = CreatePackage(minimumSourceVersion: SemanticVersion.Parse("1.2.3"));

        Assert.True(_policy.IsPackageCompatible(package, SemanticVersion.Parse("1.2.3"), _updaterVersion));
    }

    [Fact]
    public void IsPackageCompatible_Should_Accept_Package_Without_MinimumUpdaterVersion_When_Updater_Is_Unavailable()
    {
        UpdatePackage package = CreatePackage();

        Assert.True(_policy.IsPackageCompatible(package, SemanticVersion.Parse("1.2.3"), currentUpdaterVersion: null));
    }

    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("0.9.0", true)]
    [InlineData("1.0.1", false)]
    public void IsPackageCompatible_Should_Compare_MinimumUpdaterVersion(string minimumUpdaterVersion, bool expected)
    {
        UpdatePackage package = CreatePackage(minimumUpdaterVersion: SemanticVersion.Parse(minimumUpdaterVersion));

        bool result = _policy.IsPackageCompatible(package, SemanticVersion.Parse("1.2.3"), _updaterVersion);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPackageCompatible_Should_Reject_RequiredUpdaterVersion_When_Updater_Is_Unavailable()
    {
        UpdatePackage package = CreatePackage(minimumUpdaterVersion: SemanticVersion.Parse("1.0.0"));

        Assert.False(_policy.IsPackageCompatible(package, SemanticVersion.Parse("1.2.3"), currentUpdaterVersion: null));
    }

    private static UpdatePackage CreatePackage(
        string os = "windows",
        string architecture = "any",
        string framework = "net10.0-windows",
        string deployment = "frameworkDependent",
        string format = "zip",
        SemanticVersion? minimumSourceVersion = null,
        SemanticVersion? minimumUpdaterVersion = null)
    {
        return new UpdatePackage(
            "windows-any",
            SemanticVersion.Parse("1.2.4"),
            os,
            architecture,
            framework,
            deployment,
            format,
            new Uri("https://updates.example.test/packages/filemerger.zip"),
            123,
            new string('a', 64),
            "FileMerger.Wpf.exe",
            minimumSourceVersion,
            minimumUpdaterVersion);
    }
}