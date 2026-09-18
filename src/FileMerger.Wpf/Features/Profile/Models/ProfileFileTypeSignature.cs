using FileMerger.Domain.Enums;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Features.Profile.Models;

public sealed class ProfileFileTypeSignature(
    string extension,
    string displayName,
    FileKind kind,
    bool isEnabled,
    bool supportsLanguageSpecificProcessing) : IEquatable<ProfileFileTypeSignature>
{
    public string Extension { get; } = extension ?? throw new ArgumentNullException(nameof(extension));
    public string DisplayName { get; } = displayName ?? throw new ArgumentNullException(nameof(displayName));
    public FileKind Kind { get; } = kind;
    public bool IsEnabled { get; } = isEnabled;
    public bool SupportsLanguageSpecificProcessing { get; } = supportsLanguageSpecificProcessing;

    public bool Equals(ProfileFileTypeSignature? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return string.Equals(Extension, other.Extension, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal) &&
               Kind == other.Kind &&
               IsEnabled == other.IsEnabled &&
               SupportsLanguageSpecificProcessing == other.SupportsLanguageSpecificProcessing;
    }

    public static ProfileFileTypeSignature From(WorkspaceFileTypeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ProfileFileTypeSignature(
            dto.Extension,
            dto.DisplayName,
            dto.Kind,
            dto.IsEnabled,
            dto.SupportsLanguageSpecificProcessing);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ProfileFileTypeSignature);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Extension, StringComparer.OrdinalIgnoreCase);
        hash.Add(DisplayName, StringComparer.Ordinal);
        hash.Add(Kind);
        hash.Add(IsEnabled);
        hash.Add(SupportsLanguageSpecificProcessing);

        return hash.ToHashCode();
    }
}