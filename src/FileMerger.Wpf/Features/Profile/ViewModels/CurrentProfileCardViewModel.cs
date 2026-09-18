using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class CurrentProfileCardViewModel : ViewModelBase
{
    private string _profileName = "Default";
    private string _profileSourceText = "Custom workspace profile";

    public string ProfileName
    {
        get => _profileName;
        private set => SetProperty(ref _profileName, value);
    }

    public string ProfileSourceText
    {
        get => _profileSourceText;
        private set => SetProperty(ref _profileSourceText, value);
    }

    public bool IsLinkedToLibrary => !string.IsNullOrWhiteSpace(ProfileEntryId);

    public string? ProfileEntryId { get; private set; }

    public string? ProfileOriginEntryId { get; private set; }

    public string? ProfileOriginDisplayName { get; private set; }

    public ProfileSummaryViewModel Summary { get; } = new();

    public void Apply(
        string profileName,
        string? profileEntryId,
        string? profileOriginEntryId,
        string? profileOriginDisplayName,
        WorkspaceProfileDto profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ProfileName = string.IsNullOrWhiteSpace(profileName) ? "Profile" : profileName.Trim();

        ProfileEntryId = NormalizeOptional(profileEntryId);
        ProfileOriginEntryId = NormalizeOptional(profileOriginEntryId);
        ProfileOriginDisplayName = NormalizeOptional(profileOriginDisplayName);

        ProfileSourceText = ProfileSourceTextFormatter.Format(
            ProfileEntryId,
            ProfileOriginEntryId,
            ProfileOriginDisplayName);

        Summary.Apply(profile);

        OnPropertyChanged(nameof(IsLinkedToLibrary));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

internal static class ProfileSourceTextFormatter
{
    public static string Format(string? profileEntryId, string? profileOriginEntryId, string? profileOriginDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(profileEntryId))
            return "From profile library";

        if (!string.IsNullOrWhiteSpace(profileOriginDisplayName))
            return $"Custom workspace profile based on '{profileOriginDisplayName.Trim()}'";

        if (!string.IsNullOrWhiteSpace(profileOriginEntryId))
            return "Custom workspace profile based on a profile library entry";

        return "Custom workspace profile";
    }
}