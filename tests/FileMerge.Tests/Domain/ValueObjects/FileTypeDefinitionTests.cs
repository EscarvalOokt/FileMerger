using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class FileTypeDefinitionTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var result = new FileTypeDefinition(
            extension: ".cs",
            displayName: "C# source",
            kind: FileKind.CSharp,
            isEnabled: false,
            supportsLanguageSpecificProcessing: true);

        Assert.Equal(".cs", result.Extension);
        Assert.Equal("C# source", result.DisplayName);
        Assert.Equal(FileKind.CSharp, result.Kind);
        Assert.False(result.IsEnabled);
        Assert.True(result.SupportsLanguageSpecificProcessing);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Extension_Is_Invalid(string? extension)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FileTypeDefinition(extension!, "C# source", FileKind.CSharp));

        Assert.Equal("extension", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Extension_Does_Not_Start_With_Dot()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FileTypeDefinition("cs", "C# source", FileKind.CSharp));

        Assert.Equal("extension", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_DisplayName_Is_Invalid(string? displayName)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new FileTypeDefinition(".cs", displayName!, FileKind.CSharp));

        Assert.Equal("displayName", ex.ParamName);
    }
}