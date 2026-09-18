namespace FileMerger.Application.Updates;

public enum UpdatePackagePreparationStage
{
    Downloading = 0,
    ValidatingSize,
    ValidatingHash,
    InspectingArchive,
    Extracting,
    Completed
}

public sealed record UpdatePackagePreparationProgress(
    UpdatePackagePreparationStage Stage,
    long Current,
    long Total,
    string Message)
{
    public bool IsDeterminate => Total > 0;
}