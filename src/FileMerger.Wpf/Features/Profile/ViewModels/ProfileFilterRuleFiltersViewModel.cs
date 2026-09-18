using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileFilterRuleFiltersViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private ProfileFilterRuleStatusFilterMode _status = ProfileFilterRuleStatusFilterMode.All;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public ProfileFilterRuleStatusFilterMode Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) || Status != ProfileFilterRuleStatusFilterMode.All;

    public void Reset()
    {
        SearchText = string.Empty;
        Status = ProfileFilterRuleStatusFilterMode.All;
    }

    public bool Matches(ProfileFilterRuleItemViewModel rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (!MatchesStatus(rule))
            return false;

        string searchText = SearchText.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        return Contains(rule.Pattern, searchText) ||
               Contains(rule.Description, searchText) ||
               Contains(rule.ValidationMessage, searchText) ||
               Contains(rule.Mode.ToString(), searchText) ||
               Contains(rule.Target.ToString(), searchText) ||
               Contains(rule.PatternType.ToString(), searchText);
    }

    private bool MatchesStatus(ProfileFilterRuleItemViewModel rule)
    {
        return Status switch
        {
            ProfileFilterRuleStatusFilterMode.All => true,
            ProfileFilterRuleStatusFilterMode.Enabled => rule.IsEnabled,
            ProfileFilterRuleStatusFilterMode.Disabled => !rule.IsEnabled,
            ProfileFilterRuleStatusFilterMode.Invalid => rule.HasValidationError,
            _ => true
        };
    }

    private static bool Contains(string? value, string searchText)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}