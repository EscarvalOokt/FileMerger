using FileMerger.Domain.Entities;

namespace FileMerger.Tests.Domain.Entities;

public sealed class MergeStatisticsTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var duration = TimeSpan.FromSeconds(2);

        var result = new MergeStatistics(
            filesScanned: 10,
            filesIncluded: 7,
            filesSkipped: 3,
            totalCharacters: 2500,
            duration: duration);

        Assert.Equal(10, result.FilesScanned);
        Assert.Equal(7, result.FilesIncluded);
        Assert.Equal(3, result.FilesSkipped);
        Assert.Equal(2500, result.TotalCharacters);
        Assert.Equal(duration, result.Duration);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0, "filesScanned")]
    [InlineData(0, -1, 0, 0, "filesIncluded")]
    [InlineData(0, 0, -1, 0, "filesSkipped")]
    [InlineData(0, 0, 0, -1, "totalCharacters")]
    public void Constructor_Should_Throw_When_Numeric_Argument_Is_Negative(
        int filesScanned,
        int filesIncluded,
        int filesSkipped,
        int totalCharacters,
        string expectedParamName)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MergeStatistics(filesScanned, filesIncluded, filesSkipped, totalCharacters, TimeSpan.Zero));

        Assert.Equal(expectedParamName, ex.ParamName);
    }
}