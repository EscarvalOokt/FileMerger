using FileMerger.Wpf.Features.Profile.Services;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeProfileManagerWindowService : IProfileManagerWindowService
{
    public int ShowDialogCalls { get; private set; }

    public int ShowDialogWithHostCalls { get; private set; }

    public ICurrentSessionProfileHost? LastProfileHost { get; private set; }

    public ProfileManagerContext? LastContext { get; private set; }

    public Func<ICurrentSessionProfileHost, Task>? OnShowDialogWithHostAsync { get; set; }

    public Task ShowDialogAsync()
    {
        ShowDialogCalls++;

        return Task.CompletedTask;
    }

    public Task ShowDialogAsync(ICurrentSessionProfileHost profileHost, ProfileManagerContext context)
    {
        ArgumentNullException.ThrowIfNull(profileHost);

        ShowDialogWithHostCalls++;
        LastProfileHost = profileHost;
        LastContext = context;

        return OnShowDialogWithHostAsync?.Invoke(profileHost) ?? Task.CompletedTask;
    }
}