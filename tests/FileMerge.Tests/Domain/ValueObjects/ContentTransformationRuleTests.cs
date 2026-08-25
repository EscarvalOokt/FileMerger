using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class ContentTransformationRuleTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var result = new ContentTransformationRule(
            kind: TransformationKind.RemoveUsingDirectives,
            order: 3,
            isEnabled: false,
            appliesTo: [FileKind.CSharp, FileKind.Xaml]);

        Assert.Equal(TransformationKind.RemoveUsingDirectives, result.Kind);
        Assert.Equal(3, result.Order);
        Assert.False(result.IsEnabled);
        Assert.Equal(2, result.AppliesTo.Count);
        Assert.Contains(FileKind.CSharp, result.AppliesTo);
        Assert.Contains(FileKind.Xaml, result.AppliesTo);
    }

    [Fact]
    public void Constructor_Should_Initialize_Empty_AppliesTo_When_Null_Is_Passed()
    {
        var result = new ContentTransformationRule(
            kind: TransformationKind.NormalizeLineEndings,
            order: 1,
            appliesTo: null);

        Assert.NotNull(result.AppliesTo);
        Assert.Empty(result.AppliesTo);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Order_Is_Negative()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ContentTransformationRule(
                kind: TransformationKind.NormalizeLineEndings,
                order: -1));

        Assert.Equal("order", ex.ParamName);
    }
}