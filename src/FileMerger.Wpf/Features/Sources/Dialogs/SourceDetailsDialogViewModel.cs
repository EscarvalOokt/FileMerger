using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Common;
using FileMerger.Wpf.Features.Sources.ViewModels;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Sources.Dialogs;

public sealed class SourceDetailsDialogViewModel : ViewModelBase
{
    private readonly IFolderBrowserService _folderBrowserService;
    private readonly IOpenFileDialogService _openFileDialogService;

    public SourceDetailsDialogViewModel(
        MergeSourceItemViewModel source,
        IFolderBrowserService folderBrowserService,
        IOpenFileDialogService openFileDialogService)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(folderBrowserService);
        ArgumentNullException.ThrowIfNull(openFileDialogService);

        Source = source;
        _folderBrowserService = folderBrowserService;
        _openFileDialogService = openFileDialogService;

        SelectedExclusions = [];

        Source.PropertyChanged += Source_PropertyChanged;
        Source.Exclusions.CollectionChanged += Exclusions_CollectionChanged;
        SelectedExclusions.CollectionChanged += SelectedExclusions_CollectionChanged;

        AddExcludedFolderCommand = new RelayCommand(AddExcludedFolder, () => CanAddExclusions);

        AddExcludedFileCommand = new RelayCommand(AddExcludedFile, () => CanAddExclusions);

        RemoveSelectedExclusionsCommand = new RelayCommand(
            RemoveSelectedExclusions,
            () => CanEditExclusions && SelectedExclusions.Count > 0);

        ClearExclusionsCommand = new RelayCommand(
            ClearExclusions,
            () => CanEditExclusions && Source.Exclusions.Count > 0);

        EnableSelectedExclusionsCommand = new RelayCommand(
            EnableSelectedExclusions,
            () => CanEditExclusions && SelectedExclusions.Any(x => !x.IsEnabled));

        DisableSelectedExclusionsCommand = new RelayCommand(
            DisableSelectedExclusions,
            () => CanEditExclusions && SelectedExclusions.Any(x => x.IsEnabled));

        EnableAllExclusionsCommand = new RelayCommand(
            EnableAllExclusions,
            () => CanEditExclusions && Source.Exclusions.Any(x => !x.IsEnabled));

        DisableAllExclusionsCommand = new RelayCommand(
            DisableAllExclusions,
            () => CanEditExclusions && Source.Exclusions.Any(x => x.IsEnabled));
    }

    public MergeSourceItemViewModel Source { get; }

    public ObservableCollection<MergeSourceExclusionItemViewModel> SelectedExclusions { get; }

    public bool CanEditExclusions => Source.Type == MergeSourceType.Directory;

    public bool CanAddExclusions =>
        CanEditExclusions && !string.IsNullOrWhiteSpace(Source.Path) && Directory.Exists(Source.Path);

    public bool HasExclusions => Source.Exclusions.Count > 0;

    public string SourceExclusionsHintText
    {
        get
        {
            if (Source.Type != MergeSourceType.Directory)
                return "File sources do not support source-specific exclusions.";

            if (!Directory.Exists(Source.Path))
                return "Source folder must exist before exclusions can be selected.";

            return "Excluded folders and files are skipped during discovery for this source.";
        }
    }

    public RelayCommand AddExcludedFolderCommand { get; }
    public RelayCommand AddExcludedFileCommand { get; }
    public RelayCommand RemoveSelectedExclusionsCommand { get; }
    public RelayCommand ClearExclusionsCommand { get; }
    public RelayCommand EnableSelectedExclusionsCommand { get; }
    public RelayCommand DisableSelectedExclusionsCommand { get; }
    public RelayCommand EnableAllExclusionsCommand { get; }
    public RelayCommand DisableAllExclusionsCommand { get; }

    public void ReplaceSelectedExclusions(IEnumerable<MergeSourceExclusionItemViewModel> selectedItems)
    {
        ArgumentNullException.ThrowIfNull(selectedItems);

        SelectedExclusions.Clear();
        foreach (MergeSourceExclusionItemViewModel item in selectedItems)
            SelectedExclusions.Add(item);

        RaiseExclusionCommandsCanExecuteChanged();
    }

    private void AddExcludedFolder()
    {
        IReadOnlyCollection<string>? selectedPaths = _folderBrowserService.SelectFolders(Source.Path);
        if (selectedPaths is null || selectedPaths.Count == 0)
            return;

        AddExclusions(selectedPaths, MergeSourceExclusionType.Directory);
    }

    private void AddExcludedFile()
    {
        IReadOnlyCollection<string>? selectedPaths = _openFileDialogService.SelectFiles(
            initialPath: Source.Path,
            filter: "All files|*.*");

        if (selectedPaths is null || selectedPaths.Count == 0)
            return;

        AddExclusions(selectedPaths, MergeSourceExclusionType.File);
    }

    private void AddExclusions(IEnumerable<string> selectedPaths, MergeSourceExclusionType type)
    {
        if (Source.Type != MergeSourceType.Directory)
            return;

        string? normalizedRoot = PathUtility.TryNormalize(Source.Path);
        if (string.IsNullOrWhiteSpace(normalizedRoot))
            return;

        int addedCount = 0;

        foreach (string selectedPath in selectedPaths.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            string? normalizedSelectedPath = PathUtility.TryNormalize(selectedPath);
            if (string.IsNullOrWhiteSpace(normalizedSelectedPath))
                continue;

            if (PathUtility.PathEquals(normalizedRoot, normalizedSelectedPath))
                continue;

            if (!PathUtility.IsPathInsideDirectory(normalizedSelectedPath, normalizedRoot))
                continue;

            string relativePath = Path.GetRelativePath(normalizedRoot, normalizedSelectedPath);

            if (HasDuplicateExclusion(Source, relativePath, type))
                continue;

            MergeSourceExclusion exclusion;
            try
            {
                exclusion = new MergeSourceExclusion(relativePath, type, isEnabled: true);
            }
            catch (ArgumentException)
            {
                continue;
            }

            Source.Exclusions.Add(new MergeSourceExclusionItemViewModel(exclusion));
            addedCount++;
        }

        if (addedCount > 0)
            RaiseExclusionCommandsCanExecuteChanged();
    }

    private void RemoveSelectedExclusions()
    {
        if (SelectedExclusions.Count == 0)
            return;

        MergeSourceExclusionItemViewModel[] items = [.. SelectedExclusions];

        foreach (MergeSourceExclusionItemViewModel item in items)
            Source.Exclusions.Remove(item);

        SelectedExclusions.Clear();

        RaiseExclusionCommandsCanExecuteChanged();
    }

    private void ClearExclusions()
    {
        if (Source.Exclusions.Count == 0)
            return;

        Source.Exclusions.Clear();
        SelectedExclusions.Clear();

        RaiseExclusionCommandsCanExecuteChanged();
    }

    private void EnableSelectedExclusions()
    {
        SetExclusionsEnabled(SelectedExclusions, isEnabled: true);
    }

    private void DisableSelectedExclusions()
    {
        SetExclusionsEnabled(SelectedExclusions, isEnabled: false);
    }

    private void EnableAllExclusions()
    {
        SetExclusionsEnabled(Source.Exclusions, isEnabled: true);
    }

    private void DisableAllExclusions()
    {
        SetExclusionsEnabled(Source.Exclusions, isEnabled: false);
    }

    private void SetExclusionsEnabled(IEnumerable<MergeSourceExclusionItemViewModel> exclusions, bool isEnabled)
    {
        MergeSourceExclusionItemViewModel[] items = [.. exclusions];

        bool changed = false;

        foreach (MergeSourceExclusionItemViewModel item in items)
        {
            if (item.IsEnabled == isEnabled)
                continue;

            item.IsEnabled = isEnabled;
            changed = true;
        }

        if (changed)
            RaiseExclusionCommandsCanExecuteChanged();
    }

    private void SelectedExclusions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaiseExclusionCommandsCanExecuteChanged();
    }

    private void Exclusions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasExclusions));
        RaiseExclusionCommandsCanExecuteChanged();
    }

    private void Source_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MergeSourceItemViewModel.Path)
            or nameof(MergeSourceItemViewModel.Type)
            or nameof(MergeSourceItemViewModel.Exclusions)
            or nameof(MergeSourceItemViewModel.ExclusionCount)
            or nameof(MergeSourceItemViewModel.EnabledExclusionCount)
            or nameof(MergeSourceItemViewModel.DisabledExclusionCount)
            or nameof(MergeSourceItemViewModel.ExclusionSummary))
        {
            OnPropertyChanged(nameof(CanEditExclusions));
            OnPropertyChanged(nameof(CanAddExclusions));
            OnPropertyChanged(nameof(HasExclusions));
            OnPropertyChanged(nameof(SourceExclusionsHintText));
            RaiseExclusionCommandsCanExecuteChanged();
        }
    }

    private void RaiseExclusionCommandsCanExecuteChanged()
    {
        AddExcludedFolderCommand.RaiseCanExecuteChanged();
        AddExcludedFileCommand.RaiseCanExecuteChanged();
        RemoveSelectedExclusionsCommand.RaiseCanExecuteChanged();
        ClearExclusionsCommand.RaiseCanExecuteChanged();
        EnableSelectedExclusionsCommand.RaiseCanExecuteChanged();
        DisableSelectedExclusionsCommand.RaiseCanExecuteChanged();
        EnableAllExclusionsCommand.RaiseCanExecuteChanged();
        DisableAllExclusionsCommand.RaiseCanExecuteChanged();
    }

    private static bool HasDuplicateExclusion(
        MergeSourceItemViewModel source,
        string relativePath,
        MergeSourceExclusionType type)
    {
        string normalizedRelativePath = NormalizeRelativePathForComparison(relativePath);

        return source.Exclusions.Any(x =>
            x.Type == type &&
            string.Equals(
                NormalizeRelativePathForComparison(x.RelativePath),
                normalizedRelativePath,
                StringComparison.OrdinalIgnoreCase));
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