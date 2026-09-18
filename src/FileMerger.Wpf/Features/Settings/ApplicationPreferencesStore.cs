namespace FileMerger.Wpf.Features.Settings;

public sealed class ApplicationPreferencesStore : IApplicationPreferencesStore
{
    private readonly IApplicationPreferencesService _preferencesService;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public ApplicationPreferencesStore(IApplicationPreferencesService preferencesService)
    {
        ArgumentNullException.ThrowIfNull(preferencesService);
        _preferencesService = preferencesService;
    }

    public ApplicationPreferences Current { get; private set; } = ApplicationPreferences.Default;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            Current = await _preferencesService.LoadAsync(cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task UpdateAsync(
        Func<ApplicationPreferences, ApplicationPreferences> update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        await _sync.WaitAsync(cancellationToken);
        try
        {
            ApplicationPreferences next = update(Current);
            ArgumentNullException.ThrowIfNull(next);

            ApplicationPreferences normalized = new(
                next.IsPreviewLineWrapEnabledByDefault,
                next.PreviewDisplayCharacterLimit,
                next.CrashLogRetentionLimit);

            await _preferencesService.SaveAsync(normalized, cancellationToken);
            Current = normalized;
        }
        finally
        {
            _sync.Release();
        }
    }
}