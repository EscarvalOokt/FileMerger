using System.Windows;

namespace FileMerger.Wpf.Shell.Windows;

public class ShellDialogWindow : GuardedWindow
{
    public ShellDialogWindow()
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
    }
}