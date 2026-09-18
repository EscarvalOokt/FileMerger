namespace FileMerger.Wpf.Shell.Windows;

public interface IApplicationWindowCloseGuardService
{
    Task<bool> TryPrepareAsync(CancellationToken cancellationToken = default);

    void CommitPreparedShutdown();

    void ResetPreparedShutdown();
}