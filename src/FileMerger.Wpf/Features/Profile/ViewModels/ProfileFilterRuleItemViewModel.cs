using System.Text.RegularExpressions;
using FileMerger.Domain.Enums;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileFilterRuleItemViewModel : ViewModelBase
{
    private FilterMode _mode;
    private FilterTarget _target;
    private RulePatternType _patternType;
    private string _pattern = string.Empty;
    private bool _isEnabled = true;
    private string? _description;
    private bool _isUserEditable = true;
    private string _validationMessage = string.Empty;

    public ProfileFilterRuleItemViewModel()
    {
        Validate();
    }

    public ProfileFilterRuleItemViewModel(WorkspaceFileFilterRuleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        _mode = dto.Mode;
        _target = dto.Target;
        _patternType = dto.PatternType;
        _pattern = dto.Pattern;
        _isEnabled = dto.IsEnabled;
        _description = dto.Description;
        _isUserEditable = dto.IsUserEditable;

        Validate();
    }

    public FilterMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
                Validate();
        }
    }

    public FilterTarget Target
    {
        get => _target;
        set
        {
            if (SetProperty(ref _target, value))
                Validate();
        }
    }

    public RulePatternType PatternType
    {
        get => _patternType;
        set
        {
            if (SetProperty(ref _patternType, value))
                Validate();
        }
    }

    public string Pattern
    {
        get => _pattern;
        set
        {
            if (SetProperty(ref _pattern, value ?? string.Empty))
                Validate();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, NormalizeDescription(value));
    }

    public bool IsUserEditable
    {
        get => _isUserEditable;
        set => SetProperty(ref _isUserEditable, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (SetProperty(ref _validationMessage, value))
                OnPropertyChanged(nameof(HasValidationError));
        }
    }

    public bool HasValidationError => !string.IsNullOrWhiteSpace(ValidationMessage);

    public WorkspaceFileFilterRuleDto ToDto()
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: Mode,
            Target: Target,
            PatternType: PatternType,
            Pattern: Pattern.Trim(),
            IsEnabled: IsEnabled,
            Description: NormalizeDescription(Description),
            IsUserEditable: IsUserEditable);
    }

    public ProfileFilterRuleItemViewModel Clone()
    {
        return new ProfileFilterRuleItemViewModel(ToDto());
    }

    public static ProfileFilterRuleItemViewModel FromDto(WorkspaceFileFilterRuleDto dto)
    {
        return new ProfileFilterRuleItemViewModel(dto);
    }

    private void Validate()
    {
        string pattern = Pattern.Trim();

        if (string.IsNullOrWhiteSpace(pattern))
        {
            ValidationMessage = "Pattern cannot be empty.";
            return;
        }

        if (Target == FilterTarget.DirectorySegment &&
            (pattern.Contains('\\') || pattern.Contains('/')))
        {
            ValidationMessage = "Directory segment cannot contain path separators.";
            return;
        }

        if (Target == FilterTarget.Extension && !pattern.StartsWith('.'))
        {
            ValidationMessage = "Extension pattern should start with '.'.";
            return;
        }

        if (PatternType == RulePatternType.Regex)
        {
            try
            {
                _ = new Regex(pattern, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException ex)
            {
                ValidationMessage = $"Invalid regex: {ex.Message}";
                return;
            }
        }

        ValidationMessage = string.Empty;
    }

    private static string? NormalizeDescription(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}