using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects;

public sealed record OutputMetadataOptions(
    bool IncludeBuildTimestamp = true,
    bool IncludeSessionName = true,
    bool IncludeOutputPath = true,
    bool IncludeFileSummary = true,
    SkippedFilesMetadataMode SkippedFilesMetadataMode = SkippedFilesMetadataMode.None,
    bool IncludeSourceExcludedFiles = false,
    SkippedFileCategorySelection? SkippedFileCategories = null)
{
    public static OutputMetadataOptions Default { get; } = new();

    public SkippedFileCategorySelection EffectiveSkippedFileCategories =>
        SkippedFileCategories ??
        SkippedFileCategorySelection.ForCurrentBehavior(IncludeSourceExcludedFiles);
}