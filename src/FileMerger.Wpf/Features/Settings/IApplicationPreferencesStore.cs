namespace FileMerger.Wpf.Features.Settings;

public interface IApplicationPreferencesStore
{
    ApplicationPreferences Current { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Func<ApplicationPreferences, ApplicationPreferences> update,
        CancellationToken cancellationToken = default);
}