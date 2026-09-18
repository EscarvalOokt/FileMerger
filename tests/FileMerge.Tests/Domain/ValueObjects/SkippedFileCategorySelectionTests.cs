using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class SkippedFileCategorySelectionTests
{
    [Fact]
    public void Includes_Should_Return_Configured_Value_For_Each_Category()
    {
        SkippedFileCategorySelection selection = new(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: false,
            IncludeProfileExclusions: true,
            IncludeManualExclusions: false,
            IncludeSourceExclusions: true,
            IncludeProcessingFailures: false,
            IncludeOther: true);

        Assert.True(selection.Includes(SkippedFileCategory.DisabledFileType));
        Assert.False(selection.Includes(SkippedFileCategory.UnsupportedFile));
        Assert.True(selection.Includes(SkippedFileCategory.ProfileExclusion));
        Assert.False(selection.Includes(SkippedFileCategory.ManualExclusion));
        Assert.True(selection.Includes(SkippedFileCategory.SourceExclusion));
        Assert.False(selection.Includes(SkippedFileCategory.ProcessingFailure));
        Assert.True(selection.Includes(SkippedFileCategory.Other));
    }

    [Fact]
    public void ForCurrentBehavior_Should_Enable_Normal_Categories_And_Disable_Source_Exclusions_When_Requested()
    {
        var selection = SkippedFileCategorySelection.ForCurrentBehavior(includeSourceExcludedFiles: false);

        Assert.True(selection.Includes(SkippedFileCategory.DisabledFileType));
        Assert.True(selection.Includes(SkippedFileCategory.UnsupportedFile));
        Assert.True(selection.Includes(SkippedFileCategory.ProfileExclusion));
        Assert.True(selection.Includes(SkippedFileCategory.ManualExclusion));
        Assert.False(selection.Includes(SkippedFileCategory.SourceExclusion));
        Assert.True(selection.Includes(SkippedFileCategory.ProcessingFailure));
        Assert.True(selection.Includes(SkippedFileCategory.Other));
    }

    [Fact]
    public void ForCurrentBehavior_Should_Enable_Source_Exclusions_When_Requested()
    {
        var selection = SkippedFileCategorySelection.ForCurrentBehavior(includeSourceExcludedFiles: true);

        Assert.True(selection.Includes(SkippedFileCategory.SourceExclusion));
    }
}