namespace FileMerger.Application.Updates;

public enum UpdatePackagePreparationFailureCode
{
    None = 0,
    CandidateInvalid,
    UntrustedOrigin,
    DownloadUnavailable,
    DownloadIncomplete,
    SizeMismatch,
    HashMismatch,
    InvalidArchive,
    UnsafeArchiveEntry,
    RequiredPayloadMissing,
    StagingFailed
}

public sealed class UpdatePackagePreparationException : Exception
{
    public UpdatePackagePreparationException(
        UpdatePackagePreparationFailureCode failureCode,
        string message,
        Exception? innerException = null) : base(message, innerException)
    {
        if (failureCode == UpdatePackagePreparationFailureCode.None)
            throw new ArgumentOutOfRangeException(
                nameof(failureCode),
                "A package preparation failure requires a code.");

        FailureCode = failureCode;
    }

    public UpdatePackagePreparationFailureCode FailureCode { get; }
}