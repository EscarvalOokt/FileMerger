namespace FileMerger.Infrastructure.Updates;

public sealed class ApplicationInstallationPathProvider(string? installationDirectory = null)
{
    private readonly string _installationDirectory = string.IsNullOrWhiteSpace(installationDirectory)
        ? AppContext.BaseDirectory
        : installationDirectory;

    public string GetInstallationDirectory()
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(_installationDirectory));
    }
}