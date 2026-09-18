using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FileMerger.Application.Updates;

public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    private readonly string[] _buildMetadataIdentifiers;
    private readonly string[] _prereleaseIdentifiers;

    private SemanticVersion(
        int major,
        int minor,
        int patch,
        string[] prereleaseIdentifiers,
        string[] buildMetadataIdentifiers)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        _prereleaseIdentifiers = prereleaseIdentifiers;
        _buildMetadataIdentifiers = buildMetadataIdentifiers;
    }

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public IReadOnlyList<string> PrereleaseIdentifiers => _prereleaseIdentifiers;
    public IReadOnlyList<string> BuildMetadataIdentifiers => _buildMetadataIdentifiers;
    public bool IsPrerelease => _prereleaseIdentifiers.Length > 0;

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
            return 1;

        int majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
            return majorComparison;

        int minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
            return minorComparison;

        int patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0)
            return patchComparison;

        if (_prereleaseIdentifiers.Length == 0 && other._prereleaseIdentifiers.Length == 0)
            return 0;

        if (_prereleaseIdentifiers.Length == 0)
            return 1;

        if (other._prereleaseIdentifiers.Length == 0)
            return -1;

        int sharedIdentifierCount = Math.Min(_prereleaseIdentifiers.Length, other._prereleaseIdentifiers.Length);
        for (int i = 0; i < sharedIdentifierCount; i++)
        {
            int identifierComparison = ComparePrereleaseIdentifier(
                _prereleaseIdentifiers[i],
                other._prereleaseIdentifiers[i]);

            if (identifierComparison != 0)
                return identifierComparison;
        }

        return _prereleaseIdentifiers.Length.CompareTo(other._prereleaseIdentifiers.Length);
    }

    public bool Equals(SemanticVersion? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return Major == other.Major &&
               Minor == other.Minor &&
               Patch == other.Patch &&
               _prereleaseIdentifiers.SequenceEqual(other._prereleaseIdentifiers, StringComparer.Ordinal) &&
               _buildMetadataIdentifiers.SequenceEqual(other._buildMetadataIdentifiers, StringComparer.Ordinal);
    }

    public static SemanticVersion Parse(string value)
    {
        if (!TryParse(value, out SemanticVersion? version))
            throw new FormatException($"'{value}' is not a valid semantic version.");

        return version;
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out SemanticVersion? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string candidate = value.Trim();

        int plusIndex = candidate.IndexOf('+');
        string versionWithoutBuildMetadata = plusIndex >= 0 ? candidate[..plusIndex] : candidate;
        string buildMetadata = plusIndex >= 0 ? candidate[(plusIndex + 1)..] : string.Empty;

        if (plusIndex >= 0 && candidate.IndexOf('+', plusIndex + 1) >= 0)
            return false;

        if (plusIndex >= 0 && buildMetadata.Length == 0)
            return false;

        int hyphenIndex = versionWithoutBuildMetadata.IndexOf('-');
        string coreVersion =
            hyphenIndex >= 0 ? versionWithoutBuildMetadata[..hyphenIndex] : versionWithoutBuildMetadata;
        string prerelease = hyphenIndex >= 0 ? versionWithoutBuildMetadata[(hyphenIndex + 1)..] : string.Empty;

        if (hyphenIndex >= 0 && prerelease.Length == 0)
            return false;

        string[] coreParts = coreVersion.Split('.');
        if (coreParts.Length != 3)
            return false;

        if (!TryParseCoreNumber(coreParts[0], out int major) ||
            !TryParseCoreNumber(coreParts[1], out int minor) ||
            !TryParseCoreNumber(coreParts[2], out int patch))
        {
            return false;
        }

        if (!TryParseIdentifiers(
                prerelease,
                allowLeadingZeroNumericIdentifiers: false,
                out string[] prereleaseIdentifiers))
            return false;

        if (!TryParseIdentifiers(
                buildMetadata,
                allowLeadingZeroNumericIdentifiers: true,
                out string[] buildMetadataIdentifiers))
            return false;

        version = new SemanticVersion(major, minor, patch, prereleaseIdentifiers, buildMetadataIdentifiers);

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is SemanticVersion other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Major);
        hash.Add(Minor);
        hash.Add(Patch);

        foreach (string identifier in _prereleaseIdentifiers)
            hash.Add(identifier, StringComparer.Ordinal);

        hash.Add("+");

        foreach (string identifier in _buildMetadataIdentifiers)
            hash.Add(identifier, StringComparer.Ordinal);

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(Major);
        builder.Append('.');
        builder.Append(Minor);
        builder.Append('.');
        builder.Append(Patch);

        if (_prereleaseIdentifiers.Length > 0)
        {
            builder.Append('-');
            builder.AppendJoin('.', _prereleaseIdentifiers);
        }

        if (_buildMetadataIdentifiers.Length > 0)
        {
            builder.Append('+');
            builder.AppendJoin('.', _buildMetadataIdentifiers);
        }

        return builder.ToString();
    }

    public static bool operator <(SemanticVersion left, SemanticVersion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(SemanticVersion left, SemanticVersion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(SemanticVersion left, SemanticVersion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(SemanticVersion left, SemanticVersion right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) >= 0;
    }

    private static bool TryParseCoreNumber(string value, out int number)
    {
        number = 0;

        if (value.Length == 0)
            return false;

        if (value.Length > 1 && value[0] == '0')
            return false;

        if (!value.All(char.IsAsciiDigit))
            return false;

        return int.TryParse(value, out number);
    }

    private static bool TryParseIdentifiers(
        string value,
        bool allowLeadingZeroNumericIdentifiers,
        out string[] identifiers)
    {
        identifiers = [];

        if (value.Length == 0)
            return true;

        string[] parts = value.Split('.');
        foreach (string part in parts)
        {
            if (part.Length == 0)
                return false;

            if (!part.All(IsIdentifierCharacter))
                return false;

            bool isNumeric = part.All(char.IsAsciiDigit);
            if (isNumeric && !allowLeadingZeroNumericIdentifiers && part.Length > 1 && part[0] == '0')
                return false;
        }

        identifiers = parts;
        return true;
    }

    private static bool IsIdentifierCharacter(char value)
    {
        return char.IsAsciiLetterOrDigit(value) || value == '-';
    }

    private static int ComparePrereleaseIdentifier(string left, string right)
    {
        bool leftIsNumeric = left.All(char.IsAsciiDigit);
        bool rightIsNumeric = right.All(char.IsAsciiDigit);

        if (leftIsNumeric && rightIsNumeric)
        {
            int lengthComparison = left.Length.CompareTo(right.Length);
            if (lengthComparison != 0)
                return lengthComparison;

            return string.Compare(left, right, StringComparison.Ordinal);
        }

        if (leftIsNumeric)
            return -1;

        if (rightIsNumeric)
            return 1;

        return string.Compare(left, right, StringComparison.Ordinal);
    }
}