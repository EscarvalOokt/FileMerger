using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Integration;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Files.ViewModels;

public sealed class FilesPaneViewModel : ViewModelBase
{
    private readonly FileListFiltersViewModel _filters = new();
    private readonly IClipboardService _clipboardService;

    private readonly Dictionary<string, bool> _manualInclusionOverrides =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _isUpdatingFileInclusion;
    private bool _isLoadingFiles;
    private InputFileItemViewModel? _contextFile;
    private string _filterSummary = "0 of 0 files shown";

    public FilesPaneViewModel(IClipboardService clipboardService)
    {
        ArgumentNullException.ThrowIfNull(clipboardService);

        _clipboardService = clipboardService;

        Files = [];
        SelectedFiles = [];

        FilesView = CollectionViewSource.GetDefaultView(Files);
        FilesView.Filter = FilterFile;

        _filters.PropertyChanged += Filters_PropertyChanged;
        Files.CollectionChanged += Files_CollectionChanged;

        ClearFileFiltersCommand = new RelayCommand(
            ClearFileFilters,
            CanClearFileFilters);

        ResetAllFileOverridesCommand = new RelayCommand(
            ResetAllFileOverrides,
            () => Files.Any(x => x.HasManualOverride));

        IncludeSelectedFilesCommand = new RelayCommand(
            IncludeSelectedFiles,
            () => SelectedFiles.Any(x => x.CanOverrideInclusion));

        ExcludeSelectedFilesCommand = new RelayCommand(
            ExcludeSelectedFiles,
            () => SelectedFiles.Any(x => x.CanOverrideInclusion));

        ResetSelectedFileOverridesCommand = new RelayCommand(
            ResetSelectedFileOverrides,
            () => SelectedFiles.Any(x => x.HasManualOverride));

        CopyRelativePathCommand = new RelayCommand(
            CopyRelativePath,
            CanCopyRelativePath);

        CopyFullPathCommand = new RelayCommand(
            CopyFullPath,
            CanCopyFullPath);
    }

    public event EventHandler? FileOverridesChanged;

    public ObservableCollection<InputFileItemViewModel> Files { get; }
    public ObservableCollection<InputFileItemViewModel> SelectedFiles { get; }

    public InputFileItemViewModel? ContextFile
    {
        get => _contextFile;
        set
        {
            if (!SetProperty(ref _contextFile, value))
                return;

            CopyRelativePathCommand.RaiseCanExecuteChanged();
            CopyFullPathCommand.RaiseCanExecuteChanged();
        }
    }

    public ICollectionView FilesView { get; }
    public FileListFiltersViewModel Filters => _filters;

    public bool HasFiles => Files.Count > 0;
    public bool HasNoFiles => !HasFiles;
    public bool HasVisibleFiles => !FilesView.IsEmpty;
    public bool HasNoVisibleFiles => HasFiles && FilesView.IsEmpty;

    public bool HasActiveFilters => Filters.HasActiveFilters;
    public bool ShowFilteredFilesEmptyState => HasFiles && HasActiveFilters && HasNoVisibleFiles;

    public string FilterSummary
    {
        get => _filterSummary;
        private set => SetProperty(ref _filterSummary, value);
    }

    public string EmptyFilteredFilesTitle => "No files match the current filters.";

    public string EmptyFilteredFilesDescription =>
        "Adjust the active search, selection, or facet filters, or clear all filters to show discovered files.";

    public RelayCommand ClearFileFiltersCommand { get; }
    public RelayCommand ResetAllFileOverridesCommand { get; }
    public RelayCommand IncludeSelectedFilesCommand { get; }
    public RelayCommand ExcludeSelectedFilesCommand { get; }
    public RelayCommand ResetSelectedFileOverridesCommand { get; }
    public RelayCommand CopyRelativePathCommand { get; }
    public RelayCommand CopyFullPathCommand { get; }

    public void ReplaceSelectedFiles(IEnumerable<InputFileItemViewModel> selectedItems)
    {
        ArgumentNullException.ThrowIfNull(selectedItems);

        SelectedFiles.Clear();
        foreach (InputFileItemViewModel file in selectedItems)
            SelectedFiles.Add(file);

        RaiseBulkCommandsCanExecuteChanged();
        RefreshFilesView();
    }

    public void ApplyFiles(
        IReadOnlyCollection<InputFile> automaticFiles,
        IReadOnlyCollection<InputFile> currentFiles,
        IReadOnlyDictionary<string, bool>? appliedInclusionState = null)
    {
        ArgumentNullException.ThrowIfNull(automaticFiles);
        ArgumentNullException.ThrowIfNull(currentFiles);

        var automaticMap = automaticFiles
            .GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var currentMap = currentFiles
            .GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        string[] orderedPaths =
        [
            .. automaticMap.Keys
                .Union(currentMap.Keys, StringComparer.OrdinalIgnoreCase)
                .Union(appliedInclusionState?.Keys ?? [], StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        ];

        if (orderedPaths.Length > 0)
        {
            var overridablePaths = automaticMap.Values
                .Where(x => x.IsMergeCandidate)
                .Select(x => x.FullPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string staleOrNonCandidatePath in _manualInclusionOverrides.Keys
                         .Except(overridablePaths, StringComparer.OrdinalIgnoreCase)
                         .ToArray())
            {
                _manualInclusionOverrides.Remove(staleOrNonCandidatePath);
            }
        }

        List<InputFileItemViewModel> items = [];

        foreach (string path in orderedPaths)
        {
            if (!automaticMap.TryGetValue(path, out InputFile? automaticFile))
                continue;

            bool automaticIncluded = automaticFile.IsIncluded;

            bool currentIncluded = automaticFile.IsMergeCandidate
                ? _manualInclusionOverrides.TryGetValue(path, out bool manualValue)
                    ? manualValue
                    : currentMap.TryGetValue(path, out InputFile? currentFile)
                        ? currentFile.IsIncluded
                        : automaticIncluded
                : automaticIncluded;

            bool appliedIncluded = automaticFile.IsMergeCandidate
                ? appliedInclusionState is not null &&
                  appliedInclusionState.TryGetValue(path, out bool appliedValue)
                    ? appliedValue
                    : currentMap.TryGetValue(path, out InputFile? currentFileForApplied)
                        ? currentFileForApplied.IsIncluded
                        : automaticIncluded
                : automaticIncluded;

            InputFileItemViewModel item = new(
                model: automaticFile,
                automaticIncluded: automaticIncluded,
                currentIncluded: currentIncluded,
                appliedIncluded: appliedIncluded)
            {
                HasManualOverride = currentIncluded != automaticIncluded,
                IsAppliedInPreview = currentIncluded == appliedIncluded
            };

            items.Add(item);
        }

        LoadFiles(items);
    }

    public void LoadFiles(IEnumerable<InputFileItemViewModel> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        ContextFile = null;

        UnsubscribeFromFileItemEvents();

        _isLoadingFiles = true;

        try
        {
            Files.Clear();

            foreach (InputFileItemViewModel file in files)
                Files.Add(file);
        }
        finally
        {
            _isLoadingFiles = false;
        }

        SubscribeToFileItemEvents();
        RaiseBulkCommandsCanExecuteChanged();
        RefreshFilesView();
    }

    public IReadOnlyCollection<FileInclusionOverride> BuildOverrides()
    {
        HashSet<string> nonCandidatePaths = GetKnownNonCandidatePaths();

        return
        [
            .. _manualInclusionOverrides
                .Where(x => !nonCandidatePaths.Contains(x.Key))
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => new FileInclusionOverride(x.Key, x.Value))
        ];
    }

    public Dictionary<string, bool> CaptureOverridesDictionary()
    {
        HashSet<string> nonCandidatePaths = GetKnownNonCandidatePaths();

        return _manualInclusionOverrides
            .Where(x => !nonCandidatePaths.Contains(x.Key))
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    public void ApplyOverridesDictionary(IReadOnlyDictionary<string, bool> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        _manualInclusionOverrides.Clear();

        foreach (KeyValuePair<string, bool> pair in overrides)
        {
            InputFileItemViewModel? file = Files.FirstOrDefault(x =>
                string.Equals(x.FullPath, pair.Key, StringComparison.OrdinalIgnoreCase));

            if (file is not null && !file.CanOverrideInclusion)
                continue;

            _manualInclusionOverrides[pair.Key] = pair.Value;
        }

        RunFileInclusionUpdate(() =>
        {
            foreach (InputFileItemViewModel file in Files)
            {
                bool currentIncluded =
                    _manualInclusionOverrides.TryGetValue(file.FullPath, out bool manualValue)
                        ? manualValue
                        : file.AutomaticIncluded;

                file.IsIncluded = currentIncluded;
                file.HasManualOverride = currentIncluded != file.AutomaticIncluded;
                file.IsAppliedInPreview = currentIncluded == file.AppliedIncluded;
            }
        });

        ResetAllFileOverridesCommand.RaiseCanExecuteChanged();
        RaiseBulkCommandsCanExecuteChanged();
        RefreshFilesView();
        FileOverridesChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyDictionary<string, bool> BuildCurrentInclusionState()
    {
        return Files.ToDictionary(
            x => x.FullPath,
            x => x.IsIncluded,
            StringComparer.OrdinalIgnoreCase);
    }

    public void ResetSelections()
    {
        SelectedFiles.Clear();
        RaiseBulkCommandsCanExecuteChanged();
        RefreshFilesView();
    }

    private HashSet<string> GetKnownNonCandidatePaths()
    {
        return Files
            .Where(x => !x.IsMergeCandidate)
            .Select(x => x.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private bool FilterFile(object obj)
    {
        if (obj is not InputFileItemViewModel file)
            return false;

        return _filters.Matches(file, SelectedFiles);
    }

    private void ClearFileFilters()
    {
        _filters.Reset();
        RefreshFilesView();
    }

    private bool CanClearFileFilters()
    {
        return _filters.HasActiveFilters;
    }

    private void RefreshFilesView()
    {
        RefreshFilterCounters();
        FilesView.Refresh();
        RefreshFilterSummary();
        RefreshEmptyStateProperties();
        ClearFileFiltersCommand.RaiseCanExecuteChanged();
    }

    private void RefreshFilterSummary()
    {
        int visibleCount = FilesView.Cast<InputFileItemViewModel>().Count();
        FilterSummary = $"{visibleCount:N0} of {Files.Count:N0} files shown";
    }

    private void RefreshEmptyStateProperties()
    {
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(HasNoFiles));
        OnPropertyChanged(nameof(HasVisibleFiles));
        OnPropertyChanged(nameof(HasNoVisibleFiles));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(ShowFilteredFilesEmptyState));
    }

    private void RefreshFilterCounters()
    {
        foreach (FileListFilterOptionViewModel facet in _filters.AllFacets)
        {
            int count = Files.Count(file =>
                _filters.MatchesForFacetCount(file, SelectedFiles, facet.Facet));

            facet.UpdateCount(count);
        }
    }

    private void IncludeSelectedFiles()
    {
        SetSelectedFilesInclusion(true);
    }

    private void ExcludeSelectedFiles()
    {
        SetSelectedFilesInclusion(false);
    }

    private void SetSelectedFilesInclusion(bool isIncluded)
    {
        if (SelectedFiles.Count == 0)
            return;

        InputFileItemViewModel[] selectedFiles =
            [.. SelectedFiles.Where(x => x.CanOverrideInclusion)];

        if (selectedFiles.Length == 0)
            return;

        RunFileInclusionUpdate(() =>
        {
            foreach (InputFileItemViewModel file in selectedFiles)
                ApplyFileInclusion(file, isIncluded);
        });

        FinishFileInclusionUpdate();
    }

    private void ResetSelectedFileOverrides()
    {
        if (SelectedFiles.Count == 0)
            return;

        InputFileItemViewModel[] selectedFiles = [.. SelectedFiles];

        RunFileInclusionUpdate(() =>
        {
            foreach (InputFileItemViewModel file in selectedFiles)
            {
                _manualInclusionOverrides.Remove(file.FullPath);
                file.ResetOverride();
                file.IsAppliedInPreview = file.IsIncluded == file.AppliedIncluded;
            }
        });

        FinishFileInclusionUpdate();
    }

    private bool CanCopyRelativePath()
    {
        return !string.IsNullOrWhiteSpace(ContextFile?.RelativePath);
    }

    private bool CanCopyFullPath()
    {
        return !string.IsNullOrWhiteSpace(ContextFile?.FullPath);
    }

    private void CopyRelativePath()
    {
        if (!CanCopyRelativePath())
            return;

        _clipboardService.SetText(ContextFile!.RelativePath);
    }

    private void CopyFullPath()
    {
        if (!CanCopyFullPath())
            return;

        _clipboardService.SetText(ContextFile!.FullPath);
    }

    private void RunFileInclusionUpdate(Action update)
    {
        ArgumentNullException.ThrowIfNull(update);

        _isUpdatingFileInclusion = true;

        try
        {
            update();
        }
        finally
        {
            _isUpdatingFileInclusion = false;
        }
    }

    private void ApplyFileInclusion(
        InputFileItemViewModel file,
        bool isIncluded)
    {
        if (!file.CanOverrideInclusion)
            return;

        file.IsIncluded = isIncluded;

        bool hasManualOverride = isIncluded != file.AutomaticIncluded;

        if (hasManualOverride)
            _manualInclusionOverrides[file.FullPath] = isIncluded;
        else
            _manualInclusionOverrides.Remove(file.FullPath);

        file.HasManualOverride = hasManualOverride;
        file.IsAppliedInPreview = isIncluded == file.AppliedIncluded;
    }

    private void FinishFileInclusionUpdate()
    {
        ResetAllFileOverridesCommand.RaiseCanExecuteChanged();
        RaiseBulkCommandsCanExecuteChanged();
        RefreshFilesView();
        FileOverridesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResetAllFileOverrides()
    {
        if (!Files.Any(x => x.HasManualOverride))
            return;

        _manualInclusionOverrides.Clear();

        RunFileInclusionUpdate(() =>
        {
            foreach (InputFileItemViewModel file in Files)
            {
                file.ResetOverride();
                file.IsAppliedInPreview = file.IsIncluded == file.AppliedIncluded;
            }
        });

        RaiseBulkCommandsCanExecuteChanged();
        RefreshFilesView();
        FileOverridesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Files_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isLoadingFiles)
            return;

        if (e.OldItems is not null &&
            ContextFile is not null &&
            e.OldItems.Contains(ContextFile))
        {
            ContextFile = null;
        }

        if (e.OldItems is not null)
        {
            foreach (InputFileItemViewModel item in e.OldItems)
                item.PropertyChanged -= FileItem_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (InputFileItemViewModel item in e.NewItems)
                item.PropertyChanged += FileItem_PropertyChanged;
        }

        ResetAllFileOverridesCommand.RaiseCanExecuteChanged();
        RaiseBulkCommandsCanExecuteChanged();
        RefreshFilesView();
    }

    private void FileItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not InputFileItemViewModel file)
            return;

        if (_isUpdatingFileInclusion)
            return;

        if (e.PropertyName == nameof(InputFileItemViewModel.IsIncluded))
        {
            bool hasManualOverride = file.IsIncluded != file.AutomaticIncluded;

            if (hasManualOverride)
                _manualInclusionOverrides[file.FullPath] = file.IsIncluded;
            else
                _manualInclusionOverrides.Remove(file.FullPath);

            file.HasManualOverride = hasManualOverride;
            file.IsAppliedInPreview = file.IsIncluded == file.AppliedIncluded;

            ResetAllFileOverridesCommand.RaiseCanExecuteChanged();
            RaiseBulkCommandsCanExecuteChanged();
            RefreshFilesView();
            FileOverridesChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (e.PropertyName is nameof(InputFileItemViewModel.HasManualOverride) or
            nameof(InputFileItemViewModel.IsAppliedInPreview))
        {
            ResetAllFileOverridesCommand.RaiseCanExecuteChanged();
            RaiseBulkCommandsCanExecuteChanged();
            RefreshFilesView();
        }
    }

    private void Filters_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FileListFiltersViewModel.SearchText) or
            nameof(FileListFiltersViewModel.ShowOnlySelected) or
            nameof(FileListFiltersViewModel.HasActiveFacets))
        {
            RefreshFilesView();
        }
    }

    private void SubscribeToFileItemEvents()
    {
        foreach (InputFileItemViewModel item in Files)
            item.PropertyChanged += FileItem_PropertyChanged;
    }

    private void UnsubscribeFromFileItemEvents()
    {
        foreach (InputFileItemViewModel item in Files)
            item.PropertyChanged -= FileItem_PropertyChanged;
    }

    private void RaiseBulkCommandsCanExecuteChanged()
    {
        IncludeSelectedFilesCommand.RaiseCanExecuteChanged();
        ExcludeSelectedFilesCommand.RaiseCanExecuteChanged();
        ResetSelectedFileOverridesCommand.RaiseCanExecuteChanged();
    }
}