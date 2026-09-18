using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Common;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Sources.Dialogs;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Sources.ViewModels;

public sealed class SourcesPaneViewModel : ViewModelBase
{
    private readonly IFolderBrowserService _folderBrowserService;
    private readonly IOpenFileDialogService _openFileDialogService;
    private readonly ISourceDetailsDialogService _sourceDetailsDialogService;
    private MergeSourceItemViewModel? _selectedSource;

    public SourcesPaneViewModel(
        IFolderBrowserService folderBrowserService,
        IOpenFileDialogService openFileDialogService,
        ISourceDetailsDialogService sourceDetailsDialogService)
    {
        ArgumentNullException.ThrowIfNull(folderBrowserService);
        ArgumentNullException.ThrowIfNull(openFileDialogService);
        ArgumentNullException.ThrowIfNull(sourceDetailsDialogService);

        _folderBrowserService = folderBrowserService;
        _openFileDialogService = openFileDialogService;
        _sourceDetailsDialogService = sourceDetailsDialogService;

        Sources = [];
        SelectedSources = [];

        Sources.CollectionChanged += Sources_CollectionChanged;

        AddFolderSourceCommand = new RelayCommand(AddFolderSource);
        AddFileSourceCommand = new RelayCommand(AddFileSource);

        RemoveSourceCommand = new RelayCommand(RemoveSelectedSource, () => SelectedSource is not null);

        RemoveSelectedSourcesCommand = new RelayCommand(RemoveSelectedSources, () => SelectedSources.Count > 0);

        EnableSelectedSourcesCommand = new RelayCommand(EnableSelectedSources, () => SelectedSources.Count > 0);

        DisableSelectedSourcesCommand = new RelayCommand(DisableSelectedSources, () => SelectedSources.Count > 0);

        OpenSourceDetailsCommand = new RelayCommand(OpenSourceDetails, () => SelectedSource is not null);
    }

    public ObservableCollection<MergeSourceItemViewModel> Sources { get; }
    public ObservableCollection<MergeSourceItemViewModel> SelectedSources { get; }

    public bool HasSources => Sources.Count > 0;

    public MergeSourceItemViewModel? SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (!SetProperty(ref _selectedSource, value))
                return;

            RemoveSourceCommand.RaiseCanExecuteChanged();
            OpenSourceDetailsCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand AddFolderSourceCommand { get; }
    public RelayCommand AddFileSourceCommand { get; }
    public RelayCommand RemoveSourceCommand { get; }
    public RelayCommand RemoveSelectedSourcesCommand { get; }
    public RelayCommand EnableSelectedSourcesCommand { get; }
    public RelayCommand DisableSelectedSourcesCommand { get; }
    public RelayCommand OpenSourceDetailsCommand { get; }

    public event EventHandler? SourcesChanged;

    public void ReplaceSelectedSources(IEnumerable<MergeSourceItemViewModel> selectedItems)
    {
        ArgumentNullException.ThrowIfNull(selectedItems);

        MergeSourceItemViewModel[] selected = [.. selectedItems];

        SelectedSources.Clear();
        foreach (MergeSourceItemViewModel item in selected)
            SelectedSources.Add(item);

        if (selected.Length == 0)
        {
            SelectedSource = null;
        }
        else if (SelectedSource is null || !selected.Contains(SelectedSource))
        {
            SelectedSource = selected[0];
        }

        RaiseBulkCommandsCanExecuteChanged();
    }

    public IReadOnlyCollection<MergeSource> BuildSources()
    {
        return [.. Sources.Select(x => x.ToModel())];
    }

    public IReadOnlyCollection<MergeSourceStateSnapshot> BuildStateSnapshots()
    {
        return
        [
            .. Sources.Select(x => new MergeSourceStateSnapshot(
                Path: PathUtility.NormalizeForComparison(x.Path),
                Type: x.Type,
                IsRecursive: x.CanEditRecursive && x.IsRecursive,
                IsEnabled: x.IsEnabled,
                Exclusions:
                [
                    .. x.Exclusions.Select(e => new MergeSourceExclusionStateSnapshot(
                            RelativePath: NormalizeRelativePathForComparison(e.RelativePath),
                            Type: e.Type,
                            IsEnabled: e.IsEnabled))
                        .OrderBy(e => e.Type)
                        .ThenBy(e => e.RelativePath, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(e => e.IsEnabled)
                ]))
        ];
    }

    public void LoadSources(IEnumerable<MergeSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        UnsubscribeFromSourceItemEvents();

        Sources.Clear();
        SelectedSources.Clear();
        SelectedSource = null;

        foreach (MergeSource source in sources)
            Sources.Add(new MergeSourceItemViewModel(source));

        RaiseBulkCommandsCanExecuteChanged();
        RemoveSourceCommand.RaiseCanExecuteChanged();
        OpenSourceDetailsCommand.RaiseCanExecuteChanged();
        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddFolderSource()
    {
        IReadOnlyCollection<string>? selectedPaths = _folderBrowserService.SelectFolders();
        if (selectedPaths is null || selectedPaths.Count == 0)
            return;

        int addedCount = 0;

        foreach (string path in selectedPaths.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            bool alreadyExists = Sources.Any(x =>
                x.Type == MergeSourceType.Directory && PathUtility.PathEquals(x.Path, path));

            if (alreadyExists)
                continue;

            MergeSourceItemViewModel item = new(
                new MergeSource(path, MergeSourceType.Directory, isRecursive: true, isEnabled: true));

            Sources.Add(item);

            SelectedSource ??= item;

            addedCount++;
        }

        if (addedCount > 0)
            SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddFileSource()
    {
        IReadOnlyCollection<string>? selectedPaths = _openFileDialogService.SelectFiles();
        if (selectedPaths is null || selectedPaths.Count == 0)
            return;

        int addedCount = 0;

        foreach (string path in selectedPaths.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            bool alreadyExists =
                Sources.Any(x => x.Type == MergeSourceType.File && PathUtility.PathEquals(x.Path, path));

            if (alreadyExists)
                continue;

            MergeSourceItemViewModel item = new(
                new MergeSource(path, MergeSourceType.File, isRecursive: false, isEnabled: true));

            Sources.Add(item);

            SelectedSource ??= item;

            addedCount++;
        }

        if (addedCount > 0)
            SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OpenSourceDetails()
    {
        if (SelectedSource is null)
            return;

        _sourceDetailsDialogService.Show(SelectedSource);
    }

    private void RemoveSelectedSource()
    {
        if (SelectedSource is null)
            return;

        Sources.Remove(SelectedSource);
        SelectedSource = null;

        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RemoveSelectedSources()
    {
        if (SelectedSources.Count == 0)
            return;

        MergeSourceItemViewModel[] items = [.. SelectedSources];

        foreach (MergeSourceItemViewModel item in items)
            Sources.Remove(item);

        SelectedSources.Clear();

        RaiseBulkCommandsCanExecuteChanged();

        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnableSelectedSources()
    {
        if (SelectedSources.Count == 0)
            return;

        foreach (MergeSourceItemViewModel item in SelectedSources)
            item.IsEnabled = true;

        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DisableSelectedSources()
    {
        if (SelectedSources.Count == 0)
            return;

        foreach (MergeSourceItemViewModel item in SelectedSources)
            item.IsEnabled = false;

        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Sources_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (MergeSourceItemViewModel item in e.OldItems)
                item.PropertyChanged -= SourceItem_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (MergeSourceItemViewModel item in e.NewItems)
                item.PropertyChanged += SourceItem_PropertyChanged;
        }

        if (SelectedSource is not null && !Sources.Contains(SelectedSource))
            SelectedSource = null;

        OnPropertyChanged(nameof(HasSources));

        RaiseBulkCommandsCanExecuteChanged();
        RemoveSourceCommand.RaiseCanExecuteChanged();
        OpenSourceDetailsCommand.RaiseCanExecuteChanged();
    }

    private void SourceItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        bool affectsSources = e.PropertyName is nameof(MergeSourceItemViewModel.Path)
            or nameof(MergeSourceItemViewModel.IsRecursive)
            or nameof(MergeSourceItemViewModel.IsEnabled)
            or nameof(MergeSourceItemViewModel.Exclusions)
            or nameof(MergeSourceItemViewModel.ExclusionCount)
            or nameof(MergeSourceItemViewModel.ExclusionSummary);

        if (!affectsSources)
            return;

        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UnsubscribeFromSourceItemEvents()
    {
        foreach (MergeSourceItemViewModel item in Sources)
            item.PropertyChanged -= SourceItem_PropertyChanged;
    }

    private void RaiseBulkCommandsCanExecuteChanged()
    {
        RemoveSelectedSourcesCommand.RaiseCanExecuteChanged();
        EnableSelectedSourcesCommand.RaiseCanExecuteChanged();
        DisableSelectedSourcesCommand.RaiseCanExecuteChanged();
    }

    private static string NormalizeRelativePathForComparison(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        return relativePath.Trim()
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);
    }
}