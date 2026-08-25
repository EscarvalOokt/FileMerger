using System.Collections.ObjectModel;
using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Files.ViewModels;

namespace FileMerger.Tests.Wpf.Features.Files.ViewModels;

public sealed class FileListFiltersViewModelTests
{
    [Fact]
    public void MatchesFacet_NotIncluded_Should_Match_NotIncluded_File_WithoutSkipReason()
    {
        InputFileItemViewModel file = CreateItem(
            "ManualOff.cs",
            currentIncluded: false,
            skipReason: null);

        bool result = FileListFiltersViewModel.MatchesFacet(
            file,
            FileListFacet.NotIncluded);

        Assert.True(result);
    }

    [Fact]
    public void MatchesFacet_ExcludedByProfileRule_Should_Match_NotIncluded_File_With_ProfileRule_Reason()
    {
        InputFileItemViewModel file = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        bool result = FileListFiltersViewModel.MatchesFacet(
            file,
            FileListFacet.ExcludedByProfileRule);

        Assert.True(result);
    }

    [Fact]
    public void MatchesFacet_ExcludedByProfileRule_Should_Not_Match_Manually_Included_File_With_ProfileRule_Reason()
    {
        InputFileItemViewModel file = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: true,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        bool result = FileListFiltersViewModel.MatchesFacet(
            file,
            FileListFacet.ExcludedByProfileRule);

        Assert.False(result);
    }

    [Fact]
    public void MatchesFacet_DisabledType_Should_Match_Disabled_Known_FileType()
    {
        InputFileItemViewModel file = CreateItem(
            "Disabled.json",
            extension: ".json",
            kind: FileKind.Json,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.file-type-disabled",
                "File type is disabled in the current profile."),
            isMergeCandidate: false);

        Assert.True(FileListFiltersViewModel.MatchesFacet(file, FileListFacet.DisabledType));
        Assert.False(FileListFiltersViewModel.MatchesFacet(file, FileListFacet.Unsupported));
    }

    [Fact]
    public void MatchesFacet_Unsupported_Should_Match_Unsupported_File()
    {
        InputFileItemViewModel file = CreateItem(
            "Unknown.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        Assert.True(FileListFiltersViewModel.MatchesFacet(file, FileListFacet.Unsupported));
        Assert.False(FileListFiltersViewModel.MatchesFacet(file, FileListFacet.DisabledType));
    }

    [Fact]
    public void MatchesFacet_Unsupported_Should_Not_Match_Included_File_With_Unsupported_Reason()
    {
        InputFileItemViewModel file = CreateItem(
            "Unknown.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            automaticIncluded: false,
            currentIncluded: true,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."));

        Assert.False(FileListFiltersViewModel.MatchesFacet(file, FileListFacet.Unsupported));
    }

    [Fact]
    public void MatchesFacet_Fallback_Should_Match_FallbackText_File()
    {
        InputFileItemViewModel file = CreateItem(
            "Unknown.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

        bool result = FileListFiltersViewModel.MatchesFacet(
            file,
            FileListFacet.Fallback);

        Assert.True(result);
    }

    [Fact]
    public void Matches_Should_Not_Restrict_By_Facets_When_None_Are_Selected()
    {
        var filters = new FileListFiltersViewModel();

        InputFileItemViewModel included = CreateItem("Included.cs");
        InputFileItemViewModel notIncluded = CreateItem(
            "NotIncluded.cs",
            currentIncluded: false,
            appliedIncluded: false);

        Assert.True(filters.Matches(included, []));
        Assert.True(filters.Matches(notIncluded, []));
        Assert.False(filters.HasActiveFacets);
        Assert.False(filters.HasActiveFilters);
        Assert.Equal(0, filters.SelectedFacetCount);
        Assert.Equal("Filters", filters.FacetMenuLabel);
    }

    [Fact]
    public void Matches_Should_Use_Or_Within_Inclusion_Group()
    {
        var filters = new FileListFiltersViewModel();
        SelectFacet(filters, FileListFacet.Included);
        SelectFacet(filters, FileListFacet.NotIncluded);

        InputFileItemViewModel included = CreateItem("Included.cs");
        InputFileItemViewModel notIncluded = CreateItem(
            "NotIncluded.cs",
            currentIncluded: false,
            appliedIncluded: false);

        Assert.True(filters.Matches(included, []));
        Assert.True(filters.Matches(notIncluded, []));
    }

    [Fact]
    public void Matches_Should_Use_Or_Within_ReasonType_Group()
    {
        var filters = new FileListFiltersViewModel();
        SelectFacet(filters, FileListFacet.ExcludedByProfileRule);
        SelectFacet(filters, FileListFacet.DisabledType);
        SelectFacet(filters, FileListFacet.Unsupported);
        SelectFacet(filters, FileListFacet.Fallback);

        InputFileItemViewModel profileRule = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel disabledType = CreateItem(
            "Disabled.json",
            extension: ".json",
            kind: FileKind.Json,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.file-type-disabled",
                "File type is disabled in the current profile."),
            isMergeCandidate: false);

        InputFileItemViewModel unsupported = CreateItem(
            "Unknown.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        InputFileItemViewModel fallback = CreateItem(
            "Unknown.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

        InputFileItemViewModel regular = CreateItem("Regular.cs");

        Assert.True(filters.Matches(profileRule, []));
        Assert.True(filters.Matches(disabledType, []));
        Assert.True(filters.Matches(unsupported, []));
        Assert.True(filters.Matches(fallback, []));
        Assert.False(filters.Matches(regular, []));
    }

    [Fact]
    public void Matches_Should_Use_Or_Within_Workflow_Group()
    {
        var filters = new FileListFiltersViewModel();
        SelectFacet(filters, FileListFacet.Overridden);
        SelectFacet(filters, FileListFacet.NotApplied);

        InputFileItemViewModel overridden = CreateItem(
            "Overridden.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: false);

        InputFileItemViewModel notApplied = CreateItem(
            "NotApplied.cs",
            automaticIncluded: true,
            currentIncluded: true,
            appliedIncluded: false);

        InputFileItemViewModel regular = CreateItem("Regular.cs");

        Assert.True(filters.Matches(overridden, []));
        Assert.True(filters.Matches(notApplied, []));
        Assert.False(filters.Matches(regular, []));
    }

    [Fact]
    public void Matches_Should_Use_And_Across_Active_Facet_Groups()
    {
        var filters = new FileListFiltersViewModel();
        SelectFacet(filters, FileListFacet.NotIncluded);
        SelectFacet(filters, FileListFacet.ExcludedByProfileRule);
        SelectFacet(filters, FileListFacet.NotApplied);

        InputFileItemViewModel matching = CreateItem(
            "Generated.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: true,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel alreadyApplied = CreateItem(
            "AutomaticSkip.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel manuallyIncluded = CreateItem(
            "Included.cs",
            automaticIncluded: false,
            currentIncluded: true,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        Assert.True(filters.Matches(matching, []));
        Assert.False(filters.Matches(alreadyApplied, []));
        Assert.False(filters.Matches(manuallyIncluded, []));
    }

    [Fact]
    public void Matches_Should_Combine_Fallback_And_Override_Across_Groups()
    {
        var filters = new FileListFiltersViewModel();
        SelectFacet(filters, FileListFacet.NotIncluded);
        SelectFacet(filters, FileListFacet.Fallback);
        SelectFacet(filters, FileListFacet.Overridden);

        InputFileItemViewModel matching = CreateItem(
            "ExcludedFallback.custom",
            extension: ".custom",
            kind: FileKind.Text,
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: false,
            isFallbackText: true);

        InputFileItemViewModel includedFallback = CreateItem(
            "IncludedFallback.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

        InputFileItemViewModel regularOverride = CreateItem(
            "Regular.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: false);

        Assert.True(filters.Matches(matching, []));
        Assert.False(filters.Matches(includedFallback, []));
        Assert.False(filters.Matches(regularOverride, []));
    }

    [Fact]
    public void Matches_Should_Combine_SearchText_With_ProfileRule_Facet()
    {
        var filters = new FileListFiltersViewModel
        {
            SearchText = "Generated"
        };
        SelectFacet(filters, FileListFacet.ExcludedByProfileRule);

        InputFileItemViewModel matchingFile = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel nonMatchingFile = CreateItem(
            "Other.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        Assert.True(filters.Matches(matchingFile, []));
        Assert.False(filters.Matches(nonMatchingFile, []));
    }

    [Fact]
    public void Matches_Should_Combine_ShowOnlySelected_With_DisabledType_Facet()
    {
        var filters = new FileListFiltersViewModel
        {
            ShowOnlySelected = true
        };
        SelectFacet(filters, FileListFacet.DisabledType);

        InputFileItemViewModel selectedFile = CreateItem(
            "Selected.json",
            extension: ".json",
            kind: FileKind.Json,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.file-type-disabled",
                "File type is disabled in the current profile."),
            isMergeCandidate: false);

        InputFileItemViewModel unselectedFile = CreateItem(
            "Unselected.json",
            extension: ".json",
            kind: FileKind.Json,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.file-type-disabled",
                "File type is disabled in the current profile."),
            isMergeCandidate: false);

        var selectedFiles = new ObservableCollection<InputFileItemViewModel>
        {
            selectedFile
        };

        Assert.True(filters.Matches(selectedFile, selectedFiles));
        Assert.False(filters.Matches(unselectedFile, selectedFiles));
    }

    [Fact]
    public void Matches_Should_Combine_Search_ShowOnlySelected_And_Multiple_Facet_Groups()
    {
        var filters = new FileListFiltersViewModel
        {
            SearchText = "Generated",
            ShowOnlySelected = true
        };
        SelectFacet(filters, FileListFacet.NotIncluded);
        SelectFacet(filters, FileListFacet.ExcludedByProfileRule);

        InputFileItemViewModel matching = CreateItem(
            "Generated.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel unselected = CreateItem(
            "GeneratedOther.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        var selectedFiles = new ObservableCollection<InputFileItemViewModel>
        {
            matching
        };

        Assert.True(filters.Matches(matching, selectedFiles));
        Assert.False(filters.Matches(unselected, selectedFiles));
    }

    [Fact]
    public void MatchesForFacetCount_Should_Apply_Target_Facet_When_No_Facets_Are_Selected()
    {
        var filters = new FileListFiltersViewModel();

        InputFileItemViewModel profileRuleExcluded = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel included = CreateItem("Included.cs");

        Assert.True(filters.MatchesForFacetCount(
            profileRuleExcluded,
            [],
            FileListFacet.ExcludedByProfileRule));
        Assert.False(filters.MatchesForFacetCount(
            included,
            [],
            FileListFacet.ExcludedByProfileRule));
    }

    [Fact]
    public void MatchesForFacetCount_Should_Respect_Selected_Facets_From_Other_Groups()
    {
        var filters = new FileListFiltersViewModel();
        SelectFacet(filters, FileListFacet.NotIncluded);

        InputFileItemViewModel unsupported = CreateItem(
            "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        InputFileItemViewModel includedFallback = CreateItem(
            "Included.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

        Assert.True(filters.MatchesForFacetCount(
            unsupported,
            [],
            FileListFacet.Unsupported));
        Assert.False(filters.MatchesForFacetCount(
            includedFallback,
            [],
            FileListFacet.Fallback));
    }

    [Fact]
    public void MatchesForFacetCount_Should_Ignore_Selected_Facets_From_Target_Group()
    {
        var filters = new FileListFiltersViewModel();
        SelectFacet(filters, FileListFacet.Unsupported);

        InputFileItemViewModel profileRuleExcluded = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        Assert.False(filters.Matches(profileRuleExcluded, []));
        Assert.True(filters.MatchesForFacetCount(
            profileRuleExcluded,
            [],
            FileListFacet.ExcludedByProfileRule));
    }

    [Fact]
    public void MatchesForFacetCount_Should_Respect_Other_Active_Groups_When_Own_Group_Is_Ignored()
    {
        var filters = new FileListFiltersViewModel();
        SelectFacet(filters, FileListFacet.NotIncluded);
        SelectFacet(filters, FileListFacet.Unsupported);
        SelectFacet(filters, FileListFacet.NotApplied);

        InputFileItemViewModel pendingProfileRule = CreateItem(
            "Pending.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: true,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel appliedProfileRule = CreateItem(
            "Applied.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        Assert.True(filters.MatchesForFacetCount(
            pendingProfileRule,
            [],
            FileListFacet.ExcludedByProfileRule));
        Assert.False(filters.MatchesForFacetCount(
            appliedProfileRule,
            [],
            FileListFacet.ExcludedByProfileRule));
    }

    [Fact]
    public void MatchesForFacetCount_Should_Respect_Search_And_ShowOnlySelected()
    {
        var filters = new FileListFiltersViewModel
        {
            SearchText = "Alpha",
            ShowOnlySelected = true
        };

        InputFileItemViewModel selectedAlpha = CreateItem("Alpha.cs");
        InputFileItemViewModel selectedBeta = CreateItem("Beta.cs");
        InputFileItemViewModel unselectedAlpha = CreateItem("AlphaOther.cs");

        var selectedFiles = new ObservableCollection<InputFileItemViewModel>
        {
            selectedAlpha,
            selectedBeta
        };

        Assert.True(filters.MatchesForFacetCount(
            selectedAlpha,
            selectedFiles,
            FileListFacet.Included));
        Assert.False(filters.MatchesForFacetCount(
            selectedBeta,
            selectedFiles,
            FileListFacet.Included));
        Assert.False(filters.MatchesForFacetCount(
            unselectedAlpha,
            selectedFiles,
            FileListFacet.Included));
    }

    [Fact]
    public void MatchesForFacetCount_Should_Preserve_Overlapping_ReasonType_States()
    {
        var filters = new FileListFiltersViewModel();

        InputFileItemViewModel fallbackUnsupported = CreateItem(
            "Fallback.custom",
            extension: ".custom",
            kind: FileKind.Text,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isFallbackText: true,
            isMergeCandidate: false);

        Assert.True(filters.MatchesForFacetCount(
            fallbackUnsupported,
            [],
            FileListFacet.Unsupported));
        Assert.True(filters.MatchesForFacetCount(
            fallbackUnsupported,
            [],
            FileListFacet.Fallback));
    }

    [Fact]
    public void Search_Should_Match_FallbackText_Label()
    {
        var filters = new FileListFiltersViewModel
        {
            SearchText = "fallback"
        };

        InputFileItemViewModel fallbackFile = CreateItem(
            "Unknown.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

        InputFileItemViewModel regularFile = CreateItem(
            "Regular.txt",
            extension: ".txt",
            kind: FileKind.Text,
            isFallbackText: false);

        Assert.True(filters.Matches(fallbackFile, []));
        Assert.False(filters.Matches(regularFile, []));
    }

    [Fact]
    public void FacetMenuLabel_Should_Report_SelectedFacetCount()
    {
        var filters = new FileListFiltersViewModel();

        SelectFacet(filters, FileListFacet.Included);
        SelectFacet(filters, FileListFacet.Unsupported);
        SelectFacet(filters, FileListFacet.NotApplied);

        Assert.True(filters.HasActiveFacets);
        Assert.Equal(3, filters.SelectedFacetCount);
        Assert.Equal("Filters (3)", filters.FacetMenuLabel);
    }

    [Fact]
    public void Selecting_Facet_Should_Raise_Menu_State_PropertyChanged_Notifications()
    {
        var filters = new FileListFiltersViewModel();
        var changedProperties = new List<string?>();
        filters.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        SelectFacet(filters, FileListFacet.Unsupported);

        Assert.Contains(nameof(FileListFiltersViewModel.HasActiveFacets), changedProperties);
        Assert.Contains(nameof(FileListFiltersViewModel.SelectedFacetCount), changedProperties);
        Assert.Contains(nameof(FileListFiltersViewModel.FacetMenuLabel), changedProperties);
        Assert.Contains(nameof(FileListFiltersViewModel.HasActiveFilters), changedProperties);
    }

    [Fact]
    public void Reset_Should_Clear_Search_Facets_ShowOnlySelected_And_Menu_State()
    {
        var filters = new FileListFiltersViewModel
        {
            SearchText = "abc",
            ShowOnlySelected = true
        };
        SelectFacet(filters, FileListFacet.Included);
        SelectFacet(filters, FileListFacet.DisabledType);
        SelectFacet(filters, FileListFacet.NotApplied);

        filters.Reset();

        Assert.Equal(string.Empty, filters.SearchText);
        Assert.False(filters.ShowOnlySelected);
        Assert.False(filters.HasActiveFacets);
        Assert.False(filters.HasActiveFilters);
        Assert.Equal(0, filters.SelectedFacetCount);
        Assert.Equal("Filters", filters.FacetMenuLabel);
        Assert.All(filters.AllFacets, x => Assert.False(x.IsSelected));
    }

    private static void SelectFacet(
        FileListFiltersViewModel filters,
        FileListFacet facet)
    {
        filters.AllFacets.Single(x => x.Facet == facet).IsSelected = true;
    }

    private static InputFileItemViewModel CreateItem(
        string relativePath,
        string? extension = null,
        FileKind kind = FileKind.CSharp,
        bool automaticIncluded = true,
        bool currentIncluded = true,
        bool appliedIncluded = true,
        SkipReason? skipReason = null,
        bool isFallbackText = false,
        bool isMergeCandidate = true)
    {
        extension ??= Path.GetExtension(relativePath);

        var model = new InputFile(
            fullPath: $@"D:\Project\{relativePath}",
            relativePath: relativePath,
            extension: extension,
            kind: kind,
            isIncluded: automaticIncluded,
            skipReason: skipReason,
            isFallbackText: isFallbackText,
            isMergeCandidate: isMergeCandidate);

        return new InputFileItemViewModel(
            model: model,
            automaticIncluded: automaticIncluded,
            currentIncluded: currentIncluded,
            appliedIncluded: appliedIncluded)
        {
            HasManualOverride = currentIncluded != automaticIncluded,
            IsAppliedInPreview = currentIncluded == appliedIncluded
        };
    }
}