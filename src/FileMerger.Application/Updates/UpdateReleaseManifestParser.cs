using System.Globalization;
using System.Text.Json;

namespace FileMerger.Application.Updates;

public sealed class UpdateReleaseManifestParser
{
    public const int SupportedSchemaVersion = 1;
    public const string SupportedProduct = "FileMerger";

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false
    };

    public UpdateReleaseManifest Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("The update manifest is empty.");

        ManifestDto dto;
        try
        {
            dto = JsonSerializer.Deserialize<ManifestDto>(json, _serializerOptions) ??
                  throw new InvalidDataException("The update manifest is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("The update manifest is not valid JSON.", ex);
        }

        if (dto.SchemaVersion != SupportedSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported update manifest schema version '{dto.SchemaVersion}'.");
        }

        if (!string.Equals(dto.Product, SupportedProduct, StringComparison.Ordinal))
            throw new InvalidDataException("The update manifest product does not match FileMerger.");

        if (dto.Release is null)
            throw new InvalidDataException("The update manifest does not contain a release descriptor.");

        SemanticVersion releaseVersion = ParseRequiredVersion(dto.Release.Version, "release.version");
        DateTimeOffset publishedAtUtc = ParseUtcTimestamp(dto.Release.PublishedAtUtc);
        Uri? releaseNotesUrl = ParseOptionalHttpsUri(dto.Release.ReleaseNotesUrl, "release.releaseNotesUrl");

        if (dto.Release.Packages is null || dto.Release.Packages.Length == 0)
            throw new InvalidDataException("The update manifest release does not contain any packages.");

        HashSet<string> packageIds = new(StringComparer.Ordinal);
        List<UpdatePackage> packages = [];

        foreach (PackageDto? packageDto in dto.Release.Packages)
        {
            if (packageDto is null)
                throw new InvalidDataException("The update manifest contains a null package descriptor.");

            string id = RequireNonEmpty(packageDto.Id, "package.id");
            if (!packageIds.Add(id))
                throw new InvalidDataException($"The update manifest contains duplicate package id '{id}'.");

            SemanticVersion packageVersion = ParseRequiredVersion(packageDto.Version, $"package '{id}'.version");
            if (!packageVersion.Equals(releaseVersion))
            {
                throw new InvalidDataException(
                    $"Package '{id}' version '{packageVersion}' does not match release version '{releaseVersion}'.");
            }

            string os = RequireNonEmpty(packageDto.Os, $"package '{id}'.os");
            string architecture = RequireNonEmpty(packageDto.Architecture, $"package '{id}'.architecture");
            string framework = RequireNonEmpty(packageDto.Framework, $"package '{id}'.framework");
            string deployment = RequireNonEmpty(packageDto.Deployment, $"package '{id}'.deployment");
            string format = RequireNonEmpty(packageDto.Format, $"package '{id}'.format");
            Uri url = ParseRequiredHttpsUri(packageDto.Url, $"package '{id}'.url");

            if (packageDto.SizeBytes <= 0)
                throw new InvalidDataException($"Package '{id}' sizeBytes must be greater than zero.");

            string sha256 = ParseSha256(packageDto.Sha256, id);
            string entryExecutable = ParseRelativeEntryExecutable(packageDto.EntryExecutable, id);
            SemanticVersion? minimumSourceVersion = ParseOptionalVersion(
                packageDto.MinimumSourceVersion,
                $"package '{id}'.minimumSourceVersion");
            SemanticVersion? minimumUpdaterVersion = ParseOptionalVersion(
                packageDto.MinimumUpdaterVersion,
                $"package '{id}'.minimumUpdaterVersion");

            packages.Add(
                new UpdatePackage(
                    id,
                    packageVersion,
                    os,
                    architecture,
                    framework,
                    deployment,
                    format,
                    url,
                    packageDto.SizeBytes,
                    sha256,
                    entryExecutable,
                    minimumSourceVersion,
                    minimumUpdaterVersion));
        }

        return new UpdateReleaseManifest(
            dto.SchemaVersion,
            dto.Product!,
            new UpdateRelease(releaseVersion, publishedAtUtc, releaseNotesUrl, packages));
    }

    private static SemanticVersion ParseRequiredVersion(string? value, string fieldName)
    {
        string normalized = RequireNonEmpty(value, fieldName);

        if (!SemanticVersion.TryParse(normalized, out SemanticVersion? version))
            throw new InvalidDataException($"'{fieldName}' is not a valid semantic version.");

        return version;
    }

    private static SemanticVersion? ParseOptionalVersion(string? value, string fieldName)
    {
        if (value is null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"'{fieldName}' cannot be empty when it is present.");

        if (!SemanticVersion.TryParse(value.Trim(), out SemanticVersion? version))
            throw new InvalidDataException($"'{fieldName}' is not a valid semantic version.");

        return version;
    }

    private static DateTimeOffset ParseUtcTimestamp(string? value)
    {
        string normalized = RequireNonEmpty(value, "release.publishedAtUtc");

        bool hasExplicitUtcOffset = normalized.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ||
                                    normalized.EndsWith("+00:00", StringComparison.Ordinal);

        if (!hasExplicitUtcOffset ||
            !DateTimeOffset.TryParse(
                normalized,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset timestamp) ||
            timestamp.Offset != TimeSpan.Zero)
        {
            throw new InvalidDataException("'release.publishedAtUtc' must be an ISO-8601 UTC timestamp.");
        }

        return timestamp;
    }

    private static Uri ParseRequiredHttpsUri(string? value, string fieldName)
    {
        string normalized = RequireNonEmpty(value, fieldName);

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"'{fieldName}' must be an absolute HTTPS URI.");
        }

        return uri;
    }

    private static Uri? ParseOptionalHttpsUri(string? value, string fieldName)
    {
        if (value is null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"'{fieldName}' cannot be empty when it is present.");

        return ParseRequiredHttpsUri(value, fieldName);
    }

    private static string ParseSha256(string? value, string packageId)
    {
        string normalized = RequireNonEmpty(value, $"package '{packageId}'.sha256");

        if (normalized.Length != 64 || !normalized.All(char.IsAsciiHexDigit))
        {
            throw new InvalidDataException(
                $"Package '{packageId}' sha256 must contain exactly 64 hexadecimal characters.");
        }

        return normalized.ToLowerInvariant();
    }

    private static string ParseRelativeEntryExecutable(string? value, string packageId)
    {
        string normalized = RequireNonEmpty(value, $"package '{packageId}'.entryExecutable").Replace('\\', '/');

        if (normalized.StartsWith('/') || normalized.Contains(':'))
        {
            throw new InvalidDataException(
                $"Package '{packageId}' entryExecutable must be a relative application path.");
        }

        string[] segments = normalized.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new InvalidDataException(
                $"Package '{packageId}' entryExecutable must be a safe relative application path.");
        }

        return string.Join('/', segments);
    }

    private static string RequireNonEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"'{fieldName}' is required.");

        return value.Trim();
    }

    // System.Text.Json instantiates and initializes these private DTOs through reflection.
    // ReSharper disable ClassNeverInstantiated.Local
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    private sealed class ManifestDto
    {
        public int SchemaVersion { get; init; }
        public string? Product { get; init; }
        public ReleaseDto? Release { get; init; }
    }

    private sealed class ReleaseDto
    {
        public string? Version { get; init; }
        public string? PublishedAtUtc { get; init; }
        public string? ReleaseNotesUrl { get; init; }
        public PackageDto?[]? Packages { get; init; }
    }

    private sealed class PackageDto
    {
        public string? Id { get; init; }
        public string? Version { get; init; }
        public string? Os { get; init; }
        public string? Architecture { get; init; }
        public string? Framework { get; init; }
        public string? Deployment { get; init; }
        public string? Format { get; init; }
        public string? Url { get; init; }
        public long SizeBytes { get; init; }
        public string? Sha256 { get; init; }
        public string? EntryExecutable { get; init; }
        public string? MinimumSourceVersion { get; init; }
        public string? MinimumUpdaterVersion { get; init; }
    }
    // ReSharper restore UnusedAutoPropertyAccessor.Local
    // ReSharper restore ClassNeverInstantiated.Local
}