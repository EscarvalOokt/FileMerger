namespace FileMerger.Application.Updates;

public sealed record UpdatePackage
{
    public UpdatePackage(
        string id,
        SemanticVersion version,
        string os,
        string architecture,
        string framework,
        string deployment,
        string format,
        Uri url,
        long sizeBytes,
        string sha256,
        string entryExecutable,
        SemanticVersion? minimumSourceVersion,
        SemanticVersion? minimumUpdaterVersion)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(os);
        ArgumentNullException.ThrowIfNull(architecture);
        ArgumentNullException.ThrowIfNull(framework);
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(sha256);
        ArgumentNullException.ThrowIfNull(entryExecutable);

        Id = id;
        Version = version;
        Os = os;
        Architecture = architecture;
        Framework = framework;
        Deployment = deployment;
        Format = format;
        Url = url;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        EntryExecutable = entryExecutable;
        MinimumSourceVersion = minimumSourceVersion;
        MinimumUpdaterVersion = minimumUpdaterVersion;
    }

    public string Id { get; }
    public SemanticVersion Version { get; }
    public string Os { get; }
    public string Architecture { get; }
    public string Framework { get; }
    public string Deployment { get; }
    public string Format { get; }
    public Uri Url { get; }
    public long SizeBytes { get; }
    public string Sha256 { get; }
    public string EntryExecutable { get; }
    public SemanticVersion? MinimumSourceVersion { get; }
    public SemanticVersion? MinimumUpdaterVersion { get; }
}