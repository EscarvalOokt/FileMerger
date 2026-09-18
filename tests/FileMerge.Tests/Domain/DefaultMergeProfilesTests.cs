using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain;

public sealed class DefaultMergeProfilesTests
{
    [Fact]
    public void CreateDefault_Should_Return_Profile_With_Expected_Name()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.Equal("Default", result.Name);
    }

    [Fact]
    public void CreateDefault_Should_Return_Profile_With_Expected_General_Options()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.False(result.GeneralOptions.IncludeHeaderComment);
        Assert.True(result.GeneralOptions.IncludeFileSeparators);
        Assert.True(result.GeneralOptions.IncludeRelativePathInSeparator);
        Assert.True(result.GeneralOptions.TrimTrailingEmptyLines);
        Assert.Equal(LineEndingMode.Preserve, result.GeneralOptions.LineEndingMode);
        Assert.Equal(SortMode.ByRelativePathAscending, result.GeneralOptions.SortMode);
    }

    [Fact]
    public void CreateDefault_Should_Return_Default_Known_File_Types()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        string[] expectedExtensions = KnownFileTypes.Default.Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] actualExtensions = result.FileTypes.Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expectedExtensions, actualExtensions);
    }

    [Fact]
    public void CreateDefault_Should_Return_Profile_With_Expected_Filter_Rules()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.Equal(4, result.FilterRules.Count);

        Assert.Contains(
            result.FilterRules,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact,
                Pattern: "bin"
            });

        Assert.Contains(
            result.FilterRules,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact,
                Pattern: "obj"
            });

        Assert.Contains(
            result.FilterRules,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Wildcard,
                Pattern: "*.Designer.cs"
            });

        Assert.Contains(
            result.FilterRules,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Exact,
                Pattern: "AssemblyInfo.cs"
            });
    }

    [Fact]
    public void CreateDefault_Should_Return_Profile_With_Expected_Transformations()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.Equal(2, result.Transformations.Count);

        ContentTransformationRule[] ordered = result.Transformations.OrderBy(x => x.Order).ToArray();

        Assert.Equal(TransformationKind.TrimTrailingEmptyLines, ordered[0].Kind);
        Assert.Equal(1, ordered[0].Order);
        Assert.True(ordered[0].IsEnabled);

        Assert.Equal(TransformationKind.NormalizeLineEndings, ordered[1].Kind);
        Assert.Equal(2, ordered[1].Order);
        Assert.False(ordered[1].IsEnabled);
    }

    [Fact]
    public void CreateDefault_Should_Return_Default_CSharp_FilterRules()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.Contains(
            result.FilterRules,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact,
                Pattern: "bin"
            });

        Assert.Contains(
            result.FilterRules,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact,
                Pattern: "obj"
            });

        Assert.Contains(
            result.FilterRules,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Wildcard,
                Pattern: "*.Designer.cs"
            });

        Assert.Contains(
            result.FilterRules,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Exact,
                Pattern: "AssemblyInfo.cs"
            });
    }
}