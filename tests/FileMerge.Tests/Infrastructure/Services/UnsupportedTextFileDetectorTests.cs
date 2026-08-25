using System.IO;
using System.Text;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Discovery;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class UnsupportedTextFileDetectorTests : IDisposable
{
    private readonly string _tempRoot;

    public UnsupportedTextFileDetectorTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(UnsupportedTextFileDetectorTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void IsTextCandidate_Should_Return_False_When_Fallback_Is_Disabled()
    {
        string path = CreateTextFile("notes.foo", "hello");

        UnsupportedTextFileDetector detector = new();

        bool result = detector.IsTextCandidate(
            path,
            UnsupportedTextFallbackOptions.Disabled);

        Assert.False(result);
    }

    [Fact]
    public void IsTextCandidate_Should_Return_True_For_Utf8_Text_File()
    {
        string path = CreateTextFile("notes.foo", "hello\nworld");

        UnsupportedTextFileDetector detector = new();

        bool result = detector.IsTextCandidate(
            path,
            UnsupportedTextFallbackOptions.Enabled);

        Assert.True(result);
    }

    [Fact]
    public void IsTextCandidate_Should_Return_True_For_Empty_File()
    {
        string path = CreateTextFile("empty.foo", string.Empty);

        UnsupportedTextFileDetector detector = new();

        bool result = detector.IsTextCandidate(
            path,
            UnsupportedTextFallbackOptions.Enabled);

        Assert.True(result);
    }

    [Fact]
    public void IsTextCandidate_Should_Return_False_For_File_With_Null_Byte()
    {
        string path = Path.Combine(_tempRoot, "binary.foo");
        File.WriteAllBytes(path, [0x48, 0x65, 0x00, 0x6C, 0x6F]);

        UnsupportedTextFileDetector detector = new();

        bool result = detector.IsTextCandidate(
            path,
            UnsupportedTextFallbackOptions.Enabled);

        Assert.False(result);
    }

    [Fact]
    public void IsTextCandidate_Should_Return_False_For_File_With_High_Control_Character_Ratio()
    {
        string path = Path.Combine(_tempRoot, "control.foo");
        File.WriteAllBytes(path, [0x01, 0x02, 0x03, 0x04, 0x41]);

        UnsupportedTextFileDetector detector = new();

        bool result = detector.IsTextCandidate(
            path,
            new UnsupportedTextFallbackOptions(
                isEnabled: true,
                maxControlCharacterRatio: 0.10));

        Assert.False(result);
    }

    [Fact]
    public void IsTextCandidate_Should_Return_False_When_File_Is_Larger_Than_Max_Size()
    {
        string path = CreateTextFile("large.foo", "1234567890");

        UnsupportedTextFileDetector detector = new();

        bool result = detector.IsTextCandidate(
            path,
            new UnsupportedTextFallbackOptions(
                isEnabled: true,
                maxFileSizeBytes: 5));

        Assert.False(result);
    }

    [Fact]
    public void IsTextCandidate_Should_Read_Only_Probe_Size()
    {
        string path = Path.Combine(_tempRoot, "probe.foo");

        byte[] textPrefix = Encoding.UTF8.GetBytes("hello");
        byte[] binaryTail = [0x00, 0x01, 0x02];

        File.WriteAllBytes(path, [.. textPrefix, .. binaryTail]);

        UnsupportedTextFileDetector detector = new();

        bool result = detector.IsTextCandidate(
            path,
            new UnsupportedTextFallbackOptions(
                isEnabled: true,
                maxFileSizeBytes: 100,
                probeSizeBytes: textPrefix.Length));

        Assert.True(result);
    }

    [Fact]
    public void IsTextCandidate_Should_Return_False_When_File_Does_Not_Exist()
    {
        string path = Path.Combine(_tempRoot, "missing.foo");

        UnsupportedTextFileDetector detector = new();

        bool result = detector.IsTextCandidate(
            path,
            UnsupportedTextFallbackOptions.Enabled);

        Assert.False(result);
    }

    private string CreateTextFile(string relativePath, string content)
    {
        string path = Path.Combine(_tempRoot, relativePath);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}