using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileListFiltersViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private ProfileListKindFilterMode _kind = ProfileListKindFilterMode.All;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public ProfileListKindFilterMode Kind
    {
        get => _kind;
        set
        {
            if (SetProperty(ref _kind, value))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        Kind != ProfileListKindFilterMode.All;

    public void Reset()
    {
        SearchText = string.Empty;
        Kind = ProfileListKindFilterMode.All;
    }

    public bool Matches(ProfileLibraryListItemViewModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!MatchesKind(profile))
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        string search = SearchText.Trim();

        return Contains(profile.DisplayName, search) ||
               Contains(profile.Description, search) ||
               Contains(profile.KindLabel, search) ||
               Contains(profile.UsageLabel, search) ||
               Contains(profile.UpdatedLabel, search);
    }

    private bool MatchesKind(ProfileLibraryListItemViewModel profile)
    {
        return Kind switch
        {
            ProfileListKindFilterMode.All => true,
            ProfileListKindFilterMode.User => profile.IsUserDefined,
            ProfileListKindFilterMode.BuiltIn => profile.IsBuiltIn,
            _ => true
        };
    }

    private static bool Contains(string? value, string search)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}