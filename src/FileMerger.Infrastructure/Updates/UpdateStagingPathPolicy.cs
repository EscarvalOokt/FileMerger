namespace FileMerger.Infrastructure.Updates;

public sealed class UpdateStagingPathPolicy(string? localApplicationDataRoot = null)
{
    private const string AcknowledgementFileName = "restart-verification.json";
    private const string ApplicationDirectoryName = "FileMerger";
    private const string ArchiveFileName = "package.zip";
    private const string InstallDirectoryName = "install";
    private const string PartialArchiveFileName = "package.zip.partial";
    private const string PayloadDirectoryName = "payload";
    private const string ReceiptFileName = "install-receipt.json";
    private const string RequestFileName = "install-request.json";
    private const string RollbackDirectoryName = "rollback";
    private const string UpdaterDirectoryName = "updater";
    private const string UpdatesDirectoryName = "Updates";

    private readonly string _localApplicationDataRoot = string.IsNullOrWhiteSpace(localApplicationDataRoot)
        ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        : localApplicationDataRoot;

    public string GetUpdatesRootDirectory()
    {
        return Path.Combine(_localApplicationDataRoot, ApplicationDirectoryName, UpdatesDirectoryName);
    }

    public string CreateAttemptDirectoryPath()
    {
        return Path.Combine(GetUpdatesRootDirectory(), Guid.NewGuid().ToString("N"));
    }

    public string GetPartialArchivePath(string attemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptDirectory);
        return Path.Combine(attemptDirectory, PartialArchiveFileName);
    }

    public string GetArchivePath(string attemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptDirectory);
        return Path.Combine(attemptDirectory, ArchiveFileName);
    }

    public string GetPayloadDirectory(string attemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptDirectory);
        return Path.Combine(attemptDirectory, PayloadDirectoryName);
    }

    public string CreateInstallationAttemptDirectoryPath(string verifiedAttemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedAttemptDirectory);

        return Path.Combine(verifiedAttemptDirectory, InstallDirectoryName, Guid.NewGuid().ToString("N"));
    }

    public string GetUpdaterDirectory(string installationAttemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationAttemptDirectory);
        return Path.Combine(installationAttemptDirectory, UpdaterDirectoryName);
    }

    public string GetRollbackDirectory(string installationAttemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationAttemptDirectory);
        return Path.Combine(installationAttemptDirectory, RollbackDirectoryName);
    }

    public string GetInstallRequestPath(string installationAttemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationAttemptDirectory);
        return Path.Combine(installationAttemptDirectory, RequestFileName);
    }

    public string GetInstallReceiptPath(string installationAttemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationAttemptDirectory);
        return Path.Combine(installationAttemptDirectory, ReceiptFileName);
    }

    public string GetRestartVerificationPath(string installationAttemptDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationAttemptDirectory);
        return Path.Combine(installationAttemptDirectory, AcknowledgementFileName);
    }
}