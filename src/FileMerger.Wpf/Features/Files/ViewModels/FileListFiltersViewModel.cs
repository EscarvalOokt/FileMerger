using System.ComponentModel;
using FileMerger.Domain.Enums;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Files.ViewModels;

public sealed class FileListFiltersViewModel : ViewModelBase
{
    private const string FallbackTextSearchLabel = "Fallback text";

    private string _searchText = string.Empty;
    private bool _showOnlySelected;

    public FileListFiltersViewModel()
    {
        InclusionFacets =
        [
            new(FileListFacet.Included, "Included"),
            new(FileListFacet.NotIncluded, "Not included")
        ];

        ReasonTypeFacets =
        [
            new(FileListFacet.ExcludedByProfileRule, "Profile rule"),
            new(FileListFacet.DisabledType, "Disabled type"),
            new(FileListFacet.Unsupported, "Unsupported"),
            new(FileListFacet.Fallback, "Fallback")
        ];

        WorkflowFacets =
        [
            new(FileListFacet.Overridden, "Overridden"),
            new(FileListFacet.NotApplied, "Not applied")
        ];

        AllFacets =
        [
            .. InclusionFacets,
            .. ReasonTypeFacets,
            .. WorkflowFacets
        ];

        foreach (FileListFilterOptionViewModel facet in AllFacets)
            facet.PropertyChanged += Facet_PropertyChanged;
    }

    public IReadOnlyList<FileListFilterOptionViewModel> InclusionFacets { get; }
    public IReadOnlyList<FileListFilterOptionViewModel> ReasonTypeFacets { get; }
    public IReadOnlyList<FileListFilterOptionViewModel> WorkflowFacets { get; }
    public IReadOnlyList<FileListFilterOptionViewModel> AllFacets { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public bool ShowOnlySelected
    {
        get => _showOnlySelected;
        set
        {
            if (SetProperty(ref _showOnlySelected, value))
                OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    public bool HasActiveFacets => SelectedFacetCount > 0;

    public int SelectedFacetCount => AllFacets.Count(x => x.IsSelected);

    public string FacetMenuLabel => SelectedFacetCount == 0
        ? "Filters"
        : $"Filters ({SelectedFacetCount})";

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        HasActiveFacets ||
        ShowOnlySelected;

    public void Reset()
    {
        SearchText = string.Empty;
        ShowOnlySelected = false;

        foreach (FileListFilterOptionViewModel facet in AllFacets)
            facet.IsSelected = false;
    }

    public bool Matches(
        InputFileItemViewModel file,
        IReadOnlyCollection<InputFileItemViewModel> selectedFiles)
    {
        return MatchesCommonConstraints(file, selectedFiles) &&
               MatchesFacetGroup(file, InclusionFacets) &&
               MatchesFacetGroup(file, ReasonTypeFacets) &&
               MatchesFacetGroup(file, WorkflowFacets);
    }

    public bool MatchesCommonConstraints(
        InputFileItemViewModel file,
        IReadOnlyCollection<InputFileItemViewModel> selectedFiles)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(selectedFiles);

        if (ShowOnlySelected && !selectedFiles.Contains(file))
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        return MatchesSearch(file, SearchText.Trim());
    }

    public bool MatchesForFacetCount(
        InputFileItemViewModel file,
        IReadOnlyCollection<InputFileItemViewModel> selectedFiles,
        FileListFacet facet)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(selectedFiles);

        return MatchesCommonConstraints(file, selectedFiles) &&
               MatchesFacet(file, facet) &&
               MatchesFacetGroupForCount(file, InclusionFacets, facet) &&
               MatchesFacetGroupForCount(file, ReasonTypeFacets, facet) &&
               MatchesFacetGroupForCount(file, WorkflowFacets, facet);
    }

    public static bool MatchesFacet(
        InputFileItemViewModel file,
        FileListFacet facet)
    {
        ArgumentNullException.ThrowIfNull(file);

        return facet switch
        {
            FileListFacet.Included => file.IsIncluded,
            FileListFacet.NotIncluded => file.IsNotIncluded,
            FileListFacet.ExcludedByProfileRule =>
                file.IsNotIncluded && HasSkipReasonCategory(
                    file,
                    SkippedFileCategory.ProfileExclusion),
            FileListFacet.DisabledType =>
                file.IsNotIncluded && HasSkipReasonCategory(
                    file,
                    SkippedFileCategory.DisabledFileType),
            FileListFacet.Unsupported =>
                file.IsNotIncluded && HasSkipReasonCategory(
                    file,
                    SkippedFileCategory.UnsupportedFile),
            FileListFacet.Fallback => file.HasFallbackStatus,
            FileListFacet.Overridden => file.HasOverrideStatus,
            FileListFacet.NotApplied => file.HasPendingPreviewState,
            _ => false
        };
    }

    private static bool MatchesFacetGroup(
        InputFileItemViewModel file,
        IReadOnlyCollection<FileListFilterOptionViewModel> facets)
    {
        bool hasSelectedFacet = false;

        foreach (FileListFilterOptionViewModel facet in facets)
        {
            if (!facet.IsSelected)
                continue;

            hasSelectedFacet = true;

            if (MatchesFacet(file, facet.Facet))
                return true;
        }

        return !hasSelectedFacet;
    }

    private static bool MatchesFacetGroupForCount(
        InputFileItemViewModel file,
        IReadOnlyCollection<FileListFilterOptionViewModel> facets,
        FileListFacet countedFacet)
    {
        if (facets.Any(x => x.Facet == countedFacet))
            return true;

        return MatchesFacetGroup(file, facets);
    }

    private static bool HasSkipReasonCategory(
        InputFileItemViewModel file,
        SkippedFileCategory category)
    {
        return file.Model.SkipReason?.Category == category;
    }

    private static bool MatchesSearch(
        InputFileItemViewModel file,
        string search)
    {
        return file.RelativePath.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               file.FullPath.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               file.Extension.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               file.Kind.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               file.SkipReason.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               file.IsFallbackText &&
               FallbackTextSearchLabel.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void Facet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FileListFilterOptionViewModel.IsSelected))
            return;

        OnPropertyChanged(nameof(HasActiveFacets));
        OnPropertyChanged(nameof(SelectedFacetCount));
        OnPropertyChanged(nameof(FacetMenuLabel));
        OnPropertyChanged(nameof(HasActiveFilters));
    }
}