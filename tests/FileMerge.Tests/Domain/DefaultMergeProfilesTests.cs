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
    public void CreateDefault_Should_Return_Profile_With_Expected_Cs_Options()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.False(result.CsOptions.RemoveUsingDirectives);
    }

    [Fact]
    public void CreateDefault_Should_Return_Default_Known_File_Types()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        string[] expectedExtensions = KnownFileTypes.Default
            .Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] actualExtensions = result.FileTypes
            .Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expectedExtensions, actualExtensions);
    }

    [Fact]
    public void CreateDefault_Should_Return_Profile_With_Expected_Filter_Rules()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.Equal(4, result.FilterRules.Count);

        Assert.Contains(result.FilterRules, x =>
            x.Mode == FilterMode.Exclude &&
            x.Target == FilterTarget.DirectorySegment &&
            x.PatternType == RulePatternType.Exact &&
            x.Pattern == "bin");

        Assert.Contains(result.FilterRules, x =>
            x.Mode == FilterMode.Exclude &&
            x.Target == FilterTarget.DirectorySegment &&
            x.PatternType == RulePatternType.Exact &&
            x.Pattern == "obj");

        Assert.Contains(result.FilterRules, x =>
            x.Mode == FilterMode.Exclude &&
            x.Target == FilterTarget.FileName &&
            x.PatternType == RulePatternType.Wildcard &&
            x.Pattern == "*.Designer.cs");

        Assert.Contains(result.FilterRules, x =>
            x.Mode == FilterMode.Exclude &&
            x.Target == FilterTarget.FileName &&
            x.PatternType == RulePatternType.Exact &&
            x.Pattern == "AssemblyInfo.cs");
    }

    [Fact]
    public void CreateDefault_Should_Return_Profile_With_Expected_Transformations()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.Equal(3, result.Transformations.Count);

        ContentTransformationRule[] ordered = result.Transformations.OrderBy(x => x.Order).ToArray();

        Assert.Equal(TransformationKind.RemoveUsingDirectives, ordered[0].Kind);
        Assert.Equal(0, ordered[0].Order);
        Assert.False(ordered[0].IsEnabled);
        Assert.Contains(FileKind.CSharp, ordered[0].AppliesTo);

        Assert.Equal(TransformationKind.TrimTrailingEmptyLines, ordered[1].Kind);
        Assert.Equal(1, ordered[1].Order);
        Assert.True(ordered[1].IsEnabled);

        Assert.Equal(TransformationKind.NormalizeLineEndings, ordered[2].Kind);
        Assert.Equal(2, ordered[2].Order);
        Assert.False(ordered[2].IsEnabled);
    }

    [Fact]
    public void CreateDefault_Should_Return_Default_CSharp_FilterRules()
    {
        MergeProfile result = DefaultMergeProfiles.CreateDefault();

        Assert.Contains(result.FilterRules, x =>
            x.Mode == FilterMode.Exclude &&
            x.Target == FilterTarget.DirectorySegment &&
            x.PatternType == RulePatternType.Exact &&
            x.Pattern == "bin");

        Assert.Contains(result.FilterRules, x =>
            x.Mode == FilterMode.Exclude &&
            x.Target == FilterTarget.DirectorySegment &&
            x.PatternType == RulePatternType.Exact &&
            x.Pattern == "obj");

        Assert.Contains(result.FilterRules, x =>
            x.Mode == FilterMode.Exclude &&
            x.Target == FilterTarget.FileName &&
            x.PatternType == RulePatternType.Wildcard &&
            x.Pattern == "*.Designer.cs");

        Assert.Contains(result.FilterRules, x =>
            x.Mode == FilterMode.Exclude &&
            x.Target == FilterTarget.FileName &&
            x.PatternType == RulePatternType.Exact &&
            x.Pattern == "AssemblyInfo.cs");
    }
}