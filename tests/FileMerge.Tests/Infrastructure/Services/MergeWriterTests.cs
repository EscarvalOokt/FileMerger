using System.IO;
using System.Text;
using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Merge;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class MergeWriterTests : IDisposable
{
    private readonly string _tempRoot;

    public MergeWriterTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(MergeWriterTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void Write_Should_Throw_When_Output_Is_Null()
    {
        var writer = new MergeWriter();
        var target = new OutputTarget(Path.Combine(_tempRoot, "merged.txt"));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            writer.Write(null!, target));

        Assert.Equal("output", ex.ParamName);
    }

    [Fact]
    public void Write_Should_Throw_When_Target_Is_Null()
    {
        var writer = new MergeWriter();
        MergeOutput output = CreateOutput("merged content");

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            writer.Write(output, null!));

        Assert.Equal("target", ex.ParamName);
    }

    [Fact]
    public void Write_Should_Create_Output_Directory_And_Write_Content()
    {
        var writer = new MergeWriter();

        string outputPath = Path.Combine(_tempRoot, "nested", "folder", "merged.txt");
        var target = new OutputTarget(outputPath, "utf-8");
        MergeOutput output = CreateOutput("merged content");

        writer.Write(output, target);

        Assert.True(File.Exists(outputPath));
        Assert.Equal("merged content", File.ReadAllText(outputPath, Encoding.UTF8));
    }

    [Fact]
    public void Write_Should_Use_Specified_Encoding()
    {
        var writer = new MergeWriter();

        string outputPath = Path.Combine(_tempRoot, "encoded.txt");
        var target = new OutputTarget(outputPath, "utf-16");
        MergeOutput output = CreateOutput("Привіт");

        writer.Write(output, target);

        Assert.True(File.Exists(outputPath));

        string actual = File.ReadAllText(outputPath, Encoding.Unicode);
        Assert.Equal("Привіт", actual);
    }

    private static MergeOutput CreateOutput(string content)
    {
        return new MergeOutput(
            content: content,
            sections: [],
            statistics: new MergeStatistics(1, 1, 0, content.Length, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}