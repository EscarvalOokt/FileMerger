namespace FileMerger.Wpf.Diagnostics;

public sealed record CrashLogCleanupResult(
    bool IsSuccessful,
    int DeletedCount,
    IReadOnlyCollection<string> DeletedPaths,
    IReadOnlyCollection<CrashLogMaintenanceFailure> Failures)
{
    public static CrashLogCleanupResult From(
        IReadOnlyCollection<string> deletedPaths,
        IReadOnlyCollection<CrashLogMaintenanceFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(deletedPaths);
        ArgumentNullException.ThrowIfNull(failures);

        return new CrashLogCleanupResult(
            IsSuccessful: failures.Count == 0,
            DeletedCount: deletedPaths.Count,
            DeletedPaths: deletedPaths,
            Failures: failures);
    }
}