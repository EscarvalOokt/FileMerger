using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;

namespace FileMerger.Tests.Domain.Entities;

public sealed class MergeSectionTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var inputFile = new InputFile(
            fullPath: @"D:\C# Repository\Tests\FileMerger   ",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var result = new MergeSection(
            sourceFile: inputFile,
            content: "class Test {}",
            order: 5,
            headerText: "// FILE: Test.cs");

        Assert.Equal(inputFile, result.SourceFile);
        Assert.Equal("class Test {}", result.Content);
        Assert.Equal(5, result.Order);
        Assert.Equal("// FILE: Test.cs", result.HeaderText);
    }

    [Fact]
    public void Constructor_Should_Throw_When_SourceFile_Is_Null()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new MergeSection(null!, "content", 0));

        Assert.Equal("sourceFile", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Order_Is_Negative()
    {
        var inputFile = new InputFile(
            fullPath: @"D:\C# Repository\Tests\FileMerger\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MergeSection(inputFile, "content", -1));

        Assert.Equal("order", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Replace_Null_Content_With_Empty_String()
    {
        var inputFile = new InputFile(
            fullPath: @"D:\C# Repository\Tests\FileMerger\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var result = new MergeSection(inputFile, null!, 0);

        Assert.Equal(string.Empty, result.Content);
    }
}