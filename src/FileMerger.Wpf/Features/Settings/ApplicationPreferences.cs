namespace FileMerger.Wpf.Features.Settings;

public sealed record ApplicationPreferences
{
    public const bool DefaultIsPreviewLineWrapEnabledByDefault = false;
    public const int DefaultPreviewDisplayCharacterLimit = 500_000;
    public const int MinimumPreviewDisplayCharacterLimit = 10_000;
    public const int MaximumPreviewDisplayCharacterLimit = 5_000_000;
    public const int DefaultCrashLogRetentionLimit = 25;
    public const int MinimumCrashLogRetentionLimit = 1;
    public const int MaximumCrashLogRetentionLimit = 500;

    public ApplicationPreferences(
        bool isPreviewLineWrapEnabledByDefault,
        int previewDisplayCharacterLimit,
        int crashLogRetentionLimit)
    {
        IsPreviewLineWrapEnabledByDefault = isPreviewLineWrapEnabledByDefault;
        PreviewDisplayCharacterLimit = NormalizePreviewDisplayCharacterLimit(previewDisplayCharacterLimit);
        CrashLogRetentionLimit = NormalizeCrashLogRetentionLimit(crashLogRetentionLimit);
    }

    public static ApplicationPreferences Default { get; } = new(
        DefaultIsPreviewLineWrapEnabledByDefault,
        DefaultPreviewDisplayCharacterLimit,
        DefaultCrashLogRetentionLimit);

    public bool IsPreviewLineWrapEnabledByDefault { get; }

    public int PreviewDisplayCharacterLimit { get; }

    public int CrashLogRetentionLimit { get; }

    public static int NormalizePreviewDisplayCharacterLimit(int value)
    {
        return Math.Clamp(value, MinimumPreviewDisplayCharacterLimit, MaximumPreviewDisplayCharacterLimit);
    }

    public static int NormalizeCrashLogRetentionLimit(int value)
    {
        return Math.Clamp(value, MinimumCrashLogRetentionLimit, MaximumCrashLogRetentionLimit);
    }
}