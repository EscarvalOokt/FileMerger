using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Sources.ViewModels;

public sealed class MergeSourceExclusionItemViewModel : ViewModelBase
{
    private string _relativePath = string.Empty;
    private MergeSourceExclusionType _type;
    private bool _isEnabled = true;

    public MergeSourceExclusionItemViewModel()
    {
    }

    public MergeSourceExclusionItemViewModel(MergeSourceExclusion exclusion)
    {
        ArgumentNullException.ThrowIfNull(exclusion);

        _relativePath = exclusion.RelativePath;
        _type = exclusion.Type;
        _isEnabled = exclusion.IsEnabled;
    }

    public string RelativePath
    {
        get => _relativePath;
        set => SetProperty(ref _relativePath, value);
    }

    public MergeSourceExclusionType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public MergeSourceExclusion ToModel()
    {
        return new MergeSourceExclusion(
            relativePath: RelativePath,
            type: Type,
            isEnabled: IsEnabled);
    }
}