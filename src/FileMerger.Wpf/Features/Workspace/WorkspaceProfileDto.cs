using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Wpf.Features.Workspace;

public sealed record WorkspaceProfileDto(
    bool IncludeHeaderComment,
    bool IncludeFileSeparators,
    bool IncludeRelativePathInSeparator,
    bool TrimTrailingEmptyLines,
    bool RemoveUsingDirectives,
    List<WorkspaceFileTypeDto> FileTypes,
    LineEndingMode LineEndingMode,
    SortMode SortMode,
    InputEncodingMode InputEncodingMode,
    string? PreferredInputEncodingName,
    string? FallbackInputEncodingName,
    List<WorkspaceFileFilterRuleDto>? FilterRules = null,
    bool IncludeUnsupportedTextFiles = false,
    long UnsupportedTextMaxFileSizeBytes = UnsupportedTextFallbackOptions.DefaultMaxFileSizeBytes,
    int UnsupportedTextProbeSizeBytes = UnsupportedTextFallbackOptions.DefaultProbeSizeBytes,
    double UnsupportedTextMaxControlCharacterRatio = UnsupportedTextFallbackOptions.DefaultMaxControlCharacterRatio,
    bool IncludeBuildTimestampMetadata = true,
    bool IncludeSessionNameMetadata = true,
    bool IncludeOutputPathMetadata = true,
    bool IncludeFileSummaryMetadata = true,
    SkippedFilesMetadataMode SkippedFilesMetadataMode = SkippedFilesMetadataMode.None,
    bool IncludeSourceExcludedFiles = false,
    SkippedFileCategorySelection? SkippedFileCategories = null);