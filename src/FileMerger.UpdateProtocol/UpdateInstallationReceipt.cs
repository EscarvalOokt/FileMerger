namespace FileMerger.UpdateProtocol;

public sealed record UpdateInstallationReceipt
{
    public int SchemaVersion { get; init; } = UpdateProtocolConstants.SchemaVersion;
    public string AttemptId { get; init; } = string.Empty;

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string PackageId { get; init; } = string.Empty;
    public string ExpectedApplicationVersion { get; init; } = string.Empty;

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string InstallationDirectory { get; init; } = string.Empty;

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string RollbackDirectory { get; init; } = string.Empty;
    public string AcknowledgementPath { get; init; } = string.Empty;
    public string VerificationToken { get; init; } = string.Empty;
    public UpdateInstallationStatus Status { get; init; } = UpdateInstallationStatus.Pending;
    public string? Message { get; init; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;
}