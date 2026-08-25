using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects;

public sealed record OutputMetadataOptions(
    bool IncludeBuildTimestamp = true,
    bool IncludeSessionName = true,
    bool IncludeOutputPath = true,
    bool IncludeFileSummary = true,
    SkippedFilesMetadataMode SkippedFilesMetadataMode = SkippedFilesMetadataMode.None,
    bool IncludeSourceExcludedFiles = false)
{
    public static OutputMetadataOptions Default { get; } = new();
}