using FileMerger.Application.Updates;

namespace FileMerger.Wpf.Features.Updates.Services;

public enum UpdateInstallationCoordinationOutcome
{
    ReadyToShutdown = 0,
    ActiveOperation,
    GuardCanceled,
    PreflightFailed,
    HelperLaunchFailed
}

public sealed record UpdateInstallationCoordinationResult(
    UpdateInstallationCoordinationOutcome Outcome,
    string Message,
    UpdateInstallationFailureCode FailureCode = UpdateInstallationFailureCode.None)
{
    public static UpdateInstallationCoordinationResult ReadyToShutdown(string message) =>
        new(UpdateInstallationCoordinationOutcome.ReadyToShutdown, message);

    public static UpdateInstallationCoordinationResult ActiveOperation(string message) =>
        new(UpdateInstallationCoordinationOutcome.ActiveOperation, message);

    public static UpdateInstallationCoordinationResult GuardCanceled(string message) =>
        new(UpdateInstallationCoordinationOutcome.GuardCanceled, message);

    public static UpdateInstallationCoordinationResult PreflightFailed(
        string message,
        UpdateInstallationFailureCode failureCode = UpdateInstallationFailureCode.None) =>
        new(UpdateInstallationCoordinationOutcome.PreflightFailed, message, failureCode);

    public static UpdateInstallationCoordinationResult HelperLaunchFailed(
        string message,
        UpdateInstallationFailureCode failureCode = UpdateInstallationFailureCode.HelperLaunchFailed) =>
        new(UpdateInstallationCoordinationOutcome.HelperLaunchFailed, message, failureCode);
}

public interface IUpdateInstallationCoordinator
{
    Task<UpdateInstallationCoordinationResult> PrepareAndLaunchAsync(
        VerifiedUpdatePackage verifiedPackage,
        IProgress<UpdatePackagePreparationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    void CommitShutdown();
}