using FileMerger.Application.Updates;

namespace FileMerger.Tests.Application.Updates;

public sealed class SemanticVersionTests
{
    [Theory]
    [InlineData("1.2.3", "1.2.4")]
    [InlineData("1.2.3-alpha", "1.2.3")]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1")]
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta")]
    [InlineData("1.0.0-alpha.beta", "1.0.0-beta")]
    [InlineData("1.0.0-beta", "1.0.0-beta.2")]
    [InlineData("1.0.0-beta.2", "1.0.0-beta.11")]
    [InlineData("1.0.0-beta.11", "1.0.0-rc.1")]
    [InlineData("1.0.0-rc.1", "1.0.0")]
    public void CompareTo_Should_Follow_SemVer_Precedence(string lower, string higher)
    {
        var lowerVersion = SemanticVersion.Parse(lower);
        var higherVersion = SemanticVersion.Parse(higher);

        Assert.True(lowerVersion.CompareTo(higherVersion) < 0);
        Assert.True(higherVersion.CompareTo(lowerVersion) > 0);
        Assert.True(lowerVersion < higherVersion);
        Assert.True(higherVersion > lowerVersion);
    }

    [Fact]
    public void CompareTo_Should_Ignore_Build_Metadata_For_Precedence()
    {
        var first = SemanticVersion.Parse("1.2.3+build.1");
        var second = SemanticVersion.Parse("1.2.3+build.2");

        Assert.Equal(0, first.CompareTo(second));
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("01.2.3")]
    [InlineData("1.02.3")]
    [InlineData("1.2.03")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3+")]
    [InlineData("1.2.3-alpha..1")]
    [InlineData("1.2.3-alpha.01")]
    [InlineData("1.2.3+build..1")]
    [InlineData("1.2.3+build+other")]
    [InlineData("1.2.3_alpha")]
    [InlineData("")]
    public void TryParse_Should_Reject_Invalid_SemVer(string value)
    {
        bool parsed = SemanticVersion.TryParse(value, out SemanticVersion? version);

        Assert.False(parsed);
        Assert.Null(version);
    }

    [Fact]
    public void Parse_Should_Preserve_Valid_Prerelease_And_Build_Metadata()
    {
        var version = SemanticVersion.Parse("2.10.4-alpha.7+build.20260919");

        Assert.Equal(2, version.Major);
        Assert.Equal(10, version.Minor);
        Assert.Equal(4, version.Patch);
        Assert.True(version.IsPrerelease);
        Assert.Equal(["alpha", "7"], version.PrereleaseIdentifiers);
        Assert.Equal(["build", "20260919"], version.BuildMetadataIdentifiers);
        Assert.Equal("2.10.4-alpha.7+build.20260919", version.ToString());
    }
}