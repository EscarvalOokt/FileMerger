using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class MergeProfileTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var generalOptions = new GeneralMergeOptions(includeHeaderComment: true);
        var csOptions = new CsMergeOptions(RemoveUsingDirectives: true);
        FileTypeDefinition[] fileTypes = new[]
        {
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)
        };
        FileFilterRule[] filterRules = new[]
        {
            new FileFilterRule(FilterMode.Exclude, FilterTarget.FileName, RulePatternType.Exact, "AssemblyInfo.cs")
        };
        ContentTransformationRule[] transformations = new[]
        {
            new ContentTransformationRule(TransformationKind.TrimTrailingEmptyLines, 0)
        };

        var result = new MergeProfile(
            name: "Profile 1",
            generalOptions: generalOptions,
            csOptions: csOptions,
            fileTypes: fileTypes,
            filterRules: filterRules,
            transformations: transformations);

        Assert.Equal("Profile 1", result.Name);
        Assert.Equal(generalOptions, result.GeneralOptions);
        Assert.Equal(csOptions, result.CsOptions);
        Assert.Equal(fileTypes, result.FileTypes);
        Assert.Equal(filterRules, result.FilterRules);
        Assert.Equal(transformations, result.Transformations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Name_Is_Invalid(string? name)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MergeProfile(
                name: name!,
                generalOptions: new GeneralMergeOptions(),
                csOptions: new CsMergeOptions()));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_GeneralOptions_Are_Null()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MergeProfile(
                name: "Profile 1",
                generalOptions: null!,
                csOptions: new CsMergeOptions()));

        Assert.Equal("generalOptions", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_CsOptions_Are_Null()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MergeProfile(
                name: "Profile 1",
                generalOptions: new GeneralMergeOptions(),
                csOptions: null!));

        Assert.Equal("csOptions", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Initialize_Empty_Collections_When_Null_Is_Passed()
    {
        var result = new MergeProfile(
            name: "Profile 1",
            generalOptions: new GeneralMergeOptions(),
            csOptions: new CsMergeOptions(),
            fileTypes: null,
            filterRules: null,
            transformations: null);

        Assert.NotNull(result.FileTypes);
        Assert.Empty(result.FileTypes);
        Assert.NotNull(result.FilterRules);
        Assert.Empty(result.FilterRules);
        Assert.NotNull(result.Transformations);
        Assert.Empty(result.Transformations);
    }
}