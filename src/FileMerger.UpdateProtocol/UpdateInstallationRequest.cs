namespace FileMerger.UpdateProtocol;

public sealed record UpdateInstallationRequest
{
    public int SchemaVersion { get; init; } = UpdateProtocolConstants.SchemaVersion;
    public string AttemptId { get; init; } = string.Empty;
    public int MainProcessId { get; init; }
    public string InstallationDirectory { get; init; } = string.Empty;
    public string PayloadDirectory { get; init; } = string.Empty;
    public string EntryExecutable { get; init; } = string.Empty;
    public string ExpectedApplicationVersion { get; init; } = string.Empty;
    public string PackageId { get; init; } = string.Empty;
    public string RollbackDirectory { get; init; } = string.Empty;
    public string ReceiptPath { get; init; } = string.Empty;
    public string AcknowledgementPath { get; init; } = string.Empty;
    public string VerificationToken { get; init; } = string.Empty;
}