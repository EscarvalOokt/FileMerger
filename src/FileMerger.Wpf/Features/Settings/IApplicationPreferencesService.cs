namespace FileMerger.Wpf.Features.Settings;

public interface IApplicationPreferencesService
{
    Task<ApplicationPreferences> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        ApplicationPreferences preferences,
        CancellationToken cancellationToken = default);
}