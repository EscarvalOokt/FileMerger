using FileMerger.Application.Updates;

namespace FileMerger.Application.UseCases.PrepareUpdateInstallation;

public enum PrepareUpdateInstallationOutcome
{
    Ready = 0,
    Failed
}

public sealed record PrepareUpdateInstallationResult
{
    private PrepareUpdateInstallationResult(
        PrepareUpdateInstallationOutcome outcome,
        VerifiedUpdatePackage? verifiedPackage,
        UpdateInstallationFailureCode failureCode,
        string? failureMessage)
    {
        Outcome = outcome;
        VerifiedPackage = verifiedPackage;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    public PrepareUpdateInstallationOutcome Outcome { get; }
    public VerifiedUpdatePackage? VerifiedPackage { get; }
    public UpdateInstallationFailureCode FailureCode { get; }
    public string? FailureMessage { get; }

    public static PrepareUpdateInstallationResult Ready(VerifiedUpdatePackage verifiedPackage)
    {
        ArgumentNullException.ThrowIfNull(verifiedPackage);

        return new PrepareUpdateInstallationResult(
            PrepareUpdateInstallationOutcome.Ready,
            verifiedPackage,
            UpdateInstallationFailureCode.None,
            failureMessage: null);
    }

    public static PrepareUpdateInstallationResult Failed(
        UpdateInstallationFailureCode failureCode,
        string failureMessage)
    {
        if (failureCode == UpdateInstallationFailureCode.None)
            throw new ArgumentOutOfRangeException(nameof(failureCode), "A failed result requires a failure code.");

        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        return new PrepareUpdateInstallationResult(
            PrepareUpdateInstallationOutcome.Failed,
            verifiedPackage: null,
            failureCode,
            failureMessage);
    }
}