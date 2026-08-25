using FileMerger.Wpf.Features.Settings.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakePreferencesDialogService : IPreferencesDialogService
{
    public int ShowDialogCalls { get; private set; }

    public bool Result { get; set; }

    public Task<bool> ShowDialogAsync()
    {
        ShowDialogCalls++;
        return Task.FromResult(Result);
    }
}