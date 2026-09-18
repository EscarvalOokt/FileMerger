using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Files.ViewModels;

public sealed class FileListFilterOptionViewModel : ViewModelBase
{
    private int _count;
    private bool _isSelected;

    public FileListFilterOptionViewModel(FileListFacet facet, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label cannot be empty.", nameof(label));

        Facet = facet;
        Label = label;
    }

    public FileListFacet Facet { get; }
    public string Label { get; }

    public int Count
    {
        get => _count;
        private set
        {
            if (SetProperty(ref _count, value))
                OnPropertyChanged(nameof(DisplayText));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DisplayText => $"{Label} ({Count})";

    public void UpdateCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        Count = count;
    }
}