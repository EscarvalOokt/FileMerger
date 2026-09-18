namespace FileMerger.Application.Updates;

public sealed record DownloadedUpdatePackage
{
    public DownloadedUpdatePackage(UpdatePackage package, string attemptDirectory, string archivePath)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

        Package = package;
        AttemptDirectory = attemptDirectory;
        ArchivePath = archivePath;
    }

    public UpdatePackage Package { get; }
    public string AttemptDirectory { get; }
    public string ArchivePath { get; }
}