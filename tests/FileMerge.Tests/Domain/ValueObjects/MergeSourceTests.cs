using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class MergeSourceTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var result = new MergeSource(
            path: @"D:\C# Repository\Tests\FileMerger",
            type: MergeSourceType.Directory,
            isRecursive: false,
            isEnabled: false);

        Assert.Equal(@"D:\C# Repository\Tests\FileMerger", result.Path);
        Assert.Equal(MergeSourceType.Directory, result.Type);
        Assert.False(result.IsRecursive);
        Assert.False(result.IsEnabled);
        Assert.NotNull(result.Exclusions);
        Assert.Empty(result.Exclusions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Path_Is_Invalid(string? path)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MergeSource(path!, MergeSourceType.Directory));

        Assert.Equal("path", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Set_Exclusions()
    {
        MergeSourceExclusion[] exclusions = new[]
        {
            new MergeSourceExclusion("bin", MergeSourceExclusionType.Directory),
            new MergeSourceExclusion(@"Secrets\ApiKeys.cs", MergeSourceExclusionType.File, isEnabled: false)
        };

        var result = new MergeSource(
            path: @"D:\C# Repository\Tests\FileMerger",
            type: MergeSourceType.Directory,
            exclusions: exclusions);

        Assert.Equal(exclusions, result.Exclusions);
    }

    [Fact]
    public void Constructor_Should_Initialize_Empty_Exclusions_When_Null_Is_Passed()
    {
        var result = new MergeSource(
            path: @"D:\C# Repository\Tests\FileMerger",
            type: MergeSourceType.Directory,
            exclusions: null);

        Assert.NotNull(result.Exclusions);
        Assert.Empty(result.Exclusions);
    }

    [Fact]
    public void Constructor_Should_Force_IsRecursive_To_False_For_File_Source()
    {
        var result = new MergeSource(
            path: @"D:\C# Repository\Tests\FileMerger\Program.cs",
            type: MergeSourceType.File,
            isRecursive: true);

        Assert.False(result.IsRecursive);
    }
}