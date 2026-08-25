using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class ValidationIssueTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var result = new ValidationIssue(
            severity: ValidationSeverity.Warning,
            code: "warn",
            message: "Something happened");

        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Equal("warn", result.Code);
        Assert.Equal("Something happened", result.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Code_Is_Invalid(string? code)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ValidationIssue(ValidationSeverity.Error, code!, "message"));

        Assert.Equal("code", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Message_Is_Invalid(string? message)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ValidationIssue(ValidationSeverity.Error, "code", message!));

        Assert.Equal("message", ex.ParamName);
    }
}