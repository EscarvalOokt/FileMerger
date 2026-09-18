using FileMerger.Domain.Enums;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Features.Profile.Models;

public sealed class ProfileSignature(
    bool includeHeaderComment,
    bool includeFileSeparators,
    bool includeRelativePathInSeparator,
    bool trimTrailingEmptyLines,
    LineEndingMode lineEndingMode,
    SortMode sortMode,
    InputEncodingMode inputEncodingMode,
    string? preferredInputEncodingName,
    string? fallbackInputEncodingName,
    IReadOnlyCollection<ProfileFileTypeSignature> fileTypes,
    IReadOnlyCollection<ProfileFilterRuleSignature> filterRules) : IEquatable<ProfileSignature>
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

    public IReadOnlyCollection<ProfileFileTypeSignature> FileTypes { get; } =
        fileTypes ?? throw new ArgumentNullException(nameof(fileTypes));

    public IReadOnlyCollection<ProfileFilterRuleSignature> FilterRules { get; } =
        filterRules ?? throw new ArgumentNullException(nameof(filterRules));

    public bool Equals(ProfileSignature? other)
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
               FileTypes.SequenceEqual(other.FileTypes) &&
               FilterRules.SequenceEqual(other.FilterRules);
    }

    public static ProfileSignature From(WorkspaceProfileDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        ProfileFileTypeSignature[] fileTypes =
        [
            .. dto.FileTypes.OrderBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.DisplayName, StringComparer.Ordinal)
                .Select(ProfileFileTypeSignature.From)
        ];

        ProfileFilterRuleSignature[] filterRules =
        [
            .. (dto.FilterRules ?? []).Where(x => !string.IsNullOrWhiteSpace(x.Pattern))
            .OrderBy(x => x.Mode)
            .ThenBy(x => x.Target)
            .ThenBy(x => x.PatternType)
            .ThenBy(x => x.Pattern, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Description, StringComparer.Ordinal)
            .ThenBy(x => x.IsEnabled)
            .ThenBy(x => x.IsUserEditable)
            .Select(ProfileFilterRuleSignature.From)
        ];

        return new ProfileSignature(
            dto.IncludeHeaderComment,
            dto.IncludeFileSeparators,
            dto.IncludeRelativePathInSeparator,
            dto.TrimTrailingEmptyLines,
            dto.LineEndingMode,
            dto.SortMode,
            dto.InputEncodingMode,
            dto.PreferredInputEncodingName,
            dto.FallbackInputEncodingName,
            fileTypes,
            filterRules);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ProfileSignature);
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
        hash.Add(PreferredInputEncodingName, StringComparer.Ordinal);
        hash.Add(FallbackInputEncodingName, StringComparer.Ordinal);

        foreach (ProfileFileTypeSignature fileType in FileTypes)
            hash.Add(fileType);

        foreach (ProfileFilterRuleSignature rule in FilterRules)
            hash.Add(rule);

        return hash.ToHashCode();
    }
}