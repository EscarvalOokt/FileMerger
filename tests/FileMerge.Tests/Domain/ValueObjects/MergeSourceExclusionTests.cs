using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class MergeSourceExclusionTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var result = new MergeSourceExclusion(
            relativePath: @"bin\Debug",
            type: MergeSourceExclusionType.Directory,
            isEnabled: false);

        Assert.Equal(@"bin\Debug", result.RelativePath);
        Assert.Equal(MergeSourceExclusionType.Directory, result.Type);
        Assert.False(result.IsEnabled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_RelativePath_Is_Invalid(string? relativePath)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MergeSourceExclusion(relativePath!, MergeSourceExclusionType.Directory));

        Assert.Equal("relativePath", ex.ParamName);
    }

    [Theory]
    [InlineData(@"..\Secret")]
    [InlineData(@"src\..\Secret")]
    [InlineData("../Secret")]
    [InlineData("src/../Secret")]
    public void Constructor_Should_Throw_When_RelativePath_Contains_ParentTraversal(string relativePath)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MergeSourceExclusion(relativePath, MergeSourceExclusionType.Directory));

        Assert.Equal("relativePath", ex.ParamName);
    }

    [Theory]
    [InlineData(@"D:\Project\bin")]
    [InlineData(@"\Project\bin")]
    [InlineData(@"/Project/bin")]
    public void Constructor_Should_Throw_When_RelativePath_Is_Rooted(string relativePath)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MergeSourceExclusion(relativePath, MergeSourceExclusionType.Directory));

        Assert.Equal("relativePath", ex.ParamName);
    }

    [Theory]
    [InlineData(@"bin\", "bin")]
    [InlineData(@" bin\Debug ", @"bin\Debug")]
    [InlineData(@"bin/Debug/", @"bin\Debug")]
    public void Constructor_Should_Normalize_RelativePath(string relativePath, string expectedRelativePath)
    {
        var result = new MergeSourceExclusion(relativePath, MergeSourceExclusionType.Directory);

        Assert.Equal(expectedRelativePath, result.RelativePath);
    }

    [Fact]
    public void Constructor_Should_Default_IsEnabled_To_True()
    {
        var result = new MergeSourceExclusion(relativePath: "bin", type: MergeSourceExclusionType.Directory);

        Assert.True(result.IsEnabled);
    }
}