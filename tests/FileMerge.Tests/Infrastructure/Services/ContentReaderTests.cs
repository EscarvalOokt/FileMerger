using System.IO;
using System.Text;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Infrastructure.Services.Content;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class ContentReaderTests : IDisposable
{
    private readonly string _tempRoot;

    public ContentReaderTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(ContentReaderTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void Read_Should_Throw_When_File_Is_Null()
    {
        var reader = new ContentReader();
        var options = new InputReadOptions(InputEncodingMode.Auto, null, null);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => reader.Read(null!, options));

        Assert.Equal("file", ex.ParamName);
    }

    [Fact]
    public void Read_Should_Throw_When_Options_Are_Null()
    {
        string path = Path.Combine(_tempRoot, "Test.cs");
        File.WriteAllText(path, "class Test {}", Encoding.UTF8);

        var inputFile = new InputFile(
            fullPath: path,
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var reader = new ContentReader();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => reader.Read(inputFile, null!));

        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Read_Should_Return_Success_Result_With_File_Content_In_Utf8_Mode()
    {
        string path = Path.Combine(_tempRoot, "Test.cs");
        string content = "class Test {}";

        File.WriteAllText(path, content, Encoding.UTF8);

        var inputFile = new InputFile(
            fullPath: path,
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var reader = new ContentReader();

        ContentReadResult result = reader.Read(inputFile, new InputReadOptions(
            InputEncodingMode.Utf8,
            null,
            null));

        Assert.True(result.IsSuccessful);
        Assert.Equal(content, result.Content);
        Assert.Equal("utf-8", result.EncodingName);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void Read_Should_Read_Utf8_Text_Correctly_In_Auto_Mode()
    {
        string path = Path.Combine(_tempRoot, "Test.txt");
        string content = "Привіт, FileMerger";

        File.WriteAllText(path, content, Encoding.UTF8);

        var inputFile = new InputFile(
            fullPath: path,
            relativePath: "Test.txt",
            extension: ".txt",
            kind: FileKind.Text);

        var reader = new ContentReader();

        ContentReadResult result = reader.Read(inputFile, new InputReadOptions(
            InputEncodingMode.Auto,
            null,
            "windows-1251"));

        Assert.True(result.IsSuccessful);
        Assert.Equal(content, result.Content);
        Assert.Equal("utf-8", result.EncodingName);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void Read_Should_Read_File_With_Utf8Bom_In_Auto_Mode()
    {
        string path = Path.Combine(_tempRoot, "Bom.txt");
        string content = "UTF8 BOM text";
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var inputFile = new InputFile(
            fullPath: path,
            relativePath: "Bom.txt",
            extension: ".txt",
            kind: FileKind.Text);

        var reader = new ContentReader();

        ContentReadResult result = reader.Read(inputFile, new InputReadOptions(
            InputEncodingMode.Auto,
            null,
            "windows-1251"));

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Content);
        Assert.Contains("UTF8 BOM text", result.Content);
        Assert.Equal("utf-8", result.EncodingName);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void Read_Should_Read_File_With_Specific_Encoding_When_Mode_Is_Specific()
    {
        string path = Path.Combine(_tempRoot, "Cp1251.txt");
        string content = "Привіт світ";
        var encoding = Encoding.GetEncoding("windows-1251");

        File.WriteAllText(path, content, encoding);

        var inputFile = new InputFile(
            fullPath: path,
            relativePath: "Cp1251.txt",
            extension: ".txt",
            kind: FileKind.Text);

        var reader = new ContentReader();

        ContentReadResult result = reader.Read(inputFile, new InputReadOptions(
            InputEncodingMode.Specific,
            "windows-1251",
            null));

        Assert.True(result.IsSuccessful);
        Assert.Equal(content, result.Content);
        Assert.Equal("windows-1251", result.EncodingName);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void Read_Should_Use_Fallback_Encoding_When_Utf8_Decoding_Fails()
    {
        string path = Path.Combine(_tempRoot, "Fallback.txt");
        string content = "Привіт світ";
        var encoding = Encoding.GetEncoding("windows-1251");

        File.WriteAllText(path, content, encoding);

        var inputFile = new InputFile(
            fullPath: path,
            relativePath: "Fallback.txt",
            extension: ".txt",
            kind: FileKind.Text);

        var reader = new ContentReader();

        ContentReadResult result = reader.Read(inputFile, new InputReadOptions(
            InputEncodingMode.Auto,
            null,
            "windows-1251"));

        Assert.True(result.IsSuccessful);
        Assert.Equal(content, result.Content);
        Assert.Equal("windows-1251", result.EncodingName);
        Assert.Null(result.Issue);
    }

    [Fact]
    public void Read_Should_Return_Failure_Result_When_Specific_Encoding_Name_Is_Invalid()
    {
        string path = Path.Combine(_tempRoot, "Test.txt");
        File.WriteAllText(path, "Hello", Encoding.UTF8);

        var inputFile = new InputFile(
            fullPath: path,
            relativePath: "Test.txt",
            extension: ".txt",
            kind: FileKind.Text);

        var reader = new ContentReader();

        ContentReadResult result = reader.Read(inputFile, new InputReadOptions(
            InputEncodingMode.Specific,
            "invalid-encoding-name",
            null));

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Content);
        Assert.Null(result.EncodingName);
        Assert.NotNull(result.Issue);
        Assert.Equal("file.read.failed", result.Issue!.Code);
        Assert.Contains("invalid-encoding-name", result.Issue.Message);
    }

    [Fact]
    public void Read_Should_Return_Failure_Result_When_File_Does_Not_Exist()
    {
        string path = Path.Combine(_tempRoot, "Missing.cs");

        var inputFile = new InputFile(
            fullPath: path,
            relativePath: "Missing.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var reader = new ContentReader();

        ContentReadResult result = reader.Read(inputFile, new InputReadOptions(
            InputEncodingMode.Auto,
            null,
            null));

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Content);
        Assert.Null(result.EncodingName);
        Assert.NotNull(result.Issue);
        Assert.Equal("file.read.failed", result.Issue!.Code);
        Assert.Equal(ValidationSeverity.Warning, result.Issue.Severity);
        Assert.Contains("Missing.cs", result.Issue.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}