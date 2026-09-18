namespace FileMerger.Application.Updates;

public sealed class UpdateInstallationException : Exception
{
    public UpdateInstallationException(
        UpdateInstallationFailureCode failureCode,
        string message,
        Exception? innerException = null) : base(message, innerException)
    {
        if (failureCode == UpdateInstallationFailureCode.None)
            throw new ArgumentOutOfRangeException(nameof(failureCode), "An installation failure requires a code.");

        FailureCode = failureCode;
    }

    public UpdateInstallationFailureCode FailureCode { get; }
}