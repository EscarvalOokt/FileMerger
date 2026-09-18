using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class FileFilterRuleTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var result = new FileFilterRule(
            mode: FilterMode.Exclude,
            target: FilterTarget.FileName,
            patternType: RulePatternType.Wildcard,
            pattern: "*.Designer.cs",
            isEnabled: false,
            description: "Exclude designer files");

        Assert.Equal(FilterMode.Exclude, result.Mode);
        Assert.Equal(FilterTarget.FileName, result.Target);
        Assert.Equal(RulePatternType.Wildcard, result.PatternType);
        Assert.Equal("*.Designer.cs", result.Pattern);
        Assert.False(result.IsEnabled);
        Assert.Equal("Exclude designer files", result.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Pattern_Is_Invalid(string? pattern)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new FileFilterRule(
            FilterMode.Exclude,
            FilterTarget.FileName,
            RulePatternType.Wildcard,
            pattern!));

        Assert.Equal("pattern", ex.ParamName);
    }
}