namespace FileMerger.Wpf.Features.Settings;

public sealed record ApplicationPreferencesDto(
    bool? IsPreviewLineWrapEnabledByDefault = null,
    int? PreviewDisplayCharacterLimit = null,
    int? CrashLogRetentionLimit = null)
{
    public static ApplicationPreferencesDto FromModel(ApplicationPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        return new ApplicationPreferencesDto(
            preferences.IsPreviewLineWrapEnabledByDefault,
            preferences.PreviewDisplayCharacterLimit,
            preferences.CrashLogRetentionLimit);
    }

    public ApplicationPreferences ToModel()
    {
        return new ApplicationPreferences(
            IsPreviewLineWrapEnabledByDefault ??
            ApplicationPreferences.DefaultIsPreviewLineWrapEnabledByDefault,
            PreviewDisplayCharacterLimit ??
            ApplicationPreferences.DefaultPreviewDisplayCharacterLimit,
            CrashLogRetentionLimit ??
            ApplicationPreferences.DefaultCrashLogRetentionLimit);
    }
}