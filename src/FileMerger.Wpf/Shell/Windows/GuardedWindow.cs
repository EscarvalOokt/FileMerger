using System.ComponentModel;
using System.Windows;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Shell.Windows;

public class GuardedWindow : Window
{
    private bool _closeConfirmed;
    private bool _isCloseGuardRunning;

    protected GuardedWindow()
    {
        Closing += GuardedWindow_Closing;
    }

    public void ApproveClose()
    {
        _closeConfirmed = true;
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void GuardedWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_closeConfirmed)
            return;

        if (_isCloseGuardRunning)
        {
            e.Cancel = true;
            return;
        }

        if (DataContext is not IAsyncCloseGuard closeGuard)
        {
            _closeConfirmed = true;
            return;
        }

        e.Cancel = true;
        _isCloseGuardRunning = true;

        bool canClose;
        try
        {
            canClose = await closeGuard.CanCloseAsync();
        }
        catch
        {
            canClose = false;
        }
        finally
        {
            _isCloseGuardRunning = false;
        }

        if (!canClose)
            return;

        _closeConfirmed = true;
        await Dispatcher.BeginInvoke(new Action(Close));
    }
}