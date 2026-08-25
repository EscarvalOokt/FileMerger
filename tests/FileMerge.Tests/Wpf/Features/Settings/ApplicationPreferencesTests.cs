using FileMerger.Wpf.Features.Preview;
using FileMerger.Wpf.Features.Settings;

namespace FileMerger.Tests.Wpf.Features.Settings;

public sealed class ApplicationPreferencesTests
{
    [Fact]
    public void Default_Should_Use_Current_Baseline_Values()
    {
        ApplicationPreferences preferences = ApplicationPreferences.Default;

        Assert.False(preferences.IsPreviewLineWrapEnabledByDefault);
        Assert.Equal(500_000, preferences.PreviewDisplayCharacterLimit);
        Assert.Equal(25, preferences.CrashLogRetentionLimit);
    }

    [Fact]
    public void Constructor_Should_Clamp_PreviewDisplayCharacterLimit_To_Minimum()
    {
        var preferences = new ApplicationPreferences(
            false,
            ApplicationPreferences.MinimumPreviewDisplayCharacterLimit - 1,
            ApplicationPreferences.DefaultCrashLogRetentionLimit);

        Assert.Equal(
            ApplicationPreferences.MinimumPreviewDisplayCharacterLimit,
            preferences.PreviewDisplayCharacterLimit);
    }

    [Fact]
    public void Constructor_Should_Clamp_PreviewDisplayCharacterLimit_To_Maximum()
    {
        var preferences = new ApplicationPreferences(
            false,
            ApplicationPreferences.MaximumPreviewDisplayCharacterLimit + 1,
            ApplicationPreferences.DefaultCrashLogRetentionLimit);

        Assert.Equal(
            ApplicationPreferences.MaximumPreviewDisplayCharacterLimit,
            preferences.PreviewDisplayCharacterLimit);
    }

    [Fact]
    public void Constructor_Should_Clamp_CrashLogRetentionLimit_To_Minimum()
    {
        var preferences = new ApplicationPreferences(
            false,
            ApplicationPreferences.DefaultPreviewDisplayCharacterLimit,
            ApplicationPreferences.MinimumCrashLogRetentionLimit - 1);

        Assert.Equal(
            ApplicationPreferences.MinimumCrashLogRetentionLimit,
            preferences.CrashLogRetentionLimit);
    }

    [Fact]
    public void Constructor_Should_Clamp_CrashLogRetentionLimit_To_Maximum()
    {
        var preferences = new ApplicationPreferences(
            false,
            ApplicationPreferences.DefaultPreviewDisplayCharacterLimit,
            ApplicationPreferences.MaximumCrashLogRetentionLimit + 1);

        Assert.Equal(
            ApplicationPreferences.MaximumCrashLogRetentionLimit,
            preferences.CrashLogRetentionLimit);
    }

    [Fact]
    public void DefaultPreviewDisplayCharacterLimit_Should_Match_Current_PreviewTextFormatter_Default()
    {
        Assert.Equal(
            PreviewTextFormatter.MaxPreviewCharacters,
            ApplicationPreferences.DefaultPreviewDisplayCharacterLimit);
    }
}