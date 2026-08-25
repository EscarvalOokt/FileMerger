using System.Windows;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Shell.Help;

public sealed class KeyboardShortcutsDialogService : IKeyboardShortcutsDialogService
{
    private readonly IWindowOwnerResolver _ownerResolver;

    public KeyboardShortcutsDialogService(IWindowOwnerResolver ownerResolver)
    {
        ArgumentNullException.ThrowIfNull(ownerResolver);

        _ownerResolver = ownerResolver;
    }

    public void ShowDialog()
    {
        KeyboardShortcutsDialogWindow window = new()
        {
            DataContext = KeyboardShortcutsDialogViewModel.CreateDefault()
        };

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        window.ShowDialog();
    }
}