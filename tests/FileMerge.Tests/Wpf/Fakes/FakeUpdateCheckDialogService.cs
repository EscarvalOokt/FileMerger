using FileMerger.Wpf.Features.Updates.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeUpdateCheckDialogService : IUpdateCheckDialogService
{
    public int ShowDialogCalls { get; private set; }

    public void ShowDialog()
    {
        ShowDialogCalls++;
    }
}