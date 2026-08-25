using FileMerger.Domain.Enums;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class FileTypeOptionViewModel : ViewModelBase
{
    private bool _isEnabled;

    public FileTypeOptionViewModel(
        string extension,
        string displayName,
        FileKind kind,
        bool isEnabled,
        bool supportsLanguageSpecificProcessing)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be empty.", nameof(extension));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));

        Extension = extension;
        DisplayName = displayName;
        Kind = kind;
        _isEnabled = isEnabled;
        SupportsLanguageSpecificProcessing = supportsLanguageSpecificProcessing;
    }

    public string Extension { get; }
    public string DisplayName { get; }
    public FileKind Kind { get; }
    public bool SupportsLanguageSpecificProcessing { get; }

    public string DisplayLabel => $"{DisplayName} ({Extension})";

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
}