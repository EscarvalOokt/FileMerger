using System.Globalization;
using FileMerger.Wpf.Diagnostics;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Settings.ViewModels;

public sealed class PreferencesDialogViewModel : ViewModelBase
{
    private readonly IApplicationPreferencesStore _applicationPreferencesStore;
    private readonly ICrashLogMaintenanceService _crashLogMaintenanceService;
    private readonly IUserPromptService _userPromptService;

    private bool _isPreviewLineWrapEnabledByDefault;
    private string _previewDisplayCharacterLimitText;
    private string _crashLogRetentionLimitText;
    private string _crashLogFileCountText = string.Empty;
    private string _crashLogMaintenanceStatusText = string.Empty;
    private bool _hasCrashLogFiles;
    private bool _hasOldCrashLogFiles;
    private bool _isBusy;
    private string _validationErrorMessage = string.Empty;
    private string _saveErrorMessage = string.Empty;

    public PreferencesDialogViewModel(
        IApplicationPreferencesStore applicationPreferencesStore,
        ICrashLogMaintenanceService crashLogMaintenanceService,
        IUserPromptService userPromptService)
    {
        ArgumentNullException.ThrowIfNull(applicationPreferencesStore);
        ArgumentNullException.ThrowIfNull(crashLogMaintenanceService);
        ArgumentNullException.ThrowIfNull(userPromptService);

        _applicationPreferencesStore = applicationPreferencesStore;
        _crashLogMaintenanceService = crashLogMaintenanceService;
        _userPromptService = userPromptService;

        ApplicationPreferences current = applicationPreferencesStore.Current;

        _isPreviewLineWrapEnabledByDefault =
            current.IsPreviewLineWrapEnabledByDefault;
        _previewDisplayCharacterLimitText =
            current.PreviewDisplayCharacterLimit.ToString(
                CultureInfo.InvariantCulture);
        _crashLogRetentionLimitText =
            current.CrashLogRetentionLimit.ToString(
                CultureInfo.InvariantCulture);

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
        CancelCommand = new RelayCommand(Cancel, () => !IsBusy);
        RestoreDefaultsCommand = new RelayCommand(
            RestoreDefaults,
            () => !IsBusy);
        RefreshCrashLogsCommand = new RelayCommand(
            RefreshCrashLogs,
            () => !IsBusy);
        OpenCrashLogsFolderCommand = new RelayCommand(
            OpenCrashLogsFolder,
            () => !IsBusy);
        ClearOldCrashLogsCommand = new RelayCommand(
            ClearOldCrashLogs,
            () => !IsBusy && HasOldCrashLogFiles);
        ClearAllCrashLogsCommand = new RelayCommand(
            ClearAllCrashLogs,
            () => !IsBusy && HasCrashLogFiles);

        Validate();
        RefreshCrashLogState(clearStatus: true);
    }

    public event EventHandler<bool?>? RequestClose;

    public bool IsPreviewLineWrapEnabledByDefault
    {
        get => _isPreviewLineWrapEnabledByDefault;
        set
        {
            if (!SetProperty(ref _isPreviewLineWrapEnabledByDefault, value))
                return;

            ClearSaveError();
        }
    }

    public string PreviewDisplayCharacterLimitText
    {
        get => _previewDisplayCharacterLimitText;
        set
        {
            string normalized = value ?? string.Empty;
            if (!SetProperty(
                    ref _previewDisplayCharacterLimitText,
                    normalized))
            {
                return;
            }

            ClearSaveError();
            Validate();
        }
    }

    public string CrashLogRetentionLimitText
    {
        get => _crashLogRetentionLimitText;
        set
        {
            string normalized = value ?? string.Empty;
            if (!SetProperty(ref _crashLogRetentionLimitText, normalized))
                return;

            ClearSaveError();
            Validate();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;

            RaiseSaveStateChanged();
            CancelCommand.RaiseCanExecuteChanged();
            RestoreDefaultsCommand.RaiseCanExecuteChanged();
            RefreshCrashLogsCommand.RaiseCanExecuteChanged();
            OpenCrashLogsFolderCommand.RaiseCanExecuteChanged();
            ClearOldCrashLogsCommand.RaiseCanExecuteChanged();
            ClearAllCrashLogsCommand.RaiseCanExecuteChanged();
        }
    }

    public string ErrorMessage =>
        !string.IsNullOrWhiteSpace(_saveErrorMessage)
            ? _saveErrorMessage
            : _validationErrorMessage;

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasValidationErrors =>
        !string.IsNullOrWhiteSpace(_validationErrorMessage);

    public bool CanSave => !IsBusy && !HasValidationErrors;

    public string CrashLogDirectoryPath =>
        _crashLogMaintenanceService.GetCrashLogDirectory();

    public string CrashLogFileCountText
    {
        get => _crashLogFileCountText;
        private set => SetProperty(ref _crashLogFileCountText, value);
    }

    public string CrashLogMaintenanceStatusText
    {
        get => _crashLogMaintenanceStatusText;
        private set
        {
            if (SetProperty(ref _crashLogMaintenanceStatusText, value))
                OnPropertyChanged(nameof(HasCrashLogMaintenanceStatus));
        }
    }

    public bool HasCrashLogMaintenanceStatus =>
        !string.IsNullOrWhiteSpace(CrashLogMaintenanceStatusText);

    public bool HasCrashLogFiles
    {
        get => _hasCrashLogFiles;
        private set
        {
            if (SetProperty(ref _hasCrashLogFiles, value))
                ClearAllCrashLogsCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasOldCrashLogFiles
    {
        get => _hasOldCrashLogFiles;
        private set
        {
            if (SetProperty(ref _hasOldCrashLogFiles, value))
                ClearOldCrashLogsCommand.RaiseCanExecuteChanged();
        }
    }

    public static string PreviewDisplayCharacterLimitHint =>
        $"Allowed range: {FormatNumber(ApplicationPreferences.MinimumPreviewDisplayCharacterLimit)}" +
        $"–{FormatNumber(ApplicationPreferences.MaximumPreviewDisplayCharacterLimit)}. " +
        "Takes effect on the next preview build. Copy and Save still use the full output.";

    public static string CrashLogRetentionLimitHint =>
        $"Allowed range: {FormatNumber(ApplicationPreferences.MinimumCrashLogRetentionLimit)}" +
        $"–{FormatNumber(ApplicationPreferences.MaximumCrashLogRetentionLimit)}. " +
        "Keeps the latest N crash log files when old logs are cleared. " +
        "Save changes before running cleanup to apply a new limit.";

    public AsyncRelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand RestoreDefaultsCommand { get; }

    public RelayCommand RefreshCrashLogsCommand { get; }

    public RelayCommand OpenCrashLogsFolderCommand { get; }

    public RelayCommand ClearOldCrashLogsCommand { get; }

    public RelayCommand ClearAllCrashLogsCommand { get; }

    private async Task SaveAsync()
    {
        Validate();
        if (HasValidationErrors)
            return;

        int previewLimit = int.Parse(
            PreviewDisplayCharacterLimitText,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture);

        int crashRetentionLimit = int.Parse(
            CrashLogRetentionLimitText,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture);

        ClearSaveError();
        IsBusy = true;
        try
        {
            await _applicationPreferencesStore.UpdateAsync(_ =>
                new ApplicationPreferences(
                    isPreviewLineWrapEnabledByDefault:
                    IsPreviewLineWrapEnabledByDefault,
                    previewDisplayCharacterLimit: previewLimit,
                    crashLogRetentionLimit: crashRetentionLimit));

            RequestClose?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            SetSaveError($"Failed to save preferences: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }

    private void RestoreDefaults()
    {
        ApplicationPreferences defaults = ApplicationPreferences.Default;

        SetProperty(
            ref _isPreviewLineWrapEnabledByDefault,
            defaults.IsPreviewLineWrapEnabledByDefault,
            nameof(IsPreviewLineWrapEnabledByDefault));

        SetProperty(
            ref _previewDisplayCharacterLimitText,
            defaults.PreviewDisplayCharacterLimit.ToString(
                CultureInfo.InvariantCulture),
            nameof(PreviewDisplayCharacterLimitText));

        SetProperty(
            ref _crashLogRetentionLimitText,
            defaults.CrashLogRetentionLimit.ToString(
                CultureInfo.InvariantCulture),
            nameof(CrashLogRetentionLimitText));

        ClearSaveError();
        Validate();
    }

    private void RefreshCrashLogs()
    {
        RefreshCrashLogState(clearStatus: false);
    }

    private void RefreshCrashLogState(bool clearStatus)
    {
        if (clearStatus)
            CrashLogMaintenanceStatusText = string.Empty;

        CrashLogFileListResult result =
            _crashLogMaintenanceService.GetCrashLogFiles();

        if (!result.IsSuccessful)
        {
            CrashLogFileCountText = "Unable to read crash log folder.";
            HasCrashLogFiles = false;
            HasOldCrashLogFiles = false;

            string message = result.Error?.Message ?? "Unknown error.";
            CrashLogMaintenanceStatusText =
                $"Failed to read crash log folder: {message}";
            return;
        }

        int count = result.Files.Count;
        CrashLogFileCountText = FormatCrashLogFileCountText(count);
        HasCrashLogFiles = count > 0;
        HasOldCrashLogFiles =
            count > _applicationPreferencesStore.Current.CrashLogRetentionLimit;
    }

    private void OpenCrashLogsFolder()
    {
        CrashLogFolderOpenResult result =
            _crashLogMaintenanceService.OpenCrashLogDirectory();

        if (result.IsSuccessful)
        {
            CrashLogMaintenanceStatusText = "Crash log folder opened.";
        }
        else
        {
            string message = result.Error?.Message ?? "Unknown error.";
            CrashLogMaintenanceStatusText =
                $"Failed to open crash log folder: {message}";
        }

        RefreshCrashLogState(clearStatus: false);
    }

    private void ClearOldCrashLogs()
    {
        if (!HasOldCrashLogFiles)
            return;

        int retentionLimit =
            _applicationPreferencesStore.Current.CrashLogRetentionLimit;

        bool confirmed = _userPromptService.Confirm(
            title: "Clear old crash logs?",
            message:
            "This will delete crash log files older than the saved retention limit. " +
            $"The current saved limit is {FormatNumber(retentionLimit)} file(s). " +
            "This cannot be undone.");

        if (!confirmed)
            return;

        CrashLogCleanupResult result =
            _crashLogMaintenanceService.CleanupOldCrashLogs();

        ApplyCleanupResult(result, oldLogs: true);
        RefreshCrashLogState(clearStatus: false);
    }

    private void ClearAllCrashLogs()
    {
        if (!HasCrashLogFiles)
            return;

        bool confirmed = _userPromptService.Confirm(
            title: "Clear all crash logs?",
            message:
            "This will delete all crash log files from the diagnostics folder. " +
            "This cannot be undone.");

        if (!confirmed)
            return;

        CrashLogCleanupResult result =
            _crashLogMaintenanceService.ClearAllCrashLogs();

        ApplyCleanupResult(result, oldLogs: false);
        RefreshCrashLogState(clearStatus: false);
    }

    private void ApplyCleanupResult(
        CrashLogCleanupResult result,
        bool oldLogs)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccessful)
        {
            if (result.DeletedCount == 0)
            {
                CrashLogMaintenanceStatusText = oldLogs
                    ? "No old crash logs to clear."
                    : "No crash logs to clear.";
                return;
            }

            CrashLogMaintenanceStatusText =
                $"Deleted {FormatCrashLogFileCount(result.DeletedCount)}.";
            return;
        }

        CrashLogMaintenanceStatusText =
            $"Deleted {FormatCrashLogFileCount(result.DeletedCount)}, but " +
            $"{FormatCrashLogFileCount(result.Failures.Count)} could not be deleted.";
    }

    private void Validate()
    {
        List<string> errors = [];

        ValidateInteger(
            PreviewDisplayCharacterLimitText,
            "Preview display character limit",
            ApplicationPreferences.MinimumPreviewDisplayCharacterLimit,
            ApplicationPreferences.MaximumPreviewDisplayCharacterLimit,
            errors);

        ValidateInteger(
            CrashLogRetentionLimitText,
            "Crash log retention limit",
            ApplicationPreferences.MinimumCrashLogRetentionLimit,
            ApplicationPreferences.MaximumCrashLogRetentionLimit,
            errors);

        string nextErrorMessage = string.Join(Environment.NewLine, errors);
        if (string.Equals(
                _validationErrorMessage,
                nextErrorMessage,
                StringComparison.Ordinal))
        {
            return;
        }

        _validationErrorMessage = nextErrorMessage;
        RaiseErrorStateChanged();
        RaiseSaveStateChanged();
    }

    private static void ValidateInteger(
        string text,
        string displayName,
        int minimum,
        int maximum,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add($"{displayName} is required.");
            return;
        }

        if (!int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value))
        {
            errors.Add($"{displayName} must be a whole number.");
            return;
        }

        if (value < minimum || value > maximum)
        {
            errors.Add(
                $"{displayName} must be between " +
                $"{FormatNumber(minimum)} and {FormatNumber(maximum)}.");
        }
    }

    private void ClearSaveError()
    {
        if (string.IsNullOrEmpty(_saveErrorMessage))
            return;

        _saveErrorMessage = string.Empty;
        RaiseErrorStateChanged();
    }

    private void SetSaveError(string message)
    {
        if (string.Equals(_saveErrorMessage, message, StringComparison.Ordinal))
            return;

        _saveErrorMessage = message;
        RaiseErrorStateChanged();
    }

    private void RaiseErrorStateChanged()
    {
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasErrorMessage));
        OnPropertyChanged(nameof(HasValidationErrors));
    }

    private void RaiseSaveStateChanged()
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.RaiseCanExecuteChanged();
    }

    private static string FormatNumber(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatCrashLogFileCountText(int count)
    {
        return count switch
        {
            0 => "No crash logs found.",
            1 => "1 crash log file found.",
            _ => $"{FormatNumber(count)} crash log files found."
        };
    }

    private static string FormatCrashLogFileCount(int count)
    {
        return count == 1
            ? "1 crash log file"
            : $"{FormatNumber(count)} crash log files";
    }
}