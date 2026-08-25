using System.Windows;

namespace FileMerger.Wpf.Shell.Windows;

public interface IWindowOwnerResolver
{
    Window? ResolveOwner(Window? excludedWindow = null);
}