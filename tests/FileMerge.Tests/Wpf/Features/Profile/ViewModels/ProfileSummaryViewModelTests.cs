using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Profile.ViewModels;

public sealed class ProfileSummaryViewModelTests
{
    [Fact]
    public void Apply_Should_Build_FileTypes_Headline()
    {
        var summary = new ProfileSummaryViewModel();

        summary.Apply(CreateProfile(
            fileTypes:
            [
                FileType(".cs", true, supportsLanguageSpecificProcessing: true),
                FileType(".xaml", true),
                FileType(".json", false)
            ]));

        Assert.Equal("2 enabled file types", summary.FileTypesHeadline);
        Assert.Equal(".cs, .xaml", summary.FileTypes);
    }

    [Fact]
    public void Apply_Should_Build_FilterRules_Headline()
    {
        var summary = new ProfileSummaryViewModel();

        summary.Apply(CreateProfile(
            filterRules:
            [
                FilterRule("bin", isEnabled: true),
                FilterRule("obj", isEnabled: false),
                FilterRule("Library", isEnabled: true)
            ]));

        Assert.Equal("2 enabled / 3 total", summary.FilterRulesHeadline);
        Assert.Equal("1 disabled rule will be ignored.", summary.FilterRules);
    }

    [Fact]
    public void Apply_Should_Build_OutputMetadata_Summary()
    {
        var summary = new ProfileSummaryViewModel();

        summary.Apply(CreateProfile(
            includeBuildTimestampMetadata: true,
            includeSessionNameMetadata: false,
            includeOutputPathMetadata: true,
            includeFileSummaryMetadata: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed));

        Assert.Equal(
            "Build timestamp · Output path · File summary · Skipped files: Detailed · " +
            "Skipped categories: Disabled file types, Unsupported files, Profile exclusions, " +
            "Manual exclusions, Processing failures, Other",
            summary.OutputMetadata);
    }

    [Fact]
    public void Apply_Should_Include_Legacy_Source_Exclusions_In_Output_Metadata_Category_Summary()
    {
        var summary = new ProfileSummaryViewModel();

        summary.Apply(CreateProfile(
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
            includeSourceExcludedFiles: true));

        Assert.Contains("Skipped files: Detailed", summary.OutputMetadata);
        Assert.Contains("Source exclusions", summary.OutputMetadata);
        Assert.DoesNotContain("Source-excluded files", summary.OutputMetadata);
    }

    [Fact]
    public void Apply_Should_Build_Output_Metadata_Summary_From_Explicit_Skipped_Category_Selection()
    {
        var summary = new ProfileSummaryViewModel();
        SkippedFileCategorySelection selection = new(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: false,
            IncludeProfileExclusions: true,
            IncludeManualExclusions: false,
            IncludeSourceExclusions: true,
            IncludeProcessingFailures: false,
            IncludeOther: true);

        summary.Apply(CreateProfile(
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
            includeSourceExcludedFiles: false,
            skippedFileCategories: selection));

        Assert.Contains("Skipped files: Simple", summary.OutputMetadata);
        Assert.Contains(
            "Skipped categories: Disabled file types, Profile exclusions, Source exclusions, Other",
            summary.OutputMetadata);
        Assert.DoesNotContain("Unsupported files", summary.OutputMetadata);
        Assert.DoesNotContain("Manual exclusions", summary.OutputMetadata);
        Assert.DoesNotContain("Processing failures", summary.OutputMetadata);
    }

    [Fact]
    public void Apply_Should_Show_None_When_Explicit_Skipped_Category_Selection_Is_Empty()
    {
        var summary = new ProfileSummaryViewModel();
        SkippedFileCategorySelection selection = new(
            IncludeDisabledFileTypes: false,
            IncludeUnsupportedFiles: false,
            IncludeProfileExclusions: false,
            IncludeManualExclusions: false,
            IncludeSourceExclusions: false,
            IncludeProcessingFailures: false,
            IncludeOther: false);

        summary.Apply(CreateProfile(
            skippedFileCategories: selection));

        Assert.Contains("Skipped files: None", summary.OutputMetadata);
        Assert.Contains("Skipped categories: none", summary.OutputMetadata);
    }

    [Fact]
    public void Apply_Should_Build_Disabled_UnsupportedTextFallback_Summary()
    {
        var summary = new ProfileSummaryViewModel();

        summary.Apply(CreateProfile(includeUnsupportedTextFiles: false));

        Assert.Equal("Disabled", summary.UnsupportedTextFallback);
    }

    [Fact]
    public void Apply_Should_Build_Enabled_UnsupportedTextFallback_Summary()
    {
        var summary = new ProfileSummaryViewModel();

        summary.Apply(CreateProfile(
            includeUnsupportedTextFiles: true,
            unsupportedTextMaxFileSizeBytes: 262_144,
            unsupportedTextProbeSizeBytes: 4_096,
            unsupportedTextMaxControlCharacterRatio: 0.1));

        Assert.Contains("Enabled", summary.UnsupportedTextFallback);
        Assert.Contains("Max size: 262144 bytes", summary.UnsupportedTextFallback);
        Assert.Contains("Probe: 4096 bytes", summary.UnsupportedTextFallback);
        Assert.Contains("Max control chars:", summary.UnsupportedTextFallback);
    }

    [Fact]
    public void Apply_Should_Build_FilterRules_Summary_When_All_Rules_Are_Enabled()
    {
        var summary = new ProfileSummaryViewModel();

        summary.Apply(CreateProfile(
            filterRules:
            [
                FilterRule("bin", isEnabled: true),
                FilterRule("obj", isEnabled: true)
            ]));

        Assert.Equal("2 enabled / 2 total", summary.FilterRulesHeadline);
        Assert.Equal("All configured rules are enabled.", summary.FilterRules);
    }

    private static WorkspaceProfileDto CreateProfile(
        List<WorkspaceFileTypeDto>? fileTypes = null,
        List<WorkspaceFileFilterRuleDto>? filterRules = null,
        bool includeUnsupportedTextFiles = false,
        long unsupportedTextMaxFileSizeBytes = UnsupportedTextFallbackOptions.DefaultMaxFileSizeBytes,
        int unsupportedTextProbeSizeBytes = UnsupportedTextFallbackOptions.DefaultProbeSizeBytes,
        double unsupportedTextMaxControlCharacterRatio = UnsupportedTextFallbackOptions.DefaultMaxControlCharacterRatio,
        bool includeBuildTimestampMetadata = true,
        bool includeSessionNameMetadata = true,
        bool includeOutputPathMetadata = true,
        bool includeFileSummaryMetadata = true,
        SkippedFilesMetadataMode skippedFilesMetadataMode = SkippedFilesMetadataMode.None,
        bool includeSourceExcludedFiles = false,
        SkippedFileCategorySelection? skippedFileCategories = null)
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: true,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            RemoveUsingDirectives: false,
            FileTypes: fileTypes ?? [],
            LineEndingMode: LineEndingMode.Preserve,
            SortMode: SortMode.ByRelativePathAscending,
            InputEncodingMode: InputEncodingMode.Auto,
            PreferredInputEncodingName: null,
            FallbackInputEncodingName: "windows-1251",
            FilterRules: filterRules,
            IncludeUnsupportedTextFiles: includeUnsupportedTextFiles,
            UnsupportedTextMaxFileSizeBytes: unsupportedTextMaxFileSizeBytes,
            UnsupportedTextProbeSizeBytes: unsupportedTextProbeSizeBytes,
            UnsupportedTextMaxControlCharacterRatio: unsupportedTextMaxControlCharacterRatio,
            IncludeBuildTimestampMetadata: includeBuildTimestampMetadata,
            IncludeSessionNameMetadata: includeSessionNameMetadata,
            IncludeOutputPathMetadata: includeOutputPathMetadata,
            IncludeFileSummaryMetadata: includeFileSummaryMetadata,
            SkippedFilesMetadataMode: skippedFilesMetadataMode,
            IncludeSourceExcludedFiles: includeSourceExcludedFiles,
            SkippedFileCategories: skippedFileCategories);
    }

    private static WorkspaceFileTypeDto FileType(
        string extension,
        bool isEnabled,
        bool supportsLanguageSpecificProcessing = false)
    {
        return new WorkspaceFileTypeDto(
            Extension: extension,
            DisplayName: extension,
            Kind: FileKind.Text,
            IsEnabled: isEnabled,
            SupportsLanguageSpecificProcessing: supportsLanguageSpecificProcessing);
    }

    private static WorkspaceFileFilterRuleDto FilterRule(
        string pattern,
        bool isEnabled)
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: pattern,
            IsEnabled: isEnabled,
            Description: null,
            IsUserEditable: true);
    }
}