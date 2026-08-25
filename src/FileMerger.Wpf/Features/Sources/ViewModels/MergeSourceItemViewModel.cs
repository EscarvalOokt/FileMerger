using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Sources.ViewModels;

public sealed class MergeSourceItemViewModel : ViewModelBase
{
    private string _path = string.Empty;
    private bool _isRecursive = true;
    private bool _isEnabled = true;

    public MergeSourceItemViewModel()
    {
        Type = MergeSourceType.Directory;

        Exclusions = [];
        Exclusions.CollectionChanged += Exclusions_CollectionChanged;
    }

    public MergeSourceItemViewModel(MergeSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _path = source.Path;
        Type = source.Type;
        _isRecursive = source.Type == MergeSourceType.Directory && source.IsRecursive;
        _isEnabled = source.IsEnabled;

        Exclusions = source.Type == MergeSourceType.Directory
            ? [.. source.Exclusions.Select(x => new MergeSourceExclusionItemViewModel(x))]
            : [];

        Exclusions.CollectionChanged += Exclusions_CollectionChanged;

        foreach (MergeSourceExclusionItemViewModel exclusion in Exclusions)
            exclusion.PropertyChanged += Exclusion_PropertyChanged;
    }

    public ObservableCollection<MergeSourceExclusionItemViewModel> Exclusions { get; }

    public string Path
    {
        get => _path;
        set => SetProperty(ref _path, value);
    }

    public MergeSourceType Type { get; }

    public bool CanEditRecursive => Type == MergeSourceType.Directory;

    public bool IsRecursive
    {
        get => _isRecursive;
        set
        {
            bool normalized = CanEditRecursive && value;
            SetProperty(ref _isRecursive, normalized);
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public int ExclusionCount => Exclusions.Count;

    public int EnabledExclusionCount => Exclusions.Count(x => x.IsEnabled);

    public int DisabledExclusionCount => Exclusions.Count(x => !x.IsEnabled);

    public bool HasDisabledExclusions => DisabledExclusionCount > 0;

    public string ExclusionSummary
    {
        get
        {
            if (ExclusionCount == 0)
                return "No exclusions";

            if (ExclusionCount == 1)
                return DisabledExclusionCount == 1
                    ? "1 exclusion · disabled"
                    : "1 exclusion";

            if (DisabledExclusionCount == 0)
                return $"{ExclusionCount} exclusions";

            if (EnabledExclusionCount == 0)
                return $"{ExclusionCount} exclusions · all disabled";

            return $"{ExclusionCount} exclusions · {EnabledExclusionCount} enabled · {DisabledExclusionCount} disabled";
        }
    }

    public MergeSource ToModel()
    {
        return new MergeSource(
            path: Path,
            type: Type,
            isRecursive: CanEditRecursive && IsRecursive,
            isEnabled: IsEnabled,
            exclusions: Type == MergeSourceType.Directory
                ? [.. Exclusions.Select(x => x.ToModel())]
                : []);
    }

    private void Exclusions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (MergeSourceExclusionItemViewModel item in e.OldItems)
                item.PropertyChanged -= Exclusion_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (MergeSourceExclusionItemViewModel item in e.NewItems)
                item.PropertyChanged += Exclusion_PropertyChanged;
        }

        RaiseExclusionSummaryPropertiesChanged();
    }

    private void Exclusion_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MergeSourceExclusionItemViewModel.RelativePath) or
            nameof(MergeSourceExclusionItemViewModel.Type) or
            nameof(MergeSourceExclusionItemViewModel.IsEnabled))
        {
            RaiseExclusionSummaryPropertiesChanged();
        }
    }

    private void RaiseExclusionSummaryPropertiesChanged()
    {
        OnPropertyChanged(nameof(Exclusions));
        OnPropertyChanged(nameof(ExclusionCount));
        OnPropertyChanged(nameof(EnabledExclusionCount));
        OnPropertyChanged(nameof(DisabledExclusionCount));
        OnPropertyChanged(nameof(HasDisabledExclusions));
        OnPropertyChanged(nameof(ExclusionSummary));
    }
}