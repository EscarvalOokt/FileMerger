using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.Entities;

public sealed class InputFileTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var skipReason = new SkipReason("rule", "excluded by rule");

        var result = new InputFile(
            fullPath: @"D:\C# Repository\Tests\FileMerger\Test.cs",
            relativePath: @"Sub\Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp,
            isIncluded: false,
            sizeInBytes: 123,
            skipReason: skipReason,
            isMergeCandidate: false);

        Assert.Equal(@"D:\C# Repository\Tests\FileMerger\Test.cs", result.FullPath);
        Assert.Equal(@"Sub\Test.cs", result.RelativePath);
        Assert.Equal(".cs", result.Extension);
        Assert.Equal(FileKind.CSharp, result.Kind);
        Assert.False(result.IsIncluded);
        Assert.Equal(123, result.SizeInBytes);
        Assert.Equal(skipReason, result.SkipReason);
        Assert.False(result.IsMergeCandidate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_FullPath_Is_Invalid(string? fullPath)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new InputFile(fullPath!, "Test.cs", ".cs", FileKind.CSharp));

        Assert.Equal("fullPath", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_RelativePath_Is_Invalid(string? relativePath)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new InputFile(@"D:\C# Repository\Tests\FileMerger\Test.cs", relativePath!, ".cs", FileKind.CSharp));

        Assert.Equal("relativePath", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Extension_Is_Invalid(string? extension)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new InputFile(@"D:\C# Repository\Tests\FileMerger\Test.cs", "Test.cs", extension!, FileKind.CSharp));

        Assert.Equal("extension", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Allow_Empty_Extension_For_Extensionless_File()
    {
        var result = new InputFile(
            fullPath: @"D:\Project\LICENSE",
            relativePath: "LICENSE",
            extension: string.Empty,
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason("discovery.unsupported-file-type", "Unsupported."),
            isMergeCandidate: false);

        Assert.Equal(string.Empty, result.Extension);
        Assert.False(result.IsMergeCandidate);
        Assert.False(result.IsIncluded);
    }

    [Fact]
    public void Exclude_Should_Return_New_Instance_With_Excluded_State()
    {
        var source = new InputFile(
            fullPath: @"D:\C# Repository\Tests\FileMerger\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var reason = new SkipReason("skip", "Skipped");

        InputFile result = source.Exclude(reason);

        Assert.NotSame(source, result);
        Assert.True(source.IsIncluded);
        Assert.Null(source.SkipReason);

        Assert.False(result.IsIncluded);
        Assert.Equal(reason, result.SkipReason);
        Assert.Equal(source.IsMergeCandidate, result.IsMergeCandidate);
    }

    [Fact]
    public void Exclude_Should_Throw_When_Reason_Is_Null()
    {
        var source = new InputFile(
            fullPath: @"D:\C# Repository\Tests\FileMerger\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => source.Exclude(null!));

        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public void Include_Should_Return_New_Instance_With_Included_State()
    {
        var source = new InputFile(
            fullPath: @"D:\C# Repository\Tests\FileMerger\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp,
            isIncluded: false,
            skipReason: new SkipReason("skip", "Skipped"));

        InputFile result = source.Include();

        Assert.NotSame(source, result);
        Assert.False(source.IsIncluded);
        Assert.NotNull(source.SkipReason);

        Assert.True(result.IsIncluded);
        Assert.Null(result.SkipReason);
        Assert.Equal(source.IsMergeCandidate, result.IsMergeCandidate);
    }
}