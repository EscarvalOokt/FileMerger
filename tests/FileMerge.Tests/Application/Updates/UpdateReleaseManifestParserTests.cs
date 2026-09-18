using System.IO;
using FileMerger.Application.Updates;

namespace FileMerger.Tests.Application.Updates;

public sealed class UpdateReleaseManifestParserTests
{
    private readonly UpdateReleaseManifestParser _parser = new();

    [Fact]
    public void Parse_Should_Read_Valid_Schema1_Manifest()
    {
        UpdateReleaseManifest manifest = _parser.Parse(CreateValidManifest());

        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal("FileMerger", manifest.Product);
        Assert.Equal("0.2.0-alpha.2", manifest.Release.Version.ToString());
        Assert.Equal(DateTimeOffset.Parse("2026-09-19T00:00:00Z"), manifest.Release.PublishedAtUtc);
        Assert.Equal(new Uri("https://updates.example.test/releases/0.2.0-alpha.2"), manifest.Release.ReleaseNotesUrl);

        UpdatePackage package = Assert.Single(manifest.Release.Packages);
        Assert.Equal("windows-any", package.Id);
        Assert.Equal("0.2.0-alpha.2", package.Version.ToString());
        Assert.Equal("windows", package.Os);
        Assert.Equal("any", package.Architecture);
        Assert.Equal("net10.0-windows", package.Framework);
        Assert.Equal("frameworkDependent", package.Deployment);
        Assert.Equal("zip", package.Format);
        Assert.Equal(new Uri("https://updates.example.test/packages/filemerger-0.2.0-alpha.2.zip"), package.Url);
        Assert.Equal(123456L, package.SizeBytes);
        Assert.Equal(new string('a', 64), package.Sha256);
        Assert.Equal("FileMerger.Wpf.exe", package.EntryExecutable);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("2")]
    public void Parse_Should_Reject_Unsupported_Schema(string schemaVersion)
    {
        string json = CreateValidManifest().Replace("\"schemaVersion\": 1", $"\"schemaVersion\": {schemaVersion}");

        InvalidDataException ex = Assert.Throws<InvalidDataException>(() => _parser.Parse(json));

        Assert.Contains("schema version", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Should_Reject_Wrong_Product()
    {
        string json = CreateValidManifest().Replace("\"product\": \"FileMerger\"", "\"product\": \"OtherProduct\"");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_Malformed_Json()
    {
        Assert.Throws<InvalidDataException>(() => _parser.Parse("{ not-json"));
    }

    [Fact]
    public void Parse_Should_Reject_Missing_Packages()
    {
        string json = CreateValidManifest().Replace("\"packages\": [", "\"packages\": null, \"ignoredPackages\": [");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_Invalid_Release_Version()
    {
        string json = CreateValidManifest().Replace("0.2.0-alpha.2", "not-a-version");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_Package_Version_That_Differs_From_Release()
    {
        string json = CreateValidManifest()
            .Replace(
                "\"version\": \"0.2.0-alpha.2\",\n        \"os\"",
                "\"version\": \"0.2.0-alpha.3\",\n        \"os\"");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_NonHttps_Package_Url()
    {
        string json = CreateValidManifest()
            .Replace(
                "https://updates.example.test/packages/filemerger-0.2.0-alpha.2.zip",
                "http://updates.example.test/packages/filemerger-0.2.0-alpha.2.zip");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_NonUtc_PublishedAt()
    {
        string json = CreateValidManifest().Replace("2026-09-19T00:00:00Z", "2026-09-19T03:00:00+03:00");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_PublishedAt_Without_Explicit_Utc_Offset()
    {
        string json = CreateValidManifest().Replace("2026-09-19T00:00:00Z", "2026-09-19T00:00:00");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_Invalid_Sha256()
    {
        string json = CreateValidManifest().Replace(new string('a', 64), "abc123");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_Invalid_MinimumSourceVersion()
    {
        string json = CreateValidManifest()
            .Replace(
                "\"entryExecutable\": \"FileMerger.Wpf.exe\"",
                "\"entryExecutable\": \"FileMerger.Wpf.exe\",\n        \"minimumSourceVersion\": \"invalid\"");

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Parse_Should_Reject_Duplicate_Package_Ids()
    {
        string package = ExtractPackage(CreateValidManifest());
        string json = CreateValidManifest().Replace(package, package + ",\n" + package);

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    [Theory]
    [InlineData("../FileMerger.Wpf.exe")]
    [InlineData("C:/FileMerger/FileMerger.Wpf.exe")]
    [InlineData("/FileMerger.Wpf.exe")]
    public void Parse_Should_Reject_Unsafe_EntryExecutable(string entryExecutable)
    {
        string json = CreateValidManifest().Replace("FileMerger.Wpf.exe", entryExecutable);

        Assert.Throws<InvalidDataException>(() => _parser.Parse(json));
    }

    private static string ExtractPackage(string json)
    {
        int start = json.IndexOf("      {", StringComparison.Ordinal);
        int end = json.IndexOf("      }", start, StringComparison.Ordinal) + "      }".Length;
        return json[start..end];
    }

    private static string CreateValidManifest()
    {
        return $$"""
                 {
                   "schemaVersion": 1,
                   "product": "FileMerger",
                   "release": {
                     "version": "0.2.0-alpha.2",
                     "publishedAtUtc": "2026-09-19T00:00:00Z",
                     "releaseNotesUrl": "https://updates.example.test/releases/0.2.0-alpha.2",
                     "packages": [
                       {
                         "id": "windows-any",
                         "version": "0.2.0-alpha.2",
                         "os": "windows",
                         "architecture": "any",
                         "framework": "net10.0-windows",
                         "deployment": "frameworkDependent",
                         "format": "zip",
                         "url": "https://updates.example.test/packages/filemerger-0.2.0-alpha.2.zip",
                         "sizeBytes": 123456,
                         "sha256": "{{new string('a', 64)}}",
                         "entryExecutable": "FileMerger.Wpf.exe"
                       }
                     ]
                   }
                 }
                 """;
    }
}