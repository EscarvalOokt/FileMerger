using FileMerger.Wpf.Features.Profile.Services;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeProfileManagerWindowService : IProfileManagerWindowService
{
    public int ShowDialogCalls { get; private set; }

    public Func<Task>? OnShowDialogAsync { get; set; }

    public Task ShowDialogAsync()
    {
        ShowDialogCalls++;

        return OnShowDialogAsync?.Invoke()
               ?? Task.CompletedTask;
    }
}