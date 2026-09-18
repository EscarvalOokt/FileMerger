using FileMerger.Application.Updates;

namespace FileMerger.Application.UseCases.LaunchUpdateInstaller;

public enum LaunchUpdateInstallerOutcome
{
    Started = 0,
    Failed
}

public sealed record LaunchUpdateInstallerResult
{
    private LaunchUpdateInstallerResult(
        LaunchUpdateInstallerOutcome outcome,
        UpdateInstallationFailureCode failureCode,
        string? failureMessage)
    {
        Outcome = outcome;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    public LaunchUpdateInstallerOutcome Outcome { get; }
    public UpdateInstallationFailureCode FailureCode { get; }
    public string? FailureMessage { get; }

    public static LaunchUpdateInstallerResult Started()
    {
        return new LaunchUpdateInstallerResult(
            LaunchUpdateInstallerOutcome.Started,
            UpdateInstallationFailureCode.None,
            failureMessage: null);
    }

    public static LaunchUpdateInstallerResult Failed(UpdateInstallationFailureCode failureCode, string failureMessage)
    {
        if (failureCode == UpdateInstallationFailureCode.None)
            throw new ArgumentOutOfRangeException(nameof(failureCode), "A failed result requires a failure code.");

        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        return new LaunchUpdateInstallerResult(LaunchUpdateInstallerOutcome.Failed, failureCode, failureMessage);
    }
}