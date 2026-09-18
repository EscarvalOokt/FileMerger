using System.Windows;

namespace FileMerger.Wpf.Shell.Windows;

public sealed class WindowOwnerResolver : IWindowOwnerResolver
{
    public Window? ResolveOwner(Window? excludedWindow = null)
    {
        System.Windows.Application? application = System.Windows.Application.Current;
        if (application is null)
            return null;

        Window? activeWindow = application.Windows.OfType<Window>()
            .FirstOrDefault(x => x.IsActive && !ReferenceEquals(x, excludedWindow));

        if (activeWindow is not null)
            return activeWindow;

        Window? mainWindow = application.MainWindow;
        if (mainWindow is not null && !ReferenceEquals(mainWindow, excludedWindow))
            return mainWindow;

        return application.Windows.OfType<Window>()
            .FirstOrDefault(x => x.IsVisible && !ReferenceEquals(x, excludedWindow));
    }
}