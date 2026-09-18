using System.Collections.ObjectModel;
using System.ComponentModel;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileFileTypeGroupViewModel : ViewModelBase
{
    private readonly ObservableCollection<FileTypeOptionViewModel> _visibleFileTypes = [];
    private bool _isExpanded;

    public ProfileFileTypeGroupViewModel(
        string title,
        string description,
        IEnumerable<FileTypeOptionViewModel> fileTypes,
        bool isExpanded)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        ArgumentNullException.ThrowIfNull(fileTypes);

        Title = title;
        Description = description;
        _isExpanded = isExpanded;

        FileTypes = new ObservableCollection<FileTypeOptionViewModel>(
            fileTypes.OrderBy(x => x.Extension, StringComparer.OrdinalIgnoreCase));

        foreach (FileTypeOptionViewModel fileType in FileTypes)
            fileType.PropertyChanged += FileType_PropertyChanged;

        EnableAllCommand = new RelayCommand(EnableAll, () => CanEnableAll);
        DisableAllCommand = new RelayCommand(DisableAll, () => CanDisableAll);

        ApplyVisibleFileTypes(_ => true);
    }

    public string Title { get; }

    public string Description { get; }

    public ObservableCollection<FileTypeOptionViewModel> FileTypes { get; }

    public ObservableCollection<FileTypeOptionViewModel> VisibleFileTypes => _visibleFileTypes;

    public int EnabledCount => FileTypes.Count(x => x.IsEnabled);

    public int TotalCount => FileTypes.Count;

    public int VisibleCount => VisibleFileTypes.Count;

    public bool HasVisibleFileTypes => VisibleCount > 0;

    public string SummaryLabel => $"{EnabledCount}/{TotalCount} enabled";

    public string FilteredSummaryLabel =>
        VisibleCount == TotalCount ? SummaryLabel : $"{VisibleCount}/{TotalCount} shown • {SummaryLabel}";

    public bool CanEnableAll => VisibleFileTypes.Any(x => !x.IsEnabled);

    public bool CanDisableAll => VisibleFileTypes.Any(x => x.IsEnabled);

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public RelayCommand EnableAllCommand { get; }

    public RelayCommand DisableAllCommand { get; }

    public void ApplyVisibleFileTypes(Func<FileTypeOptionViewModel, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _visibleFileTypes.Clear();

        foreach (FileTypeOptionViewModel fileType in FileTypes.Where(predicate))
            _visibleFileTypes.Add(fileType);

        RefreshState();
        OnPropertyChanged(nameof(VisibleFileTypes));
    }

    public void Detach()
    {
        foreach (FileTypeOptionViewModel fileType in FileTypes)
            fileType.PropertyChanged -= FileType_PropertyChanged;
    }

    private void EnableAll()
    {
        SetVisible(isEnabled: true);
    }

    private void DisableAll()
    {
        SetVisible(isEnabled: false);
    }

    private void SetVisible(bool isEnabled)
    {
        FileTypeOptionViewModel[] visibleFileTypes =
        [
            .. VisibleFileTypes
        ];

        foreach (FileTypeOptionViewModel fileType in visibleFileTypes)
            fileType.IsEnabled = isEnabled;

        RefreshState();
    }

    private void FileType_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileTypeOptionViewModel.IsEnabled))
            RefreshState();
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(EnabledCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(HasVisibleFileTypes));
        OnPropertyChanged(nameof(SummaryLabel));
        OnPropertyChanged(nameof(FilteredSummaryLabel));
        OnPropertyChanged(nameof(CanEnableAll));
        OnPropertyChanged(nameof(CanDisableAll));

        EnableAllCommand.RaiseCanExecuteChanged();
        DisableAllCommand.RaiseCanExecuteChanged();
    }
}