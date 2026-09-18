using FileMerger.Application.Updates;

namespace FileMerger.Application.UseCases.CheckForUpdates;

public enum UpdateCheckFailureCode
{
    None = 0,
    CurrentVersionInvalid,
    ReleaseSourceUnavailable,
    ManifestInvalid,
    AmbiguousCompatiblePackage
}

public sealed record CheckForUpdatesResult
{
    private CheckForUpdatesResult(
        UpdateCheckOutcome outcome,
        SemanticVersion? currentVersion,
        SemanticVersion? releaseVersion,
        UpdatePackage? selectedPackage,
        UpdateCheckFailureCode failureCode,
        string? failureMessage)
    {
        Outcome = outcome;
        CurrentVersion = currentVersion;
        ReleaseVersion = releaseVersion;
        SelectedPackage = selectedPackage;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    public UpdateCheckOutcome Outcome { get; }
    public SemanticVersion? CurrentVersion { get; }
    public SemanticVersion? ReleaseVersion { get; }
    public UpdatePackage? SelectedPackage { get; }
    public UpdateCheckFailureCode FailureCode { get; }
    public string? FailureMessage { get; }

    public static CheckForUpdatesResult UpdateAvailable(
        SemanticVersion currentVersion,
        SemanticVersion releaseVersion,
        UpdatePackage selectedPackage)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);
        ArgumentNullException.ThrowIfNull(releaseVersion);
        ArgumentNullException.ThrowIfNull(selectedPackage);

        return new CheckForUpdatesResult(
            UpdateCheckOutcome.UpdateAvailable,
            currentVersion,
            releaseVersion,
            selectedPackage,
            UpdateCheckFailureCode.None,
            failureMessage: null);
    }

    public static CheckForUpdatesResult NoUpdateAvailable(
        SemanticVersion currentVersion,
        SemanticVersion? releaseVersion)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);

        return new CheckForUpdatesResult(
            UpdateCheckOutcome.NoUpdateAvailable,
            currentVersion,
            releaseVersion,
            selectedPackage: null,
            UpdateCheckFailureCode.None,
            failureMessage: null);
    }

    public static CheckForUpdatesResult Failed(
        UpdateCheckFailureCode failureCode,
        string failureMessage,
        SemanticVersion? currentVersion = null,
        SemanticVersion? releaseVersion = null)
    {
        if (failureCode == UpdateCheckFailureCode.None)
            throw new ArgumentOutOfRangeException(nameof(failureCode), "A failed result requires a failure code.");

        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);

        return new CheckForUpdatesResult(
            UpdateCheckOutcome.Failed,
            currentVersion,
            releaseVersion,
            selectedPackage: null,
            failureCode,
            failureMessage);
    }
}