using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.Entities;

public sealed class MergeOutputTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var inputFile = new InputFile(@"D:\C# Repository\Tests\FileMerger\Test.cs", "Test.cs", ".cs", FileKind.CSharp);
        var section = new MergeSection(inputFile, "content", 0);
        var statistics = new MergeStatistics(1, 1, 0, 7, TimeSpan.FromMilliseconds(10));
        var outputTarget = new OutputTarget(@"C:\out\merged.txt");
        DateTime generatedAt = DateTime.UtcNow;

        var result = new MergeOutput(
            content: "content",
            sections: [section],
            statistics: statistics,
            generatedAtUtc: generatedAt,
            outputTarget: outputTarget);

        Assert.Equal("content", result.Content);
        Assert.Single(result.Sections);
        Assert.Equal(section, result.Sections.Single());
        Assert.Equal(statistics, result.Statistics);
        Assert.Equal(generatedAt, result.GeneratedAtUtc);
        Assert.Equal(outputTarget, result.OutputTarget);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Content_Is_Null()
    {
        var statistics = new MergeStatistics(1, 1, 0, 0, TimeSpan.Zero);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MergeOutput(null!, [], statistics, DateTime.UtcNow));

        Assert.Equal("content", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Sections_Are_Null()
    {
        var statistics = new MergeStatistics(1, 1, 0, 0, TimeSpan.Zero);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MergeOutput("content", null!, statistics, DateTime.UtcNow));

        Assert.Equal("sections", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Statistics_Are_Null()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MergeOutput("content", [], null!, DateTime.UtcNow));

        Assert.Equal("statistics", ex.ParamName);
    }
}