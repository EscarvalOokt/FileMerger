using FileMerger.Wpf.Features.Settings;

namespace FileMerger.Tests.Wpf.Features.Settings;

public sealed class ApplicationPreferencesStoreTests
{
    [Fact]
    public void Current_Should_Start_With_Default()
    {
        ApplicationPreferencesStore store = new(new FakeApplicationPreferencesService());

        Assert.Equal(ApplicationPreferences.Default, store.Current);
    }

    [Fact]
    public async Task InitializeAsync_Should_Load_Preferences()
    {
        ApplicationPreferences expected = new(true, 20_000, 50);
        FakeApplicationPreferencesService service = new()
        {
            PreferencesToLoad = expected
        };
        ApplicationPreferencesStore store = new(service);

        await store.InitializeAsync();

        Assert.Equal(expected, store.Current);
    }

    [Fact]
    public async Task UpdateAsync_Should_Save_And_Update_Current()
    {
        FakeApplicationPreferencesService service = new();
        ApplicationPreferencesStore store = new(service);
        ApplicationPreferences expected = new(true, 20_000, 50);

        await store.UpdateAsync(_ => expected);

        Assert.Equal(expected, Assert.Single(service.SavedPreferences));
        Assert.Equal(expected, store.Current);
    }

    [Fact]
    public async Task UpdateAsync_Should_Preserve_Current_When_Save_Fails()
    {
        FakeApplicationPreferencesService service = new()
        {
            SaveException = new InvalidOperationException("Save failed.")
        };
        ApplicationPreferencesStore store = new(service);
        ApplicationPreferences original = store.Current;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.UpdateAsync(_ => new ApplicationPreferences(true, 20_000, 50)));

        Assert.Equal(original, store.Current);
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Update_Is_Null()
    {
        ApplicationPreferencesStore store = new(new FakeApplicationPreferencesService());

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null!));
    }

    private sealed class FakeApplicationPreferencesService : IApplicationPreferencesService
    {
        public ApplicationPreferences PreferencesToLoad { get; set; } = ApplicationPreferences.Default;

        public Exception? SaveException { get; set; }

        public List<ApplicationPreferences> SavedPreferences { get; } = [];

        public Task<ApplicationPreferences> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PreferencesToLoad);
        }

        public Task SaveAsync(ApplicationPreferences preferences, CancellationToken cancellationToken = default)
        {
            if (SaveException is not null)
                return Task.FromException(SaveException);

            SavedPreferences.Add(preferences);
            return Task.CompletedTask;
        }
    }
}