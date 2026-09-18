using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class OutputTargetTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var result = new OutputTarget(@"D:\C# Repository\Tests\FileMerger\out\merged.txt", "utf-8");

        Assert.Equal(@"D:\C# Repository\Tests\FileMerger\out\merged.txt", result.Path);
        Assert.Equal("utf-8", result.EncodingName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Path_Is_Invalid(string? path)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new OutputTarget(path!));

        Assert.Equal("path", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_EncodingName_Is_Invalid(string? encodingName)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new OutputTarget(
            @"D:\C# Repository\Tests\FileMerger\out\merged.txt",
            encodingName!));

        Assert.Equal("encodingName", ex.ParamName);
    }
}