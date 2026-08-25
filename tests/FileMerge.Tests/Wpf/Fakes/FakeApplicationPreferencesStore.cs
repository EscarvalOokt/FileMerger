using FileMerger.Wpf.Features.Settings;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeApplicationPreferencesStore : IApplicationPreferencesStore
{
    public ApplicationPreferences Current { get; private set; } =
        ApplicationPreferences.Default;

    public List<ApplicationPreferences> SavedPreferences { get; } = [];

    public Exception? UpdateException { get; set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        Func<ApplicationPreferences, ApplicationPreferences> update,
        CancellationToken cancellationToken = default)
    {
        if (UpdateException is not null)
            return Task.FromException(UpdateException);

        Current = update(Current);
        SavedPreferences.Add(Current);
        return Task.CompletedTask;
    }

    public void SetCurrent(ApplicationPreferences preferences)
    {
        Current = preferences;
    }
}