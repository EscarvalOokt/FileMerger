using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects;

public sealed record GeneralMergeOptions
{
    public GeneralMergeOptions(
        bool includeHeaderComment = false,
        bool includeFileSeparators = true,
        bool includeRelativePathInSeparator = true,
        bool trimTrailingEmptyLines = true,
        LineEndingMode lineEndingMode = LineEndingMode.Preserve,
        SortMode sortMode = SortMode.ByRelativePathAscending,
        InputEncodingMode inputEncodingMode = InputEncodingMode.Auto,
        string? preferredInputEncodingName = null,
        string? fallbackInputEncodingName = null,
        UnsupportedTextFallbackOptions? unsupportedTextFallbackOptions = null,
        OutputMetadataOptions? outputMetadataOptions = null)
    {
        IncludeHeaderComment = includeHeaderComment;
        IncludeFileSeparators = includeFileSeparators;
        IncludeRelativePathInSeparator = includeRelativePathInSeparator;
        TrimTrailingEmptyLines = trimTrailingEmptyLines;
        LineEndingMode = lineEndingMode;
        SortMode = sortMode;
        InputEncodingMode = inputEncodingMode;
        PreferredInputEncodingName = preferredInputEncodingName;
        FallbackInputEncodingName = fallbackInputEncodingName;
        UnsupportedTextFallbackOptions = unsupportedTextFallbackOptions ?? UnsupportedTextFallbackOptions.Disabled;
        OutputMetadataOptions = outputMetadataOptions ?? OutputMetadataOptions.Default;
    }

    public bool IncludeHeaderComment { get; }
    public bool IncludeFileSeparators { get; }
    public bool IncludeRelativePathInSeparator { get; }
    public bool TrimTrailingEmptyLines { get; }
    public LineEndingMode LineEndingMode { get; }
    public SortMode SortMode { get; }

    public InputEncodingMode InputEncodingMode { get; }
    public string? PreferredInputEncodingName { get; }
    public string? FallbackInputEncodingName { get; }

    public UnsupportedTextFallbackOptions UnsupportedTextFallbackOptions { get; }
    public OutputMetadataOptions OutputMetadataOptions { get; }
}