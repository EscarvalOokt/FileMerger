using FileMerger.Wpf.Shared.ViewModels;
using WpfApplication = System.Windows.Application;

namespace FileMerger.Wpf.Shell.Windows;

public sealed class ApplicationWindowCloseGuardService : IApplicationWindowCloseGuardService
{
    private readonly List<GuardedWindow> _preparedWindows = [];

    public async Task<bool> TryPrepareAsync(CancellationToken cancellationToken = default)
    {
        ResetPreparedShutdown();

        WpfApplication? application = WpfApplication.Current;
        if (application is null)
            return true;

        GuardedWindow[] guardedWindows =
        [
            .. application.Windows.OfType<GuardedWindow>().Where(window => window.DataContext is IAsyncCloseGuard)
        ];

        foreach (GuardedWindow window in guardedWindows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (window.DataContext is not IAsyncCloseGuard closeGuard)
                continue;

            bool canClose;
            try
            {
                canClose = await closeGuard.CanCloseAsync();
            }
            catch
            {
                canClose = false;
            }

            if (!canClose)
            {
                ResetPreparedShutdown();
                return false;
            }

            _preparedWindows.Add(window);
        }

        return true;
    }

    public void CommitPreparedShutdown()
    {
        foreach (GuardedWindow window in _preparedWindows)
            window.ApproveClose();

        _preparedWindows.Clear();
    }

    public void ResetPreparedShutdown()
    {
        _preparedWindows.Clear();
    }
}