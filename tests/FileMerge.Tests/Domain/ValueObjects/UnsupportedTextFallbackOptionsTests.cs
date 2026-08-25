using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class UnsupportedTextFallbackOptionsTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        UnsupportedTextFallbackOptions result = new(
            isEnabled: true,
            maxFileSizeBytes: 123,
            probeSizeBytes: 45,
            maxControlCharacterRatio: 0.25);

        Assert.True(result.IsEnabled);
        Assert.Equal(123, result.MaxFileSizeBytes);
        Assert.Equal(45, result.ProbeSizeBytes);
        Assert.Equal(0.25, result.MaxControlCharacterRatio);
    }

    [Fact]
    public void Disabled_Should_Use_Disabled_Defaults()
    {
        UnsupportedTextFallbackOptions result = UnsupportedTextFallbackOptions.Disabled;

        Assert.False(result.IsEnabled);
        Assert.Equal(UnsupportedTextFallbackOptions.DefaultMaxFileSizeBytes, result.MaxFileSizeBytes);
        Assert.Equal(UnsupportedTextFallbackOptions.DefaultProbeSizeBytes, result.ProbeSizeBytes);
        Assert.Equal(UnsupportedTextFallbackOptions.DefaultMaxControlCharacterRatio, result.MaxControlCharacterRatio);
    }

    [Fact]
    public void Enabled_Should_Use_Enabled_Defaults()
    {
        UnsupportedTextFallbackOptions result = UnsupportedTextFallbackOptions.Enabled;

        Assert.True(result.IsEnabled);
        Assert.Equal(UnsupportedTextFallbackOptions.DefaultMaxFileSizeBytes, result.MaxFileSizeBytes);
        Assert.Equal(UnsupportedTextFallbackOptions.DefaultProbeSizeBytes, result.ProbeSizeBytes);
        Assert.Equal(UnsupportedTextFallbackOptions.DefaultMaxControlCharacterRatio, result.MaxControlCharacterRatio);
    }

    [Theory]
    [InlineData(-1)]
    public void Constructor_Should_Throw_When_MaxFileSizeBytes_Is_Invalid(long value)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnsupportedTextFallbackOptions(maxFileSizeBytes: value));

        Assert.Equal("maxFileSizeBytes", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Should_Throw_When_ProbeSizeBytes_Is_Invalid(int value)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnsupportedTextFallbackOptions(probeSizeBytes: value));

        Assert.Equal("probeSizeBytes", ex.ParamName);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Constructor_Should_Throw_When_MaxControlCharacterRatio_Is_Invalid(double value)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnsupportedTextFallbackOptions(maxControlCharacterRatio: value));

        Assert.Equal("maxControlCharacterRatio", ex.ParamName);
    }
}