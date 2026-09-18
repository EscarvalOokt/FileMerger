namespace FileMerger.Application.Updates;

public sealed record UpdateRelease
{
    public UpdateRelease(
        SemanticVersion version,
        DateTimeOffset publishedAtUtc,
        Uri? releaseNotesUrl,
        IReadOnlyCollection<UpdatePackage> packages)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(packages);

        Version = version;
        PublishedAtUtc = publishedAtUtc;
        ReleaseNotesUrl = releaseNotesUrl;
        Packages = packages;
    }

    public SemanticVersion Version { get; }
    public DateTimeOffset PublishedAtUtc { get; }
    public Uri? ReleaseNotesUrl { get; }
    public IReadOnlyCollection<UpdatePackage> Packages { get; }
}