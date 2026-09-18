using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileLibraryListItemViewModel : ViewModelBase
{
    private readonly string _usageLabel;
    private ProfileLibraryEntry _entry;
    private bool _isDirty;
    private bool _isUsedByCurrentSession;

    public ProfileLibraryListItemViewModel(ProfileLibraryEntry entry, string usageLabel = "Active")
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(usageLabel);

        _entry = entry;
        _usageLabel = usageLabel;
    }

    public ProfileLibraryEntry Entry => _entry;

    public string Id => _entry.Id;
    public string DisplayName => _entry.DisplayName;
    public string Description => _entry.Description;
    public string KindLabel => _entry.IsBuiltIn ? "Built-in" : "User";
    public string UpdatedLabel => _entry.UpdatedAtUtc?.ToLocalTime().ToString("g") ?? string.Empty;

    public bool IsBuiltIn => _entry.IsBuiltIn;
    public bool IsReadOnly => _entry.IsReadOnly;
    public bool IsUserDefined => _entry.IsUserDefined;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasUpdatedLabel => !string.IsNullOrWhiteSpace(UpdatedLabel);
    public bool HasUsageLabel => IsUsedByCurrentSession;

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (!SetProperty(ref _isDirty, value))
                return;

            OnPropertyChanged(nameof(DirtyLabel));
            OnPropertyChanged(nameof(HasDirtyLabel));
        }
    }

    public string DirtyLabel => IsDirty ? "Dirty" : string.Empty;

    public bool HasDirtyLabel => IsDirty;

    public bool IsUsedByCurrentSession
    {
        get => _isUsedByCurrentSession;
        set
        {
            if (!SetProperty(ref _isUsedByCurrentSession, value))
                return;

            OnPropertyChanged(nameof(UsageLabel));
            OnPropertyChanged(nameof(HasUsageLabel));
        }
    }

    public string UsageLabel => IsUsedByCurrentSession ? _usageLabel : string.Empty;

    public void SetUsedByCurrentSession(bool value)
    {
        IsUsedByCurrentSession = value;
    }

    public void SetDirty(bool value)
    {
        IsDirty = value;
    }

    public void Update(ProfileLibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;

        IsDirty = false;

        OnPropertyChanged(string.Empty);
        OnPropertyChanged(nameof(UsageLabel));
        OnPropertyChanged(nameof(HasUsageLabel));
    }
}