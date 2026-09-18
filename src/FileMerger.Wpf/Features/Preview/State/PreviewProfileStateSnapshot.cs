using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Wpf.Features.Preview.State;

public sealed class PreviewProfileStateSnapshot(
    bool includeHeaderComment,
    bool includeFileSeparators,
    bool includeRelativePathInSeparator,
    bool trimTrailingEmptyLines,
    LineEndingMode lineEndingMode,
    SortMode sortMode,
    InputEncodingMode inputEncodingMode,
    string? preferredInputEncodingName,
    string? fallbackInputEncodingName,
    IReadOnlyCollection<PreviewFileTypeStateSnapshot> fileTypes,
    IReadOnlyCollection<PreviewFileFilterRuleStateSnapshot> filterRules,
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
    SkippedFileCategorySelection? skippedFileCategories = null) : IEquatable<PreviewProfileStateSnapshot>
{
    public bool IncludeHeaderComment { get; } = includeHeaderComment;
    public bool IncludeFileSeparators { get; } = includeFileSeparators;
    public bool IncludeRelativePathInSeparator { get; } = includeRelativePathInSeparator;
    public bool TrimTrailingEmptyLines { get; } = trimTrailingEmptyLines;
    public LineEndingMode LineEndingMode { get; } = lineEndingMode;
    public SortMode SortMode { get; } = sortMode;
    public InputEncodingMode InputEncodingMode { get; } = inputEncodingMode;
    public string? PreferredInputEncodingName { get; } = preferredInputEncodingName;
    public string? FallbackInputEncodingName { get; } = fallbackInputEncodingName;

    public IReadOnlyCollection<PreviewFileTypeStateSnapshot> FileTypes { get; } =
        fileTypes ?? throw new ArgumentNullException(nameof(fileTypes));

    public IReadOnlyCollection<PreviewFileFilterRuleStateSnapshot> FilterRules { get; } =
        filterRules ?? throw new ArgumentNullException(nameof(filterRules));

    public bool IncludeUnsupportedTextFiles { get; } = includeUnsupportedTextFiles;
    public long UnsupportedTextMaxFileSizeBytes { get; } = unsupportedTextMaxFileSizeBytes;
    public int UnsupportedTextProbeSizeBytes { get; } = unsupportedTextProbeSizeBytes;
    public double UnsupportedTextMaxControlCharacterRatio { get; } = unsupportedTextMaxControlCharacterRatio;

    public bool IncludeBuildTimestampMetadata { get; } = includeBuildTimestampMetadata;
    public bool IncludeSessionNameMetadata { get; } = includeSessionNameMetadata;
    public bool IncludeOutputPathMetadata { get; } = includeOutputPathMetadata;
    public bool IncludeFileSummaryMetadata { get; } = includeFileSummaryMetadata;
    public SkippedFilesMetadataMode SkippedFilesMetadataMode { get; } = skippedFilesMetadataMode;
    public bool IncludeSourceExcludedFiles { get; } = includeSourceExcludedFiles;

    public SkippedFileCategorySelection SkippedFileCategories { get; } = skippedFileCategories ??
                                                                         SkippedFileCategorySelection
                                                                             .ForCurrentBehavior(
                                                                                 includeSourceExcludedFiles);

    public bool Equals(PreviewProfileStateSnapshot? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return IncludeHeaderComment == other.IncludeHeaderComment &&
               IncludeFileSeparators == other.IncludeFileSeparators &&
               IncludeRelativePathInSeparator == other.IncludeRelativePathInSeparator &&
               TrimTrailingEmptyLines == other.TrimTrailingEmptyLines &&
               LineEndingMode == other.LineEndingMode &&
               SortMode == other.SortMode &&
               InputEncodingMode == other.InputEncodingMode &&
               string.Equals(PreferredInputEncodingName, other.PreferredInputEncodingName, StringComparison.Ordinal) &&
               string.Equals(FallbackInputEncodingName, other.FallbackInputEncodingName, StringComparison.Ordinal) &&
               IncludeUnsupportedTextFiles == other.IncludeUnsupportedTextFiles &&
               UnsupportedTextMaxFileSizeBytes == other.UnsupportedTextMaxFileSizeBytes &&
               UnsupportedTextProbeSizeBytes == other.UnsupportedTextProbeSizeBytes &&
               UnsupportedTextMaxControlCharacterRatio.Equals(other.UnsupportedTextMaxControlCharacterRatio) &&
               IncludeBuildTimestampMetadata == other.IncludeBuildTimestampMetadata &&
               IncludeSessionNameMetadata == other.IncludeSessionNameMetadata &&
               IncludeOutputPathMetadata == other.IncludeOutputPathMetadata &&
               IncludeFileSummaryMetadata == other.IncludeFileSummaryMetadata &&
               SkippedFilesMetadataMode == other.SkippedFilesMetadataMode &&
               IncludeSourceExcludedFiles == other.IncludeSourceExcludedFiles &&
               SkippedFileCategories == other.SkippedFileCategories &&
               FileTypeSnapshotsEqual(FileTypes, other.FileTypes) &&
               FilterRuleSnapshotsEqual(FilterRules, other.FilterRules);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PreviewProfileStateSnapshot);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(IncludeHeaderComment);
        hash.Add(IncludeFileSeparators);
        hash.Add(IncludeRelativePathInSeparator);
        hash.Add(TrimTrailingEmptyLines);
        hash.Add(LineEndingMode);
        hash.Add(SortMode);
        hash.Add(InputEncodingMode);
        hash.Add(PreferredInputEncodingName);
        hash.Add(FallbackInputEncodingName);
        hash.Add(IncludeUnsupportedTextFiles);
        hash.Add(UnsupportedTextMaxFileSizeBytes);
        hash.Add(UnsupportedTextProbeSizeBytes);
        hash.Add(UnsupportedTextMaxControlCharacterRatio);
        hash.Add(IncludeBuildTimestampMetadata);
        hash.Add(IncludeSessionNameMetadata);
        hash.Add(IncludeOutputPathMetadata);
        hash.Add(IncludeFileSummaryMetadata);
        hash.Add(SkippedFilesMetadataMode);
        hash.Add(IncludeSourceExcludedFiles);
        hash.Add(SkippedFileCategories);

        foreach (PreviewFileTypeStateSnapshot fileType in FileTypes)
            hash.Add(fileType);

        foreach (PreviewFileFilterRuleStateSnapshot filterRule in FilterRules)
            hash.Add(filterRule);

        return hash.ToHashCode();
    }

    private static bool FileTypeSnapshotsEqual(
        IReadOnlyCollection<PreviewFileTypeStateSnapshot> left,
        IReadOnlyCollection<PreviewFileTypeStateSnapshot> right)
    {
        if (left.Count != right.Count)
            return false;

        using IEnumerator<PreviewFileTypeStateSnapshot> leftEnumerator = left.GetEnumerator();
        using IEnumerator<PreviewFileTypeStateSnapshot> rightEnumerator = right.GetEnumerator();

        while (leftEnumerator.MoveNext())
        {
            if (!rightEnumerator.MoveNext())
                return false;

            PreviewFileTypeStateSnapshot leftFileType = leftEnumerator.Current;
            PreviewFileTypeStateSnapshot rightFileType = rightEnumerator.Current;

            if (!string.Equals(leftFileType.Extension, rightFileType.Extension, StringComparison.OrdinalIgnoreCase) ||
                leftFileType.IsEnabled != rightFileType.IsEnabled)
            {
                return false;
            }
        }

        return !rightEnumerator.MoveNext();
    }

    private static bool FilterRuleSnapshotsEqual(
        IReadOnlyCollection<PreviewFileFilterRuleStateSnapshot> left,
        IReadOnlyCollection<PreviewFileFilterRuleStateSnapshot> right)
    {
        if (left.Count != right.Count)
            return false;

        using IEnumerator<PreviewFileFilterRuleStateSnapshot> leftEnumerator = left.GetEnumerator();
        using IEnumerator<PreviewFileFilterRuleStateSnapshot> rightEnumerator = right.GetEnumerator();

        while (leftEnumerator.MoveNext())
        {
            if (!rightEnumerator.MoveNext())
                return false;

            PreviewFileFilterRuleStateSnapshot leftRule = leftEnumerator.Current;
            PreviewFileFilterRuleStateSnapshot rightRule = rightEnumerator.Current;

            if (leftRule.Mode != rightRule.Mode ||
                leftRule.Target != rightRule.Target ||
                leftRule.PatternType != rightRule.PatternType ||
                leftRule.IsEnabled != rightRule.IsEnabled ||
                !string.Equals(leftRule.Pattern, rightRule.Pattern, StringComparison.Ordinal) ||
                !string.Equals(leftRule.Description, rightRule.Description, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return !rightEnumerator.MoveNext();
    }
}