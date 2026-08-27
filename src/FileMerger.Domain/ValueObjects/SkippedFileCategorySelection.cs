using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects;

public sealed record SkippedFileCategorySelection(
    bool IncludeDisabledFileTypes,
    bool IncludeUnsupportedFiles,
    bool IncludeProfileExclusions,
    bool IncludeManualExclusions,
    bool IncludeSourceExclusions,
    bool IncludeProcessingFailures,
    bool IncludeOther)
{
    public bool Includes(SkippedFileCategory category)
    {
        return category switch
        {
            SkippedFileCategory.DisabledFileType => IncludeDisabledFileTypes,
            SkippedFileCategory.UnsupportedFile => IncludeUnsupportedFiles,
            SkippedFileCategory.ProfileExclusion => IncludeProfileExclusions,
            SkippedFileCategory.ManualExclusion => IncludeManualExclusions,
            SkippedFileCategory.SourceExclusion => IncludeSourceExclusions,
            SkippedFileCategory.ProcessingFailure => IncludeProcessingFailures,
            SkippedFileCategory.Other => IncludeOther,
            _ => false
        };
    }

    public static SkippedFileCategorySelection ForCurrentBehavior(
        bool includeSourceExcludedFiles)
    {
        return new SkippedFileCategorySelection(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: true,
            IncludeProfileExclusions: true,
            IncludeManualExclusions: true,
            IncludeSourceExclusions: includeSourceExcludedFiles,
            IncludeProcessingFailures: true,
            IncludeOther: true);
    }
}