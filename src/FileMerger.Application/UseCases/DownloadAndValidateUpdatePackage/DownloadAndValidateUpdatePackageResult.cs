using FileMerger.Application.Updates;

namespace FileMerger.Application.UseCases.DownloadAndValidateUpdatePackage;

public enum DownloadAndValidateUpdatePackageOutcome
{
    Verified = 0,
    Failed
}

public sealed record DownloadAndValidateUpdatePackageResult
{
    private DownloadAndValidateUpdatePackageResult(
        DownloadAndValidateUpdatePackageOutcome outcome,
        VerifiedUpdatePackage? verifiedPackage,
        UpdatePackagePreparationFailureCode failureCode,
        string? failureMessage)
    {
        Outcome = outcome;
        VerifiedPackage = verifiedPackage;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    public DownloadAndValidateUpdatePackageOutcome Outcome { get; }
    public VerifiedUpdatePackage? VerifiedPackage { get; }
    public UpdatePackagePreparationFailureCode FailureCode { get; }
    public string? FailureMessage { get; }

    public static DownloadAndValidateUpdatePackageResult Verified(VerifiedUpdatePackage verifiedPackage)
    {
        ArgumentNullException.ThrowIfNull(verifiedPackage);

        return new DownloadAndValidateUpdatePackageResult(
            DownloadAndValidateUpdatePackageOutcome.Verified,
            verifiedPackage,
            UpdatePackagePreparationFailureCode.None,
            failureMessage: null);
    }

    public static DownloadAndValidateUpdatePackageResult Failed(
        UpdatePackagePreparationFailureCode failureCode,
        string failureMessage)
    {
        if (failureCode == UpdatePackagePreparationFailureCode.None)
            throw new ArgumentOutOfRangeException(nameof(failureCode), "A failed result requires a failure code.");

        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        return new DownloadAndValidateUpdatePackageResult(
            DownloadAndValidateUpdatePackageOutcome.Failed,
            verifiedPackage: null,
            failureCode,
            failureMessage);
    }
}