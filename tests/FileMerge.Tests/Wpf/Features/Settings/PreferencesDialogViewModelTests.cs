using System.Globalization;
using System.IO;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Wpf.Diagnostics;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Features.Settings.ViewModels;

namespace FileMerger.Tests.Wpf.Features.Settings;

public sealed class PreferencesDialogViewModelTests
{
    [Fact]
    public void Constructor_Should_Load_Current_Preferences()
    {
        FakeApplicationPreferencesStore store = CreateStore(
            new ApplicationPreferences(true, 20_000, 50));

        PreferencesDialogViewModel viewModel = CreateViewModel(store);

        Assert.True(viewModel.IsPreviewLineWrapEnabledByDefault);
        Assert.Equal("20000", viewModel.PreviewDisplayCharacterLimitText);
        Assert.Equal("50", viewModel.CrashLogRetentionLimitText);
    }

    [Fact]
    public void SaveCommand_Should_Save_Updated_Preferences()
    {
        FakeApplicationPreferencesStore store = new();
        PreferencesDialogViewModel viewModel = CreateViewModel(store);
        viewModel.IsPreviewLineWrapEnabledByDefault = true;
        viewModel.PreviewDisplayCharacterLimitText = "25000";
        viewModel.CrashLogRetentionLimitText = "75";

        viewModel.SaveCommand.Execute(null);

        ApplicationPreferences saved = Assert.Single(store.SavedPreferences);
        Assert.True(saved.IsPreviewLineWrapEnabledByDefault);
        Assert.Equal(25_000, saved.PreviewDisplayCharacterLimit);
        Assert.Equal(75, saved.CrashLogRetentionLimit);
    }

    [Fact]
    public void SaveCommand_Should_Request_Close_When_Save_Succeeds()
    {
        PreferencesDialogViewModel viewModel = CreateViewModel();
        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.SaveCommand.Execute(null);

        Assert.True(closeResult);
    }

    [Fact]
    public void CancelCommand_Should_Request_Close_Without_Saving()
    {
        FakeApplicationPreferencesStore store = new();
        PreferencesDialogViewModel viewModel = CreateViewModel(store);
        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.CancelCommand.Execute(null);

        Assert.False(closeResult);
        Assert.Empty(store.SavedPreferences);
    }

    [Fact]
    public void RestoreDefaultsCommand_Should_Reset_Fields_Without_Saving()
    {
        FakeApplicationPreferencesStore store = CreateStore(
            new ApplicationPreferences(true, 20_000, 50));
        PreferencesDialogViewModel viewModel = CreateViewModel(store);

        viewModel.RestoreDefaultsCommand.Execute(null);

        Assert.Equal(
            ApplicationPreferences.Default.IsPreviewLineWrapEnabledByDefault,
            viewModel.IsPreviewLineWrapEnabledByDefault);
        Assert.Equal(
            ToText(ApplicationPreferences.DefaultPreviewDisplayCharacterLimit),
            viewModel.PreviewDisplayCharacterLimitText);
        Assert.Equal(
            ToText(ApplicationPreferences.DefaultCrashLogRetentionLimit),
            viewModel.CrashLogRetentionLimitText);
        Assert.Empty(store.SavedPreferences);
    }

    [Fact]
    public void SaveCommand_Should_Be_Disabled_When_PreviewLimit_Is_Not_Number()
    {
        PreferencesDialogViewModel viewModel = CreateViewModel();

        viewModel.PreviewDisplayCharacterLimitText = "not a number";

        AssertSaveDisabledWithValidationError(viewModel);
    }

    [Fact]
    public void SaveCommand_Should_Be_Disabled_When_PreviewLimit_Is_Below_Minimum()
    {
        PreferencesDialogViewModel viewModel = CreateViewModel();

        viewModel.PreviewDisplayCharacterLimitText = ToText(
            ApplicationPreferences.MinimumPreviewDisplayCharacterLimit - 1);

        AssertSaveDisabledWithValidationError(viewModel);
    }

    [Fact]
    public void SaveCommand_Should_Be_Disabled_When_PreviewLimit_Is_Above_Maximum()
    {
        PreferencesDialogViewModel viewModel = CreateViewModel();

        viewModel.PreviewDisplayCharacterLimitText = ToText(
            ApplicationPreferences.MaximumPreviewDisplayCharacterLimit + 1);

        AssertSaveDisabledWithValidationError(viewModel);
    }

    [Fact]
    public void SaveCommand_Should_Be_Disabled_When_CrashRetention_Is_Not_Number()
    {
        PreferencesDialogViewModel viewModel = CreateViewModel();

        viewModel.CrashLogRetentionLimitText = "not a number";

        AssertSaveDisabledWithValidationError(viewModel);
    }

    [Fact]
    public void SaveCommand_Should_Be_Disabled_When_CrashRetention_Is_Below_Minimum()
    {
        PreferencesDialogViewModel viewModel = CreateViewModel();

        viewModel.CrashLogRetentionLimitText = ToText(
            ApplicationPreferences.MinimumCrashLogRetentionLimit - 1);

        AssertSaveDisabledWithValidationError(viewModel);
    }

    [Fact]
    public void SaveCommand_Should_Be_Disabled_When_CrashRetention_Is_Above_Maximum()
    {
        PreferencesDialogViewModel viewModel = CreateViewModel();

        viewModel.CrashLogRetentionLimitText = ToText(
            ApplicationPreferences.MaximumCrashLogRetentionLimit + 1);

        AssertSaveDisabledWithValidationError(viewModel);
    }

    [Fact]
    public void SaveCommand_Should_Show_Error_And_Not_Close_When_Save_Fails()
    {
        FakeApplicationPreferencesStore store = new()
        {
            UpdateException = new InvalidOperationException("Save failed.")
        };
        PreferencesDialogViewModel viewModel = CreateViewModel(store);
        bool closeRequested = false;
        viewModel.RequestClose += (_, _) => closeRequested = true;

        viewModel.SaveCommand.Execute(null);

        Assert.True(viewModel.HasErrorMessage);
        Assert.Contains("Save failed.", viewModel.ErrorMessage);
        Assert.False(closeRequested);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public void Constructor_Should_Load_Crash_Log_Directory_And_File_Count()
    {
        FakeApplicationPreferencesStore store = CreateStore(
            new ApplicationPreferences(false, 20_000, 2));
        FakeCrashLogMaintenanceService crashLogs = new();
        AddCrashLogFiles(crashLogs, 3);

        PreferencesDialogViewModel viewModel =
            CreateViewModel(store, crashLogs);

        Assert.Equal(crashLogs.DirectoryPath, viewModel.CrashLogDirectoryPath);
        Assert.Equal("3 crash log files found.", viewModel.CrashLogFileCountText);
        Assert.True(viewModel.HasCrashLogFiles);
        Assert.True(viewModel.HasOldCrashLogFiles);
    }

    [Fact]
    public void RefreshCrashLogsCommand_Should_Update_File_Count()
    {
        FakeCrashLogMaintenanceService crashLogs = new();
        PreferencesDialogViewModel viewModel =
            CreateViewModel(crashLogs: crashLogs);
        AddCrashLogFiles(crashLogs, 2);

        viewModel.RefreshCrashLogsCommand.Execute(null);

        Assert.Equal("2 crash log files found.", viewModel.CrashLogFileCountText);
        Assert.True(viewModel.HasCrashLogFiles);
    }

    [Fact]
    public void RefreshCrashLogsCommand_Should_Show_Error_When_Enumeration_Fails()
    {
        FakeCrashLogMaintenanceService crashLogs = new();
        PreferencesDialogViewModel viewModel =
            CreateViewModel(crashLogs: crashLogs);
        crashLogs.FileListResult = CrashLogFileListResult.Failure(
            new IOException("read failed"));

        viewModel.RefreshCrashLogsCommand.Execute(null);

        Assert.True(viewModel.HasCrashLogMaintenanceStatus);
        Assert.Contains(
            "read failed",
            viewModel.CrashLogMaintenanceStatusText);
        Assert.Equal(
            "Unable to read crash log folder.",
            viewModel.CrashLogFileCountText);
    }

    [Fact]
    public void OpenCrashLogsFolderCommand_Should_Call_Service_And_Show_Status()
    {
        FakeCrashLogMaintenanceService crashLogs = new();
        PreferencesDialogViewModel viewModel =
            CreateViewModel(crashLogs: crashLogs);

        viewModel.OpenCrashLogsFolderCommand.Execute(null);

        Assert.Equal(1, crashLogs.OpenCalls);
        Assert.Equal(
            "Crash log folder opened.",
            viewModel.CrashLogMaintenanceStatusText);
    }

    [Fact]
    public void OpenCrashLogsFolderCommand_Should_Show_Error_When_Service_Fails()
    {
        FakeCrashLogMaintenanceService crashLogs = new();
        crashLogs.OpenResult = CrashLogFolderOpenResult.Failure(
            crashLogs.DirectoryPath,
            new InvalidOperationException("open failed"));
        PreferencesDialogViewModel viewModel =
            CreateViewModel(crashLogs: crashLogs);

        viewModel.OpenCrashLogsFolderCommand.Execute(null);

        Assert.Contains(
            "open failed",
            viewModel.CrashLogMaintenanceStatusText);
    }

    [Fact]
    public void ClearOldCrashLogsCommand_Should_Ask_For_Confirmation()
    {
        FakeApplicationPreferencesStore store = CreateStore(
            new ApplicationPreferences(false, 20_000, 1));
        FakeCrashLogMaintenanceService crashLogs = new();
        AddCrashLogFiles(crashLogs, 2);
        FakeUserPromptService prompts = new();
        PreferencesDialogViewModel viewModel =
            CreateViewModel(store, crashLogs, prompts);

        viewModel.ClearOldCrashLogsCommand.Execute(null);

        Assert.Equal(1, prompts.ConfirmCalls);
        Assert.Equal("Clear old crash logs?", prompts.LastConfirmTitle);
        Assert.Contains("saved limit is 1", prompts.LastConfirmMessage);
    }

    [Fact]
    public void ClearOldCrashLogsCommand_Should_Not_Run_When_Confirmation_Is_Cancelled()
    {
        FakeApplicationPreferencesStore store = CreateStore(
            new ApplicationPreferences(false, 20_000, 1));
        FakeCrashLogMaintenanceService crashLogs = new();
        AddCrashLogFiles(crashLogs, 2);
        FakeUserPromptService prompts = new()
        {
            ConfirmResult = false
        };
        PreferencesDialogViewModel viewModel =
            CreateViewModel(store, crashLogs, prompts);

        viewModel.ClearOldCrashLogsCommand.Execute(null);

        Assert.Equal(0, crashLogs.CleanupOldCalls);
    }

    [Fact]
    public void ClearOldCrashLogsCommand_Should_Run_When_Confirmed_And_Show_Deleted_Count()
    {
        FakeApplicationPreferencesStore store = CreateStore(
            new ApplicationPreferences(false, 20_000, 1));
        FakeCrashLogMaintenanceService crashLogs = new()
        {
            CleanupOldResult =
                CrashLogCleanupResult.From(["a", "b"], [])
        };
        AddCrashLogFiles(crashLogs, 2);
        FakeUserPromptService prompts = new();
        PreferencesDialogViewModel viewModel =
            CreateViewModel(store, crashLogs, prompts);

        viewModel.ClearOldCrashLogsCommand.Execute(null);

        Assert.Equal(1, crashLogs.CleanupOldCalls);
        Assert.Contains(
            "Deleted 2",
            viewModel.CrashLogMaintenanceStatusText);
    }

    [Fact]
    public void ClearOldCrashLogsCommand_Should_Be_Disabled_When_No_Old_Logs()
    {
        FakeApplicationPreferencesStore store = CreateStore(
            new ApplicationPreferences(false, 20_000, 5));
        FakeCrashLogMaintenanceService crashLogs = new();
        AddCrashLogFiles(crashLogs, 2);

        PreferencesDialogViewModel viewModel =
            CreateViewModel(store, crashLogs);

        Assert.False(viewModel.ClearOldCrashLogsCommand.CanExecute(null));
    }

    [Fact]
    public void ClearAllCrashLogsCommand_Should_Ask_For_Confirmation()
    {
        FakeCrashLogMaintenanceService crashLogs = new();
        AddCrashLogFiles(crashLogs, 1);
        FakeUserPromptService prompts = new();
        PreferencesDialogViewModel viewModel =
            CreateViewModel(crashLogs: crashLogs, prompts: prompts);

        viewModel.ClearAllCrashLogsCommand.Execute(null);

        Assert.Equal(1, prompts.ConfirmCalls);
        Assert.Equal("Clear all crash logs?", prompts.LastConfirmTitle);
        Assert.Contains("cannot be undone", prompts.LastConfirmMessage);
    }

    [Fact]
    public void ClearAllCrashLogsCommand_Should_Not_Run_When_Confirmation_Is_Cancelled()
    {
        FakeCrashLogMaintenanceService crashLogs = new();
        AddCrashLogFiles(crashLogs, 1);
        FakeUserPromptService prompts = new()
        {
            ConfirmResult = false
        };
        PreferencesDialogViewModel viewModel =
            CreateViewModel(crashLogs: crashLogs, prompts: prompts);

        viewModel.ClearAllCrashLogsCommand.Execute(null);

        Assert.Equal(0, crashLogs.ClearAllCalls);
    }

    [Fact]
    public void ClearAllCrashLogsCommand_Should_Run_When_Confirmed_And_Show_Deleted_Count()
    {
        FakeCrashLogMaintenanceService crashLogs = new()
        {
            ClearAllResult = CrashLogCleanupResult.From(["a"], [])
        };
        AddCrashLogFiles(crashLogs, 1);
        PreferencesDialogViewModel viewModel =
            CreateViewModel(crashLogs: crashLogs);

        viewModel.ClearAllCrashLogsCommand.Execute(null);

        Assert.Equal(1, crashLogs.ClearAllCalls);
        Assert.Contains(
            "Deleted 1",
            viewModel.CrashLogMaintenanceStatusText);
    }

    [Fact]
    public void ClearAllCrashLogsCommand_Should_Be_Disabled_When_No_Logs()
    {
        PreferencesDialogViewModel viewModel = CreateViewModel();

        Assert.False(viewModel.ClearAllCrashLogsCommand.CanExecute(null));
    }

    [Fact]
    public void Diagnostics_Actions_Should_Not_Request_Close()
    {
        FakeApplicationPreferencesStore store = CreateStore(
            new ApplicationPreferences(false, 20_000, 1));
        FakeCrashLogMaintenanceService crashLogs = new();
        AddCrashLogFiles(crashLogs, 2);
        PreferencesDialogViewModel viewModel =
            CreateViewModel(store, crashLogs);
        bool closeRequested = false;
        viewModel.RequestClose += (_, _) => closeRequested = true;

        viewModel.RefreshCrashLogsCommand.Execute(null);
        viewModel.OpenCrashLogsFolderCommand.Execute(null);
        viewModel.ClearOldCrashLogsCommand.Execute(null);
        viewModel.ClearAllCrashLogsCommand.Execute(null);

        Assert.False(closeRequested);
    }

    private static PreferencesDialogViewModel CreateViewModel(
        FakeApplicationPreferencesStore? store = null,
        FakeCrashLogMaintenanceService? crashLogs = null,
        FakeUserPromptService? prompts = null)
    {
        return new PreferencesDialogViewModel(
            store ?? new FakeApplicationPreferencesStore(),
            crashLogs ?? new FakeCrashLogMaintenanceService(),
            prompts ?? new FakeUserPromptService());
    }

    private static FakeApplicationPreferencesStore CreateStore(
        ApplicationPreferences preferences)
    {
        FakeApplicationPreferencesStore store = new();
        store.SetCurrent(preferences);
        return store;
    }

    private static void AddCrashLogFiles(
        FakeCrashLogMaintenanceService crashLogs,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            crashLogs.Files.Add(new CrashLogFileInfo(
                Path: $@"C:\FileMerger\CrashLogs\crash-{i}.log",
                FileName: $"crash-{i}.log",
                LastWriteTimeUtc: DateTime.UtcNow.AddMinutes(-i),
                SizeInBytes: 100 + i));
        }
    }

    private static void AssertSaveDisabledWithValidationError(
        PreferencesDialogViewModel viewModel)
    {
        Assert.True(viewModel.HasValidationErrors);
        Assert.True(viewModel.HasErrorMessage);
        Assert.False(viewModel.CanSave);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
    }

    private static string ToText(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}