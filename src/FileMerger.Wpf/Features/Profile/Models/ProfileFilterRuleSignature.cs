using FileMerger.Domain.Enums;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Features.Profile.Models;

public sealed class ProfileFilterRuleSignature(
    FilterMode mode,
    FilterTarget target,
    RulePatternType patternType,
    string pattern,
    bool isEnabled,
    string? description,
    bool isUserEditable) : IEquatable<ProfileFilterRuleSignature>
{
    public FilterMode Mode { get; } = mode;
    public FilterTarget Target { get; } = target;
    public RulePatternType PatternType { get; } = patternType;
    public string Pattern { get; } = pattern ?? throw new ArgumentNullException(nameof(pattern));
    public bool IsEnabled { get; } = isEnabled;
    public string? Description { get; } = description;
    public bool IsUserEditable { get; } = isUserEditable;

    public bool Equals(ProfileFilterRuleSignature? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return Mode == other.Mode &&
               Target == other.Target &&
               PatternType == other.PatternType &&
               IsEnabled == other.IsEnabled &&
               IsUserEditable == other.IsUserEditable &&
               string.Equals(Pattern, other.Pattern, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Description, other.Description, StringComparison.Ordinal);
    }

    public static ProfileFilterRuleSignature From(WorkspaceFileFilterRuleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ProfileFilterRuleSignature(
            dto.Mode,
            dto.Target,
            dto.PatternType,
            dto.Pattern,
            dto.IsEnabled,
            dto.Description,
            dto.IsUserEditable);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ProfileFilterRuleSignature);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Mode);
        hash.Add(Target);
        hash.Add(PatternType);
        hash.Add(Pattern, StringComparer.OrdinalIgnoreCase);
        hash.Add(IsEnabled);
        hash.Add(Description, StringComparer.Ordinal);
        hash.Add(IsUserEditable);

        return hash.ToHashCode();
    }
}