using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain;

public sealed class BuiltInFileTypeCatalogTests
{
    [Fact]
    public void GetAll_Should_Return_KnownFileTypes_All()
    {
        var catalog = new BuiltInFileTypeCatalog();

        string[] expected = KnownFileTypes.All
            .Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] actual = catalog.GetAll()
            .Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetDefault_Should_Return_KnownFileTypes_Default()
    {
        var catalog = new BuiltInFileTypeCatalog();

        string[] expected = KnownFileTypes.Default
            .Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] actual = catalog.GetDefault()
            .Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FindByExtension_Should_Return_FileType_CaseInsensitively()
    {
        var catalog = new BuiltInFileTypeCatalog();

        FileTypeDefinition? result = catalog.FindByExtension(".CS");

        Assert.NotNull(result);
        Assert.Equal(".cs", result!.Extension);
    }

    [Fact]
    public void FindByExtension_Should_Return_Null_When_Extension_Is_Unknown()
    {
        var catalog = new BuiltInFileTypeCatalog();

        FileTypeDefinition? result = catalog.FindByExtension(".unknown");

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void FindByExtension_Should_Return_Null_When_Extension_Is_Empty(string? extension)
    {
        var catalog = new BuiltInFileTypeCatalog();

        FileTypeDefinition? result = catalog.FindByExtension(extension!);

        Assert.Null(result);
    }

    [Fact]
    public void FindByExtension_Should_Return_Slnx_FileType_CaseInsensitively()
    {
        var catalog = new BuiltInFileTypeCatalog();

        FileTypeDefinition? result = catalog.FindByExtension(".SLNX");

        Assert.NotNull(result);
        Assert.Equal(".slnx", result!.Extension);
        Assert.Equal("Visual Studio XML solution", result.DisplayName);
        Assert.Equal(FileKind.Xml, result.Kind);
    }

    [Fact]
    public void FindByExtension_Should_Return_EditorConfig_FileType_CaseInsensitively()
    {
        var catalog = new BuiltInFileTypeCatalog();

        FileTypeDefinition? result = catalog.FindByExtension(".EDITORCONFIG");

        Assert.NotNull(result);
        Assert.Equal(".editorconfig", result!.Extension);
        Assert.Equal("EditorConfig", result.DisplayName);
        Assert.Equal(FileKind.Text, result.Kind);
    }

    [Fact]
    public void FindByExtension_Should_Return_Props_FileType_CaseInsensitively()
    {
        var catalog = new BuiltInFileTypeCatalog();

        FileTypeDefinition? result = catalog.FindByExtension(".PROPS");

        Assert.NotNull(result);
        Assert.Equal(".props", result!.Extension);
        Assert.Equal("MSBuild props", result.DisplayName);
        Assert.Equal(FileKind.Xml, result.Kind);
    }

    [Fact]
    public void FindByExtension_Should_Return_Targets_FileType_CaseInsensitively()
    {
        var catalog = new BuiltInFileTypeCatalog();

        FileTypeDefinition? result = catalog.FindByExtension(".TARGETS");

        Assert.NotNull(result);
        Assert.Equal(".targets", result!.Extension);
        Assert.Equal("MSBuild targets", result.DisplayName);
        Assert.Equal(FileKind.Xml, result.Kind);
    }
}