using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileFileTypeFiltersViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private ProfileFileTypeFilterMode _mode = ProfileFileTypeFilterMode.All;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public ProfileFileTypeFilterMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        Mode != ProfileFileTypeFilterMode.All;

    public void Reset()
    {
        SearchText = string.Empty;
        Mode = ProfileFileTypeFilterMode.All;
    }

    public bool Matches(
        FileTypeOptionViewModel fileType,
        ProfileFileTypeGroupViewModel group)
    {
        ArgumentNullException.ThrowIfNull(fileType);
        ArgumentNullException.ThrowIfNull(group);

        if (!MatchesStatus(fileType))
            return false;

        string searchText = SearchText.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        if (Contains(group.Title, searchText) ||
            Contains(group.Description, searchText))
        {
            return true;
        }

        return Contains(fileType.Extension, searchText) ||
               Contains(fileType.DisplayName, searchText) ||
               Contains(fileType.DisplayLabel, searchText) ||
               Contains(fileType.Kind.ToString(), searchText);
    }

    private bool MatchesStatus(FileTypeOptionViewModel fileType)
    {
        return Mode switch
        {
            ProfileFileTypeFilterMode.All => true,
            ProfileFileTypeFilterMode.Enabled => fileType.IsEnabled,
            ProfileFileTypeFilterMode.Disabled => !fileType.IsEnabled,
            _ => true
        };
    }

    private static bool Contains(string? value, string searchText)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}