using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Content;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class ContentTransformationServiceTests
{
    [Fact]
    public void Transform_Should_Throw_When_Content_Is_Null()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();
        MergeProfile profile = CreateProfile();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.Transform(null!, file, profile));

        Assert.Equal("content", ex.ParamName);
    }

    [Fact]
    public void Transform_Should_Throw_When_File_Is_Null()
    {
        var service = new ContentTransformationService();
        MergeProfile profile = CreateProfile();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.Transform("content", null!, profile));

        Assert.Equal("file", ex.ParamName);
    }

    [Fact]
    public void Transform_Should_Throw_When_Profile_Is_Null()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.Transform("content", file, null!));

        Assert.Equal("profile", ex.ParamName);
    }

    [Fact]
    public void Transform_Should_Remove_Using_Directives_When_Rule_Enabled_And_Profile_Option_Enabled()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();

        MergeProfile profile = CreateProfile(
            csOptions: new CsMergeOptions(RemoveUsingDirectives: true),
            transformations:
            [
                new ContentTransformationRule(
                    kind: TransformationKind.RemoveUsingDirectives,
                    order: 0,
                    isEnabled: true,
                    appliesTo: [FileKind.CSharp])
            ]);

        string content = """
                         using System;
                         using System.Text;

                         namespace Demo;
                         """;

        string result = service.Transform(content, file, profile);

        Assert.DoesNotContain("using System;", result);
        Assert.DoesNotContain("using System.Text;", result);
        Assert.Contains("namespace Demo;", result);
    }

    [Fact]
    public void Transform_Should_Not_Remove_Using_Directives_When_Profile_Option_Is_Disabled()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();

        MergeProfile profile = CreateProfile(
            csOptions: new CsMergeOptions(RemoveUsingDirectives: false),
            transformations:
            [
                new ContentTransformationRule(
                    kind: TransformationKind.RemoveUsingDirectives,
                    order: 0,
                    isEnabled: true,
                    appliesTo: [FileKind.CSharp])
            ]);

        string content = """
                         using System;

                         namespace Demo;
                         """;

        string result = service.Transform(content, file, profile);

        Assert.Contains("using System;", result);
    }

    [Fact]
    public void Transform_Should_Normalize_Line_Endings_To_LF()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();

        MergeProfile profile = CreateProfile(
            generalOptions: new GeneralMergeOptions(lineEndingMode: LineEndingMode.LF),
            transformations:
            [
                new ContentTransformationRule(
                    kind: TransformationKind.NormalizeLineEndings,
                    order: 0,
                    isEnabled: true)
            ]);

        string content = "line1\r\nline2\r\nline3";

        string result = service.Transform(content, file, profile);

        Assert.DoesNotContain("\r\n", result);
        Assert.Equal("line1\nline2\nline3", result);
    }

    [Fact]
    public void Transform_Should_Normalize_Line_Endings_To_CRLF()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();

        MergeProfile profile = CreateProfile(
            generalOptions: new GeneralMergeOptions(lineEndingMode: LineEndingMode.CRLF),
            transformations:
            [
                new ContentTransformationRule(
                    kind: TransformationKind.NormalizeLineEndings,
                    order: 0,
                    isEnabled: true)
            ]);

        string content = "line1\nline2\nline3";

        string result = service.Transform(content, file, profile);

        Assert.Equal("line1\r\nline2\r\nline3", result);
    }

    [Fact]
    public void Transform_Should_Collapse_Multiple_Empty_Lines()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();

        MergeProfile profile = CreateProfile(
            transformations:
            [
                new ContentTransformationRule(
                    kind: TransformationKind.CollapseMultipleEmptyLines,
                    order: 0,
                    isEnabled: true)
            ]);

        string content = "line1\n\n\nline2\n\n\n\nline3";

        string result = service.Transform(content, file, profile);

        string expected = string.Join(Environment.NewLine + Environment.NewLine,
            "line1",
            "line2",
            "line3");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Transform_Should_Trim_Trailing_Empty_Lines_When_General_Option_Is_Enabled()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();

        MergeProfile profile = CreateProfile(
            generalOptions: new GeneralMergeOptions(trimTrailingEmptyLines: true));

        string content = "line1\r\nline2\r\n\r\n";

        string result = service.Transform(content, file, profile);

        Assert.Equal("line1\r\nline2", result);
    }

    [Fact]
    public void Transform_Should_Respect_Rule_Order()
    {
        var service = new ContentTransformationService();
        InputFile file = CreateCSharpFile();

        MergeProfile profile = CreateProfile(
            generalOptions: new GeneralMergeOptions(lineEndingMode: LineEndingMode.LF),
            transformations:
            [
                new ContentTransformationRule(
                    kind: TransformationKind.CollapseMultipleEmptyLines,
                    order: 1,
                    isEnabled: true),
                new ContentTransformationRule(
                    kind: TransformationKind.NormalizeLineEndings,
                    order: 0,
                    isEnabled: true)
            ]);

        string content = "line1\r\n\r\n\r\nline2";

        string result = service.Transform(content, file, profile);

        string expected = string.Join(Environment.NewLine + Environment.NewLine,
            "line1",
            "line2");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Transform_Should_Not_Apply_CSharp_Specific_Rules_To_Fallback_Text_File()
    {
        ContentTransformationService service = new();

        InputFile file = new(
            fullPath: @"D:\Project\Script.unknown",
            relativePath: "Script.unknown",
            extension: ".unknown",
            kind: FileKind.Text,
            isFallbackText: true);

        MergeProfile profile = CreateProfile(
            csOptions: new CsMergeOptions(RemoveUsingDirectives: true),
            transformations:
            [
                new ContentTransformationRule(
                    kind: TransformationKind.RemoveUsingDirectives,
                    order: 0,
                    isEnabled: true,
                    appliesTo: [FileKind.CSharp])
            ]);

        string content = string.Join(
            Environment.NewLine,
            "using System;",
            "",
            "public class Test {}");

        string result = service.Transform(content, file, profile);

        Assert.Contains("using System;", result);
    }

    private static InputFile CreateCSharpFile()
    {
        return new InputFile(
            fullPath: @"D:\Project\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);
    }

    private static MergeProfile CreateProfile(
        GeneralMergeOptions? generalOptions = null,
        CsMergeOptions? csOptions = null,
        IReadOnlyCollection<ContentTransformationRule>? transformations = null)
    {
        return new MergeProfile(
            name: "Test profile",
            generalOptions: generalOptions ?? new GeneralMergeOptions(),
            csOptions: csOptions ?? new CsMergeOptions(),
            fileTypes:
            [
                new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)
            ],
            filterRules: [],
            transformations: transformations);
    }
}