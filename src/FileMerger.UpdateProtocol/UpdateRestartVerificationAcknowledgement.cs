namespace FileMerger.UpdateProtocol;

public sealed record UpdateRestartVerificationAcknowledgement
{
    public int SchemaVersion { get; init; } = UpdateProtocolConstants.SchemaVersion;
    public string AttemptId { get; init; } = string.Empty;
    public string VerificationToken { get; init; } = string.Empty;
    public string ActualApplicationVersion { get; init; } = string.Empty;
    public bool ExpectedVersionMatches { get; init; }
    public string? FailureMessage { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}