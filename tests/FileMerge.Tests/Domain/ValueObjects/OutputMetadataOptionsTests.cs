using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class OutputMetadataOptionsTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        SkippedFileCategorySelection selection = new(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: false,
            IncludeProfileExclusions: true,
            IncludeManualExclusions: false,
            IncludeSourceExclusions: true,
            IncludeProcessingFailures: false,
            IncludeOther: true);

        OutputMetadataOptions result = new(
            IncludeBuildTimestamp: false,
            IncludeSessionName: false,
            IncludeOutputPath: false,
            IncludeFileSummary: false,
            SkippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
            IncludeSourceExcludedFiles: true,
            SkippedFileCategories: selection);

        Assert.False(result.IncludeBuildTimestamp);
        Assert.False(result.IncludeSessionName);
        Assert.False(result.IncludeOutputPath);
        Assert.False(result.IncludeFileSummary);
        Assert.Equal(SkippedFilesMetadataMode.Detailed, result.SkippedFilesMetadataMode);
        Assert.True(result.IncludeSourceExcludedFiles);
        Assert.Same(selection, result.SkippedFileCategories);
        Assert.Same(selection, result.EffectiveSkippedFileCategories);
    }

    [Fact]
    public void EffectiveSkippedFileCategories_Should_Use_Current_Behavior_When_Explicit_Selection_Is_Not_Set()
    {
        OutputMetadataOptions result = new(
            IncludeSourceExcludedFiles: false);

        SkippedFileCategorySelection selection = result.EffectiveSkippedFileCategories;

        Assert.True(selection.Includes(SkippedFileCategory.DisabledFileType));
        Assert.True(selection.Includes(SkippedFileCategory.UnsupportedFile));
        Assert.True(selection.Includes(SkippedFileCategory.ProfileExclusion));
        Assert.True(selection.Includes(SkippedFileCategory.ManualExclusion));
        Assert.False(selection.Includes(SkippedFileCategory.SourceExclusion));
        Assert.True(selection.Includes(SkippedFileCategory.ProcessingFailure));
        Assert.True(selection.Includes(SkippedFileCategory.Other));
    }

    [Fact]
    public void EffectiveSkippedFileCategories_Should_Include_Source_Exclusions_When_Legacy_Option_Is_Enabled()
    {
        OutputMetadataOptions result = new(
            IncludeSourceExcludedFiles: true);

        Assert.True(result.EffectiveSkippedFileCategories.Includes(
            SkippedFileCategory.SourceExclusion));
    }

    [Fact]
    public void EffectiveSkippedFileCategories_Should_Prefer_Explicit_Selection_Over_Legacy_Source_Option()
    {
        SkippedFileCategorySelection explicitSelection = new(
            IncludeDisabledFileTypes: false,
            IncludeUnsupportedFiles: false,
            IncludeProfileExclusions: false,
            IncludeManualExclusions: false,
            IncludeSourceExclusions: false,
            IncludeProcessingFailures: false,
            IncludeOther: false);

        OutputMetadataOptions result = new(
            IncludeSourceExcludedFiles: true,
            SkippedFileCategories: explicitSelection);

        Assert.Same(explicitSelection, result.EffectiveSkippedFileCategories);
        Assert.False(result.EffectiveSkippedFileCategories.Includes(
            SkippedFileCategory.SourceExclusion));
    }

    [Fact]
    public void Default_Should_Enable_Base_Header_Metadata()
    {
        OutputMetadataOptions result = OutputMetadataOptions.Default;

        Assert.True(result.IncludeBuildTimestamp);
        Assert.True(result.IncludeSessionName);
        Assert.True(result.IncludeOutputPath);
        Assert.True(result.IncludeFileSummary);
    }

    [Fact]
    public void Default_Should_Disable_Skipped_Files_Metadata()
    {
        OutputMetadataOptions result = OutputMetadataOptions.Default;

        Assert.Equal(SkippedFilesMetadataMode.None, result.SkippedFilesMetadataMode);
    }

    [Fact]
    public void Default_Should_Disable_Source_Excluded_Files_Metadata()
    {
        OutputMetadataOptions result = OutputMetadataOptions.Default;

        Assert.False(result.IncludeSourceExcludedFiles);
    }
}