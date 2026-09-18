namespace FileMerger.Updater;

public sealed class UpdateInstallationFileTransaction
{
    public Task CreateBackupAsync(
        string installationDirectory,
        string rollbackDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(rollbackDirectory);

        cancellationToken.ThrowIfCancellationRequested();

        if (Directory.Exists(rollbackDirectory))
            throw new IOException($"Rollback directory '{rollbackDirectory}' already exists.");

        Directory.CreateDirectory(rollbackDirectory);
        CopyDirectoryContents(installationDirectory, rollbackDirectory, cancellationToken);
        return Task.CompletedTask;
    }

    public Task ReplaceInstallationAsync(
        string installationDirectory,
        string payloadDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadDirectory);

        cancellationToken.ThrowIfCancellationRequested();

        ClearDirectoryContents(installationDirectory, cancellationToken);
        CopyDirectoryContents(payloadDirectory, installationDirectory, cancellationToken);
        return Task.CompletedTask;
    }

    public Task RestoreBackupAsync(
        string installationDirectory,
        string rollbackDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(rollbackDirectory);

        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(rollbackDirectory))
            throw new DirectoryNotFoundException($"Rollback directory '{rollbackDirectory}' does not exist.");

        ClearDirectoryContents(installationDirectory, cancellationToken);
        CopyDirectoryContents(rollbackDirectory, installationDirectory, cancellationToken);
        return Task.CompletedTask;
    }

    private static void CopyDirectoryContents(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }

        foreach (string sourceSubdirectory in Directory.EnumerateDirectories(
                     sourceDirectory,
                     "*",
                     SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string destinationSubdirectory = Path.Combine(destinationDirectory, Path.GetFileName(sourceSubdirectory));
            CopyDirectoryContents(sourceSubdirectory, destinationSubdirectory, cancellationToken);
        }
    }

    private static void ClearDirectoryContents(string directory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);

        foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string subdirectory in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Delete(subdirectory, recursive: true);
        }
    }
}