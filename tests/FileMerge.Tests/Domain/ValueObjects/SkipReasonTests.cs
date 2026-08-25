using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class SkipReasonTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        SkipReason result = new("skip", "Skipped by rule");

        Assert.Equal("skip", result.Code);
        Assert.Equal("Skipped by rule", result.Description);
        Assert.Null(result.RuleDetails);
    }

    [Fact]
    public void Constructor_Should_Set_RuleDetails()
    {
        SkipRuleDetails ruleDetails = new(
            mode: FilterMode.Exclude,
            target: FilterTarget.FileName,
            patternType: RulePatternType.Wildcard,
            pattern: "*.Designer.cs",
            description: "Exclude designer files");

        SkipReason result = new(
            code: "filter.rule.exclude",
            description: "Exclude designer files",
            ruleDetails: ruleDetails);

        Assert.Equal("filter.rule.exclude", result.Code);
        Assert.Equal("Exclude designer files", result.Description);
        Assert.Equal(ruleDetails, result.RuleDetails);
    }

    [Fact]
    public void Constructor_Should_Allow_Null_RuleDetails()
    {
        SkipReason result = new(
            code: "manual.exclude",
            description: "Excluded manually by user.");

        Assert.Equal("manual.exclude", result.Code);
        Assert.Equal("Excluded manually by user.", result.Description);
        Assert.Null(result.RuleDetails);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Code_Is_Invalid(string? code)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new SkipReason(code!, "description"));

        Assert.Equal("code", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Description_Is_Invalid(string? description)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new SkipReason("code", description!));

        Assert.Equal("description", ex.ParamName);
    }
}