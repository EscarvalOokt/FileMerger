using FileMerger.Wpf.Shell.Help;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeKeyboardShortcutsDialogService : IKeyboardShortcutsDialogService
{
    public int ShowDialogCalls { get; private set; }

    public void ShowDialog()
    {
        ShowDialogCalls++;
    }
}