using FileMerger.Domain.Enums;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Profile.Models;

public sealed class ProfileSignatureTests
{
    [Fact]
    public void From_Should_Differ_When_FilterRules_Differ()
    {
        WorkspaceProfileDto first = CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library")
            ]);

        WorkspaceProfileDto second = CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Temp")
            ]);

        Assert.NotEqual(ProfileSignature.From(first), ProfileSignature.From(second));
    }

    [Fact]
    public void From_Should_Treat_Same_FilterRules_As_Equal()
    {
        WorkspaceProfileDto first = CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library")
            ]);

        WorkspaceProfileDto second = CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library")
            ]);

        Assert.Equal(ProfileSignature.From(first), ProfileSignature.From(second));
    }

    private static WorkspaceProfileDto CreateProfile(List<WorkspaceFileFilterRuleDto>? filterRules = null)
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: false,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            FileTypes:
            [
                new WorkspaceFileTypeDto(
                    Extension: ".cs",
                    DisplayName: "C# source",
                    Kind: FileKind.CSharp,
                    IsEnabled: true,
                    SupportsLanguageSpecificProcessing: true)
            ],
            LineEndingMode: LineEndingMode.Preserve,
            SortMode: SortMode.ByRelativePathAscending,
            InputEncodingMode: InputEncodingMode.Auto,
            PreferredInputEncodingName: null,
            FallbackInputEncodingName: "windows-1251",
            FilterRules: filterRules);
    }

    private static WorkspaceFileFilterRuleDto ExcludeDirectoryRule(string pattern)
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: pattern,
            IsEnabled: true,
            Description: $"Exclude {pattern}",
            IsUserEditable: true);
    }
}