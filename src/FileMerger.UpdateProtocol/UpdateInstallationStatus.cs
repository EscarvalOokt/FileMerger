namespace FileMerger.UpdateProtocol;

public enum UpdateInstallationStatus
{
    Pending = 0,
    Installing,
    PendingVerification,
    Installed,
    Failed,
    RolledBack
}