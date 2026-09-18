namespace FileMerger.Application.Updates;

public sealed record UpdateReleaseManifest
{
    public UpdateReleaseManifest(int schemaVersion, string product, UpdateRelease release)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(release);

        SchemaVersion = schemaVersion;
        Product = product;
        Release = release;
    }

    public int SchemaVersion { get; }
    public string Product { get; }
    public UpdateRelease Release { get; }
}