using System.IO;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Wpf.Features.Files.ViewModels;

namespace FileMerger.Tests.Wpf.Features.Files.ViewModels;

public sealed class FilesPaneViewModelTests
{
    [Fact]
    public void New_FilesPane_Should_Report_NoFiles()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.HasFiles);
        Assert.True(viewModel.HasNoFiles);
        Assert.False(viewModel.HasVisibleFiles);
        Assert.False(viewModel.HasNoVisibleFiles);
        Assert.False(viewModel.ShowFilteredFilesEmptyState);
        Assert.Equal("0 of 0 files shown", viewModel.FilterSummary);
    }

    [Fact]
    public void LoadFiles_Should_Report_HasFiles()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        viewModel.LoadFiles([CreateFile("src/FileA.cs")]);

        Assert.True(viewModel.HasFiles);
        Assert.False(viewModel.HasNoFiles);
        Assert.True(viewModel.HasVisibleFiles);
        Assert.False(viewModel.HasNoVisibleFiles);
        Assert.False(viewModel.ShowFilteredFilesEmptyState);
        Assert.Equal("1 of 1 files shown", viewModel.FilterSummary);
    }

    [Fact]
    public void Filter_With_No_Matches_Should_Report_HasNoVisibleFiles()
    {
        FilesPaneViewModel viewModel = CreateViewModel();
        viewModel.LoadFiles([CreateFile("src/FileA.cs")]);

        viewModel.Filters.SearchText = "does-not-match";

        Assert.True(viewModel.HasFiles);
        Assert.False(viewModel.HasVisibleFiles);
        Assert.True(viewModel.HasNoVisibleFiles);
        Assert.True(viewModel.HasActiveFilters);
        Assert.True(viewModel.ShowFilteredFilesEmptyState);
        Assert.Equal("0 of 1 files shown", viewModel.FilterSummary);
    }

    [Fact]
    public void ActiveFilters_With_NoFiles_Should_Not_Show_FilteredEmptyState()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        viewModel.Filters.SearchText = "anything";

        Assert.False(viewModel.HasFiles);
        Assert.False(viewModel.HasNoVisibleFiles);
        Assert.True(viewModel.HasActiveFilters);
        Assert.False(viewModel.ShowFilteredFilesEmptyState);
        Assert.Equal("0 of 0 files shown", viewModel.FilterSummary);
    }

    [Fact]
    public void ClearFilters_Should_Clear_FilterEmptyState()
    {
        FilesPaneViewModel viewModel = CreateViewModel();
        viewModel.LoadFiles([CreateFile("src/FileA.cs")]);
        viewModel.Filters.SearchText = "does-not-match";

        viewModel.ClearFileFiltersCommand.Execute(null);

        Assert.True(viewModel.HasVisibleFiles);
        Assert.False(viewModel.HasNoVisibleFiles);
        Assert.False(viewModel.HasActiveFilters);
        Assert.False(viewModel.ShowFilteredFilesEmptyState);
        Assert.Equal("1 of 1 files shown", viewModel.FilterSummary);
    }

    [Fact]
    public void ExcludeSelectedFilesCommand_Should_Not_Throw_When_SelectedFiles_Changes_During_Update()
    {
        FilesPaneViewModel vm = CreateViewModel();

        InputFileItemViewModel first = CreateFile("src/FileA.cs");
        InputFileItemViewModel second = CreateFile("src/FileB.cs");
        InputFileItemViewModel third = CreateFile("src/FileC.cs");

        vm.LoadFiles([first, second, third]);
        vm.ReplaceSelectedFiles([first, second, third]);

        first.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InputFileItemViewModel.IsIncluded))
                vm.ReplaceSelectedFiles([]);
        };

        Exception? exception = Record.Exception(() => vm.ExcludeSelectedFilesCommand.Execute(null));

        Assert.Null(exception);

        Assert.False(first.IsIncluded);
        Assert.False(second.IsIncluded);
        Assert.False(third.IsIncluded);

        Assert.True(first.HasManualOverride);
        Assert.True(second.HasManualOverride);
        Assert.True(third.HasManualOverride);
    }

    [Fact]
    public void IncludeSelectedFilesCommand_Should_Not_Throw_When_SelectedFiles_Changes_During_Update()
    {
        FilesPaneViewModel vm = CreateViewModel();

        InputFileItemViewModel first = CreateFile(
            "src/FileA.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: true);

        InputFileItemViewModel second = CreateFile(
            "src/FileB.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: true);

        vm.LoadFiles([first, second]);
        vm.ReplaceSelectedFiles([first, second]);

        first.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InputFileItemViewModel.IsIncluded))
                vm.ReplaceSelectedFiles([]);
        };

        Exception? exception = Record.Exception(() => vm.IncludeSelectedFilesCommand.Execute(null));

        Assert.Null(exception);

        Assert.True(first.IsIncluded);
        Assert.True(second.IsIncluded);

        Assert.False(first.HasManualOverride);
        Assert.False(second.HasManualOverride);
    }

    [Fact]
    public void ResetSelectedFileOverridesCommand_Should_Not_Throw_When_SelectedFiles_Changes_During_Update()
    {
        FilesPaneViewModel vm = CreateViewModel();

        InputFileItemViewModel first = CreateFile(
            "src/FileA.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: true);

        InputFileItemViewModel second = CreateFile(
            "src/FileB.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: true);

        vm.LoadFiles([first, second]);
        vm.ReplaceSelectedFiles([first, second]);

        first.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InputFileItemViewModel.IsIncluded))
                vm.ReplaceSelectedFiles([]);
        };

        Exception? exception = Record.Exception(() => vm.ResetSelectedFileOverridesCommand.Execute(null));

        Assert.Null(exception);

        Assert.True(first.IsIncluded);
        Assert.True(second.IsIncluded);

        Assert.False(first.HasManualOverride);
        Assert.False(second.HasManualOverride);
    }

    [Fact]
    public void ExcludeSelectedFilesCommand_Should_Update_All_Initially_Selected_Files()
    {
        FilesPaneViewModel vm = CreateViewModel();

        InputFileItemViewModel first = CreateFile("src/FileA.cs");
        InputFileItemViewModel second = CreateFile("src/FileB.cs");
        InputFileItemViewModel third = CreateFile("src/FileC.cs");

        vm.LoadFiles([first, second, third]);
        vm.ReplaceSelectedFiles([first, second, third]);

        first.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InputFileItemViewModel.IsIncluded))
                vm.ReplaceSelectedFiles([first]);
        };

        vm.ExcludeSelectedFilesCommand.Execute(null);

        Assert.False(first.IsIncluded);
        Assert.False(second.IsIncluded);
        Assert.False(third.IsIncluded);
    }

    [Fact]
    public void ExcludeSelectedFilesCommand_Should_Raise_FileOverridesChanged_Once()
    {
        FilesPaneViewModel vm = CreateViewModel();

        InputFileItemViewModel first = CreateFile("src/FileA.cs");
        InputFileItemViewModel second = CreateFile("src/FileB.cs");

        vm.LoadFiles([first, second]);
        vm.ReplaceSelectedFiles([first, second]);

        int eventCount = 0;
        vm.FileOverridesChanged += (_, _) => eventCount++;

        vm.ExcludeSelectedFilesCommand.Execute(null);

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void FacetGroups_Should_Expose_All_File_Facets_With_Counts()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        Assert.Collection(
            viewModel.Filters.InclusionFacets,
            x => AssertOption(x, FileListFacet.Included, "Included"),
            x => AssertOption(x, FileListFacet.NotIncluded, "Not included"));

        Assert.Collection(
            viewModel.Filters.ReasonTypeFacets,
            x => AssertOption(x, FileListFacet.ExcludedByProfileRule, "Profile rule"),
            x => AssertOption(x, FileListFacet.DisabledType, "Disabled type"),
            x => AssertOption(x, FileListFacet.Unsupported, "Unsupported"),
            x => AssertOption(x, FileListFacet.Fallback, "Fallback"));

        Assert.Collection(
            viewModel.Filters.WorkflowFacets,
            x => AssertOption(x, FileListFacet.Overridden, "Overridden"),
            x => AssertOption(x, FileListFacet.NotApplied, "Not applied"));
    }

    [Fact]
    public void Selecting_Facet_Should_Refresh_FilesView_And_Activate_ClearFilters()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        InputFileItemViewModel included = CreateItem("Included.cs");
        InputFileItemViewModel notIncluded = CreateItem(
            "NotIncluded.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: false);

        viewModel.LoadFiles([included, notIncluded]);

        SelectFacet(viewModel, FileListFacet.Included);

        Assert.True(viewModel.HasActiveFilters);
        Assert.True(viewModel.ClearFileFiltersCommand.CanExecute(null));
        Assert.Same(included, Assert.Single(viewModel.FilesView.Cast<InputFileItemViewModel>()));
    }

    [Fact]
    public void FilterCounters_Should_Count_Main_File_Facets()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        InputFileItemViewModel included = CreateItem("Included.cs");

        InputFileItemViewModel notIncludedWithoutReason = CreateItem(
            "ManualOff.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: false);

        InputFileItemViewModel profileRuleExcluded = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel notApplied = CreateItem(
            "Dirty.cs",
            automaticIncluded: true,
            currentIncluded: false,
            appliedIncluded: true);

        InputFileItemViewModel fallback = CreateItem(
            "Unknown.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

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

        viewModel.LoadFiles(
        [
            included,
            notIncludedWithoutReason,
            profileRuleExcluded,
            notApplied,
            fallback,
            disabledType,
            unsupported
        ]);

        Assert.Equal(2, GetCount(viewModel, FileListFacet.Included));
        Assert.Equal(5, GetCount(viewModel, FileListFacet.NotIncluded));
        Assert.Equal(2, GetCount(viewModel, FileListFacet.Overridden));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.NotApplied));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.ExcludedByProfileRule));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.DisabledType));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.Unsupported));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.Fallback));
    }

    [Fact]
    public void FilterCounters_Should_Separate_NotIncluded_By_SkipReason_Facets()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        viewModel.LoadFiles(
        [
            CreateItem(
                "ManualOff.cs",
                automaticIncluded: true,
                currentIncluded: false,
                appliedIncluded: false),

            CreateItem(
                "Generated.g.cs",
                automaticIncluded: false,
                currentIncluded: false,
                appliedIncluded: false,
                skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule.")),

            CreateItem(
                "Disabled.json",
                extension: ".json",
                kind: FileKind.Json,
                automaticIncluded: false,
                currentIncluded: false,
                appliedIncluded: false,
                skipReason: new SkipReason(
                    "discovery.file-type-disabled",
                    "File type is disabled in the current profile."),
                isMergeCandidate: false),

            CreateItem(
                "Unsupported.bin",
                extension: ".bin",
                kind: FileKind.Unknown,
                automaticIncluded: false,
                currentIncluded: false,
                appliedIncluded: false,
                skipReason: new SkipReason(
                    "discovery.unsupported-file-type",
                    "File type is not supported by the current profile."),
                isMergeCandidate: false)
        ]);

        Assert.Equal(4, GetCount(viewModel, FileListFacet.NotIncluded));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.ExcludedByProfileRule));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.DisabledType));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.Unsupported));
    }

    [Fact]
    public void FilterCounters_Should_Respect_Active_Facets_From_Other_Groups()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        viewModel.LoadFiles(
        [
            CreateItem(
                "Generated.g.cs",
                automaticIncluded: false,
                currentIncluded: false,
                appliedIncluded: false,
                skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule.")),

            CreateItem(
                "Unsupported.bin",
                extension: ".bin",
                kind: FileKind.Unknown,
                automaticIncluded: false,
                currentIncluded: false,
                appliedIncluded: false,
                skipReason: new SkipReason(
                    "discovery.unsupported-file-type",
                    "File type is not supported by the current profile."),
                isMergeCandidate: false),

            CreateItem(
                "Included.custom",
                extension: ".custom",
                kind: FileKind.Text,
                isFallbackText: true)
        ]);

        SelectFacet(viewModel, FileListFacet.NotIncluded);

        Assert.Equal(1, GetCount(viewModel, FileListFacet.ExcludedByProfileRule));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.Unsupported));
        Assert.Equal(0, GetCount(viewModel, FileListFacet.Fallback));
    }

    [Fact]
    public void FilterCounters_Should_Ignore_Selected_Facets_From_Own_Group()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        InputFileItemViewModel profileRuleExcluded = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

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

        viewModel.LoadFiles([profileRuleExcluded, unsupported]);

        SelectFacet(viewModel, FileListFacet.Unsupported);

        Assert.Equal(1, GetCount(viewModel, FileListFacet.ExcludedByProfileRule));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.Unsupported));
        Assert.Same(unsupported, Assert.Single(viewModel.FilesView.Cast<InputFileItemViewModel>()));
    }

    [Fact]
    public void FilterCounters_Should_Respect_Multiple_Active_Groups()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

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

        InputFileItemViewModel pendingUnsupported = CreateItem(
            "Pending.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: true,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        viewModel.LoadFiles([pendingProfileRule, appliedProfileRule, pendingUnsupported]);

        SelectFacet(viewModel, FileListFacet.NotIncluded);
        SelectFacet(viewModel, FileListFacet.Unsupported);
        SelectFacet(viewModel, FileListFacet.NotApplied);

        Assert.Equal(0, GetCount(viewModel, FileListFacet.Included));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.NotIncluded));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.ExcludedByProfileRule));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.Unsupported));
        Assert.Equal(0, GetCount(viewModel, FileListFacet.Overridden));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.NotApplied));
        Assert.Same(pendingUnsupported, Assert.Single(viewModel.FilesView.Cast<InputFileItemViewModel>()));
    }

    [Fact]
    public void FilterCounters_Should_Update_When_SearchText_Changes()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        viewModel.LoadFiles(
        [
            CreateItem("Alpha.cs"),
            CreateItem("Beta.cs"),
            CreateItem("Alpha.custom", extension: ".custom", kind: FileKind.Text, isFallbackText: true)
        ]);

        viewModel.Filters.SearchText = "Alpha";

        Assert.Equal(2, GetCount(viewModel, FileListFacet.Included));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.Fallback));
    }

    [Fact]
    public void FilterCounters_Should_Update_When_ShowOnlySelected_Changes()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        InputFileItemViewModel selected = CreateItem("Selected.cs");
        InputFileItemViewModel unselected = CreateItem("Unselected.cs");

        viewModel.LoadFiles([selected, unselected]);
        viewModel.ReplaceSelectedFiles([selected]);

        viewModel.Filters.ShowOnlySelected = true;

        Assert.Equal(1, GetCount(viewModel, FileListFacet.Included));
        Assert.Equal(0, GetCount(viewModel, FileListFacet.NotIncluded));
    }

    [Fact]
    public void FilterCounters_Should_Update_When_File_Inclusion_Changes()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        InputFileItemViewModel file = CreateItem("File.cs");

        viewModel.LoadFiles([file]);

        file.IsIncluded = false;

        Assert.Equal(0, GetCount(viewModel, FileListFacet.Included));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.NotIncluded));
        Assert.Equal(1, GetCount(viewModel, FileListFacet.Overridden));
    }

    [Fact]
    public void FilterSummary_Should_Update_When_File_State_Changes_Affect_Active_Facet()
    {
        FilesPaneViewModel viewModel = CreateViewModel();
        InputFileItemViewModel file = CreateItem("File.cs");

        viewModel.LoadFiles([file]);
        SelectFacet(viewModel, FileListFacet.Included);

        Assert.Equal("1 of 1 files shown", viewModel.FilterSummary);
        Assert.False(viewModel.ShowFilteredFilesEmptyState);

        file.IsIncluded = false;

        Assert.Equal("0 of 1 files shown", viewModel.FilterSummary);
        Assert.True(viewModel.ShowFilteredFilesEmptyState);
    }

    [Fact]
    public void FilterSummary_Should_Update_For_Search_Facets_And_Selection()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        InputFileItemViewModel alpha = CreateItem("Alpha.cs");
        InputFileItemViewModel beta = CreateItem("Beta.cs");
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

        viewModel.LoadFiles([alpha, beta, unsupported]);
        Assert.Equal("3 of 3 files shown", viewModel.FilterSummary);

        viewModel.Filters.SearchText = "Alpha";
        Assert.Equal("1 of 3 files shown", viewModel.FilterSummary);

        viewModel.Filters.SearchText = string.Empty;
        SelectFacet(viewModel, FileListFacet.NotIncluded);
        Assert.Equal("1 of 3 files shown", viewModel.FilterSummary);

        viewModel.Filters.Reset();
        viewModel.ReplaceSelectedFiles([beta]);
        viewModel.Filters.ShowOnlySelected = true;
        Assert.Equal("1 of 3 files shown", viewModel.FilterSummary);
    }

    [Fact]
    public void FacetCombination_With_NoMatches_Should_Show_FilteredEmptyState()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        viewModel.LoadFiles(
        [
            CreateItem("Included.cs"),
            CreateItem(
                "Unsupported.bin",
                extension: ".bin",
                kind: FileKind.Unknown,
                automaticIncluded: false,
                currentIncluded: false,
                appliedIncluded: false,
                skipReason: new SkipReason(
                    "discovery.unsupported-file-type",
                    "File type is not supported by the current profile."),
                isMergeCandidate: false)
        ]);

        SelectFacet(viewModel, FileListFacet.Included);
        SelectFacet(viewModel, FileListFacet.Unsupported);

        Assert.True(viewModel.HasNoVisibleFiles);
        Assert.True(viewModel.ShowFilteredFilesEmptyState);
        Assert.Equal("0 of 2 files shown", viewModel.FilterSummary);
    }

    [Fact]
    public void ShowOnlySelected_With_NoSelection_Should_Show_FilteredEmptyState()
    {
        FilesPaneViewModel viewModel = CreateViewModel();
        viewModel.LoadFiles([CreateItem("Included.cs")]);

        viewModel.Filters.ShowOnlySelected = true;

        Assert.True(viewModel.HasNoVisibleFiles);
        Assert.True(viewModel.ShowFilteredFilesEmptyState);
        Assert.Equal("0 of 1 files shown", viewModel.FilterSummary);
    }

    [Fact]
    public void ClearFileFiltersCommand_Should_Reset_Filters_And_Restore_Counters()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        viewModel.LoadFiles(
        [
            CreateItem("Alpha.cs"),
            CreateItem("Beta.cs")
        ]);

        viewModel.Filters.SearchText = "Alpha";
        viewModel.Filters.ShowOnlySelected = true;
        SelectFacet(viewModel, FileListFacet.Included);
        SelectFacet(viewModel, FileListFacet.Unsupported);
        SelectFacet(viewModel, FileListFacet.NotApplied);

        Assert.True(viewModel.ShowFilteredFilesEmptyState);
        Assert.Equal("0 of 2 files shown", viewModel.FilterSummary);

        viewModel.ClearFileFiltersCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.Filters.SearchText);
        Assert.False(viewModel.Filters.ShowOnlySelected);
        Assert.False(viewModel.Filters.HasActiveFacets);
        Assert.Equal(0, viewModel.Filters.SelectedFacetCount);
        Assert.Equal("Filters", viewModel.Filters.FacetMenuLabel);
        Assert.All(viewModel.Filters.AllFacets, x => Assert.False(x.IsSelected));
        Assert.Equal(2, GetCount(viewModel, FileListFacet.Included));
        Assert.Equal(2, viewModel.FilesView.Cast<InputFileItemViewModel>().Count());
        Assert.Equal("2 of 2 files shown", viewModel.FilterSummary);
        Assert.False(viewModel.HasActiveFilters);
        Assert.False(viewModel.ShowFilteredFilesEmptyState);
        Assert.False(viewModel.ClearFileFiltersCommand.CanExecute(null));
    }

    [Fact]
    public void CopyRelativePathCommand_Should_Be_Disabled_When_ContextFile_Is_Null()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.CopyRelativePathCommand.CanExecute(null));
    }

    [Fact]
    public void CopyFullPathCommand_Should_Be_Disabled_When_ContextFile_Is_Null()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.CopyFullPathCommand.CanExecute(null));
    }

    [Fact]
    public void CopyRelativePathCommand_Should_Copy_ContextFile_RelativePath()
    {
        var clipboard = new FakeClipboardService();
        FilesPaneViewModel viewModel = CreateViewModel(clipboard);

        InputFileItemViewModel file = CreateItem("src/FileA.cs");
        viewModel.ContextFile = file;

        viewModel.CopyRelativePathCommand.Execute(null);

        Assert.Equal("src/FileA.cs", clipboard.LastText);
        Assert.Equal(1, clipboard.SetTextCallCount);
    }

    [Fact]
    public void CopyFullPathCommand_Should_Copy_ContextFile_FullPath()
    {
        var clipboard = new FakeClipboardService();
        FilesPaneViewModel viewModel = CreateViewModel(clipboard);

        InputFileItemViewModel file = CreateItem("src/FileA.cs");
        viewModel.ContextFile = file;

        viewModel.CopyFullPathCommand.Execute(null);

        Assert.Equal(@"D:\Project\src/FileA.cs", clipboard.LastText);
        Assert.Equal(1, clipboard.SetTextCallCount);
    }

    [Fact]
    public void CopyCommands_Should_Update_CanExecute_When_ContextFile_Changes()
    {
        FilesPaneViewModel viewModel = CreateViewModel();
        int relativeCanExecuteChanged = 0;
        int fullCanExecuteChanged = 0;

        viewModel.CopyRelativePathCommand.CanExecuteChanged += (_, _) => relativeCanExecuteChanged++;
        viewModel.CopyFullPathCommand.CanExecuteChanged += (_, _) => fullCanExecuteChanged++;

        viewModel.ContextFile = CreateItem("src/FileA.cs");

        Assert.True(viewModel.CopyRelativePathCommand.CanExecute(null));
        Assert.True(viewModel.CopyFullPathCommand.CanExecute(null));
        Assert.Equal(1, relativeCanExecuteChanged);
        Assert.Equal(1, fullCanExecuteChanged);
    }

    [Fact]
    public void LoadFiles_Should_Clear_ContextFile()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        viewModel.ContextFile = CreateItem("src/Old.cs");

        viewModel.LoadFiles(
        [
            CreateItem("src/New.cs")
        ]);

        Assert.Null(viewModel.ContextFile);
        Assert.False(viewModel.CopyRelativePathCommand.CanExecute(null));
        Assert.False(viewModel.CopyFullPathCommand.CanExecute(null));
    }


    [Fact]
    public void InclusionCommands_Should_Be_Disabled_When_Only_NonCandidate_File_Is_Selected()
    {
        FilesPaneViewModel viewModel = CreateViewModel();
        InputFileItemViewModel nonCandidate = CreateItem(
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

        viewModel.LoadFiles([nonCandidate]);
        viewModel.ReplaceSelectedFiles([nonCandidate]);

        Assert.False(viewModel.IncludeSelectedFilesCommand.CanExecute(null));
        Assert.False(viewModel.ExcludeSelectedFilesCommand.CanExecute(null));
        Assert.False(viewModel.ResetSelectedFileOverridesCommand.CanExecute(null));
    }

    [Fact]
    public void IncludeSelectedFilesCommand_Should_Ignore_NonCandidate_Files_In_Mixed_Selection()
    {
        FilesPaneViewModel viewModel = CreateViewModel();

        InputFileItemViewModel candidate = CreateItem(
            "Generated.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("filter.rule.exclude", "Excluded by rule."));

        InputFileItemViewModel nonCandidate = CreateItem(
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

        viewModel.LoadFiles([candidate, nonCandidate]);
        viewModel.ReplaceSelectedFiles([candidate, nonCandidate]);

        viewModel.IncludeSelectedFilesCommand.Execute(null);

        Assert.True(candidate.IsIncluded);
        Assert.True(candidate.HasManualOverride);
        Assert.False(nonCandidate.IsIncluded);
        Assert.False(nonCandidate.HasManualOverride);

        FileInclusionOverride inclusionOverride = Assert.Single(viewModel.BuildOverrides());
        Assert.Equal(candidate.FullPath, inclusionOverride.FullPath);
        Assert.True(inclusionOverride.IsIncluded);
    }

    [Fact]
    public void ApplyFiles_Should_Discard_Persisted_Override_For_NonCandidate_File()
    {
        FilesPaneViewModel viewModel = CreateViewModel();
        string fullPath = @"D:\Project\Unsupported.bin";

        viewModel.ApplyOverridesDictionary(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            [fullPath] = true
        });

        Assert.Single(viewModel.BuildOverrides());

        var nonCandidate = new InputFile(
            fullPath: fullPath,
            relativePath: "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        viewModel.ApplyFiles([nonCandidate], []);

        InputFileItemViewModel item = Assert.Single(viewModel.Files);
        Assert.False(item.IsIncluded);
        Assert.False(item.HasManualOverride);
        Assert.False(item.CanOverrideInclusion);
        Assert.Empty(viewModel.BuildOverrides());
        Assert.Empty(viewModel.CaptureOverridesDictionary());
    }

    [Fact]
    public void ApplyOverridesDictionary_Should_Ignore_Override_For_Loaded_NonCandidate_File()
    {
        FilesPaneViewModel viewModel = CreateViewModel();
        InputFileItemViewModel nonCandidate = CreateItem(
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

        viewModel.LoadFiles([nonCandidate]);
        viewModel.ApplyOverridesDictionary(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            [nonCandidate.FullPath] = true
        });

        Assert.False(nonCandidate.IsIncluded);
        Assert.False(nonCandidate.HasManualOverride);
        Assert.Empty(viewModel.BuildOverrides());
    }

    private static FilesPaneViewModel CreateViewModel(
        FakeClipboardService? clipboard = null)
    {
        return new FilesPaneViewModel(clipboard ?? new FakeClipboardService());
    }

    private static void AssertOption(
        FileListFilterOptionViewModel option,
        FileListFacet expectedFacet,
        string expectedLabel)
    {
        Assert.Equal(expectedFacet, option.Facet);
        Assert.Equal(expectedLabel, option.Label);
        Assert.Equal($"{expectedLabel} (0)", option.DisplayText);
        Assert.False(option.IsSelected);
    }

    private static int GetCount(
        FilesPaneViewModel viewModel,
        FileListFacet facet)
    {
        return viewModel.Filters.AllFacets.Single(x => x.Facet == facet).Count;
    }

    private static void SelectFacet(
        FilesPaneViewModel viewModel,
        FileListFacet facet)
    {
        viewModel.Filters.AllFacets.Single(x => x.Facet == facet).IsSelected = true;
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

    private static InputFileItemViewModel CreateFile(
        string relativePath,
        bool automaticIncluded = true,
        bool currentIncluded = true,
        bool appliedIncluded = true)
    {
        var model = new InputFile(
            fullPath: $@"D:\Project\{relativePath}",
            relativePath: relativePath,
            extension: Path.GetExtension(relativePath),
            kind: FileKind.CSharp,
            isIncluded: automaticIncluded);

        return new InputFileItemViewModel(
            model: model,
            automaticIncluded: automaticIncluded,
            currentIncluded: currentIncluded,
            appliedIncluded: appliedIncluded);
    }
}