namespace FileMerger.Application.Updates;

public sealed record VerifiedUpdatePackage
{
    public VerifiedUpdatePackage(
        UpdatePackage package,
        string attemptDirectory,
        string archivePath,
        string payloadDirectory,
        string entryExecutablePath)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryExecutablePath);

        Package = package;
        AttemptDirectory = attemptDirectory;
        ArchivePath = archivePath;
        PayloadDirectory = payloadDirectory;
        EntryExecutablePath = entryExecutablePath;
    }

    public UpdatePackage Package { get; }

    public string AttemptDirectory { get; }
    public string ArchivePath { get; }
    public string PayloadDirectory { get; }
    public string EntryExecutablePath { get; }
}