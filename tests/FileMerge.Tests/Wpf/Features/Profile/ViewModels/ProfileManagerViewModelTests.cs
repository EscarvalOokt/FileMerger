using System.Reflection;
using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.Status;

namespace FileMerger.Tests.Wpf.Features.Profile.ViewModels;

public sealed class ProfileManagerViewModelTests
{
    [Fact]
    public async Task InitializeAsync_Should_Mark_Profile_Active_By_EntryId()
    {
        ProfileLibraryEntry entry = CreateEntry(id: "profile-repository", name: "Repository Profile");

        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Different Display Name",
            currentProfileEntryId: "profile-repository",
            profile: CreateProfile(includeHeaderComment: true));

        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileLibraryListItemViewModel item = Assert.Single(context.ViewModel.Profiles);
        Assert.True(item.IsUsedByCurrentSession);
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Use_CurrentSession_Presentation_By_Default()
    {
        ProfileLibraryEntry entry = CreateEntry(id: "profile-apply", name: "Apply Profile");
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: null,
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        context.ViewModel.ApplyToCurrentSessionCommand.Execute(null);

        ProfileLibraryListItemViewModel item = Assert.Single(context.ViewModel.Profiles);
        Assert.Equal(1, host.ApplyCallCount);
        Assert.Equal("profile-apply", host.CurrentProfileEntryId);
        Assert.True(item.IsUsedByCurrentSession);
        Assert.Equal("Active", item.UsageLabel);
        Assert.Equal("Active", context.ViewModel.UsedProfileLabel);
        Assert.Equal(StatusSeverity.Success, context.ViewModel.StatusSeverity);
        Assert.Equal("Profile applied to current session.", context.ViewModel.StatusMessage);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Use_WorkspaceConfiguration_Presentation_For_Staged_Host()
    {
        ProfileLibraryEntry entry = CreateEntry(id: "profile-apply", name: "Apply Profile");
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: null,
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host, context: ProfileManagerContext.WorkspaceConfiguration);

        await context.ViewModel.InitializeAsync();

        context.ViewModel.ApplyToCurrentSessionCommand.Execute(null);

        ProfileLibraryListItemViewModel item = Assert.Single(context.ViewModel.Profiles);
        Assert.Equal(1, host.ApplyCallCount);
        Assert.Equal("profile-apply", host.CurrentProfileEntryId);
        Assert.True(item.IsUsedByCurrentSession);
        Assert.Equal("Selected", item.UsageLabel);
        Assert.Equal("Selected", context.ViewModel.UsedProfileLabel);
        Assert.Equal(StatusSeverity.Success, context.ViewModel.StatusSeverity);
        Assert.Equal(
            "Profile selected for workspace configuration. Confirm with OK to apply it.",
            context.ViewModel.StatusMessage);
        Assert.Equal(0, context.LibraryService.SaveCallCount);

        context.ViewModel.Editor.IncludeHeaderComment = !context.ViewModel.Editor.IncludeHeaderComment;
        context.ViewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal(2, host.ApplyCallCount);
        Assert.Null(host.CurrentProfileEntryId);
        Assert.False(item.IsUsedByCurrentSession);
        Assert.Equal(string.Empty, item.UsageLabel);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
    }

    [Fact]
    public async Task InitializeAsync_Should_Not_Infer_Active_Profile_From_Name_And_Content()
    {
        WorkspaceProfileDto profile = CreateProfile(includeHeaderComment: true);
        ProfileLibraryEntry entry = CreateEntry(id: "profile-repository", name: "Repository Profile", profile: profile);

        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Repository Profile",
            currentProfileEntryId: null,
            profile: profile);

        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileLibraryListItemViewModel item = Assert.Single(context.ViewModel.Profiles);
        Assert.False(item.IsUsedByCurrentSession);
    }

    [Fact]
    public async Task InitializeAsync_Should_Load_Category_Selection_Into_Editor_Without_Applying_To_Current_Session()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        WorkspaceProfileDto libraryProfile = CreateProfile(skippedFileCategories: selection);
        WorkspaceProfileDto currentProfile = CreateProfile();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-categories",
            name: "Category Profile",
            profile: libraryProfile);
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: currentProfile);

        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        Assert.Equal(selection, context.ViewModel.Editor.CaptureProfile().SkippedFileCategories);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Equal(currentProfile, host.CaptureCurrentProfile());
    }

    [Fact]
    public async Task SaveCommand_Should_Persist_Staged_Category_Selection()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-save",
            name: "Save Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: null,
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();
        context.ViewModel.Editor.IncludeUnsupportedFilesInSkippedMetadata = false;

        context.ViewModel.SaveCommand.Execute(null);

        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(0, host.ApplyCallCount);
        Assert.NotNull(context.LibraryService.LastSavedEntry);
        Assert.False(context.LibraryService.LastSaveAsNew.GetValueOrDefault(true));
        Assert.Equal(
            selection with { IncludeUnsupportedFiles = false },
            context.LibraryService.LastSavedEntry!.Profile.SkippedFileCategories);
        Assert.False(context.ViewModel.IsDirty);
        Assert.Null(host.CaptureCurrentProfile().SkippedFileCategories);
    }

    [Fact]
    public async Task ImportCommand_Should_Load_Imported_Category_Selection_Without_Applying_To_Current_Session()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection(includeSourceExclusions: true);
        ProfileLibraryEntry existing = CreateEntry(id: "profile-existing", name: "Existing Profile");
        ProfileLibraryEntry imported = CreateEntry(
            id: "profile-imported",
            name: "Imported Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        WorkspaceProfileDto currentProfile = CreateProfile();
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: currentProfile);
        TestContext context = CreateContext(
            [existing],
            host,
            selectedImportFiles: [@"D:\Profiles\import.filemerger.profile.json"]);
        context.LibraryService.ImportResult = [imported];

        await context.ViewModel.InitializeAsync();

        context.ViewModel.ImportCommand.Execute(null);

        Assert.NotNull(context.LibraryService.LastImportedFilePaths);
        Assert.Equal([@"D:\Profiles\import.filemerger.profile.json"], context.LibraryService.LastImportedFilePaths);
        Assert.Equal("profile-imported", context.ViewModel.SelectedProfile?.Id);
        Assert.Equal(selection, context.ViewModel.Editor.CaptureProfile().SkippedFileCategories);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Equal(currentProfile, host.CaptureCurrentProfile());
    }

    [Fact]
    public async Task ExportCommand_Should_Use_Staged_Category_Selection_Without_Applying_To_Current_Session()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-export",
            name: "Export Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        WorkspaceProfileDto currentProfile = CreateProfile();
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: null,
            profile: currentProfile);
        TestContext context = CreateContext(
            [entry],
            host,
            selectedExportPath: @"D:\Exports\profile.filemerger.profile.json");

        await context.ViewModel.InitializeAsync();
        context.ViewModel.Editor.IncludeManualExclusionsInSkippedMetadata = false;

        context.ViewModel.ExportCommand.Execute(null);

        Assert.NotNull(context.LibraryService.LastExportedEntry);
        Assert.Equal(
            selection with { IncludeManualExclusions = false },
            context.LibraryService.LastExportedEntry!.Profile.SkippedFileCategories);
        Assert.Equal(@"D:\Exports\profile.filemerger.profile.json", context.LibraryService.LastExportTargetPath);
        Assert.Equal(currentProfile, host.CaptureCurrentProfile());
    }

    [Fact]
    public async Task DeleteCommand_Should_Use_Explicit_Delete_And_Cancel_Labels()
    {
        ProfileLibraryEntry source = CreateEntry(id: "profile-delete", name: "User Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source], host);
        context.UserPromptService.ConfirmResult = false;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;

        Assert.True(viewModel.DeleteCommand.CanExecute(null));

        viewModel.DeleteCommand.Execute(null);

        Assert.Equal(1, context.UserPromptService.ConfirmCalls);
        Assert.Equal("Delete profile", context.UserPromptService.LastConfirmTitle);
        Assert.Equal("Delete profile 'User Profile'?", context.UserPromptService.LastConfirmMessage);
        Assert.Equal("Delete", context.UserPromptService.LastConfirmButtonText);
        Assert.Equal("Cancel", context.UserPromptService.LastCancelButtonText);
        Assert.Same(source, Assert.Single(context.LibraryService.Entries));
        Assert.Equal(source.Id, viewModel.SelectedProfile?.Id);
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Preserve_EntryId_When_Category_Selection_Is_Unchanged()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-apply",
            name: "Apply Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: null,
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        context.ViewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal("Apply Profile", host.CurrentProfileName);
        Assert.Equal("profile-apply", host.CurrentProfileEntryId);
        Assert.Equal(selection, host.CaptureCurrentProfile().SkippedFileCategories);
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Clear_EntryId_When_Category_Selection_Changes()
    {
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-apply",
            name: "Apply Profile",
            profile: CreateProfile(skippedFileCategories: selection));
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();
        context.ViewModel.Editor.IncludeProfileExclusionsInSkippedMetadata = true;

        context.ViewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal("Apply Profile", host.CurrentProfileName);
        Assert.Null(host.CurrentProfileEntryId);
        Assert.Equal(
            selection with { IncludeProfileExclusions = true },
            host.CaptureCurrentProfile().SkippedFileCategories);
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Update_Availability_And_Apply_Corrected_Draft()
    {
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-apply",
            name: "Apply Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]));
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: CreateProfile());
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        List<bool> canExecuteStates = [];
        viewModel.ApplyToCurrentSessionCommand.CanExecuteChanged += (_, _) =>
            canExecuteStates.Add(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));

        Assert.True(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));

        rule.Pattern = string.Empty;

        Assert.False(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));
        Assert.Contains(false, canExecuteStates);

        viewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal(0, host.ApplyCallCount);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);

        canExecuteStates.Clear();
        rule.Pattern = "obj";

        Assert.True(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));
        Assert.Contains(true, canExecuteStates);
        Assert.Null(viewModel.Editor.DraftValidationMessage);

        viewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal(1, host.ApplyCallCount);
        Assert.Equal("Apply Profile", host.CurrentProfileName);
        Assert.Null(host.CurrentProfileEntryId);
        Assert.Equal("obj", Assert.Single(host.CaptureCurrentProfile().FilterRules!).Pattern);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.True(viewModel.IsDirty);
        Assert.Null(context.LibraryService.LastSavedEntry);
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Reject_Invalid_Draft_Without_Mutating_Session_Or_Editor()
    {
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-apply",
            name: "Apply Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]));
        WorkspaceProfileDto currentProfile = CreateProfile(includeHeaderComment: true);
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: currentProfile);
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileLibraryListItemViewModel selectedProfile = Assert.Single(viewModel.Profiles);
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);

        viewModel.Editor.WorkingProfileName = "Edited Profile";
        viewModel.Description = "Unsaved description";
        rule.Pattern = string.Empty;
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        bool wasUsedByCurrentSession = selectedProfile.IsUsedByCurrentSession;

        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));

        viewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal(0, host.ApplyCallCount);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Same(currentProfile, host.CaptureCurrentProfile());
        Assert.Same(selectedProfile, viewModel.SelectedProfile);
        Assert.Equal(wasUsedByCurrentSession, selectedProfile.IsUsedByCurrentSession);
        Assert.True(viewModel.IsDirty);
        Assert.Equal("Edited Profile", viewModel.Editor.WorkingProfileName);
        Assert.Equal("Unsaved description", viewModel.Description);
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.Equal(string.Empty, rule.Pattern);
        Assert.True(rule.HasValidationError);
        Assert.Equal("bin", Assert.Single(selectedProfile.Entry.Profile.FilterRules!).Pattern);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);
        Assert.Equal($"Profile was not applied. {viewModel.Editor.DraftValidationMessage}", viewModel.StatusMessage);
    }

    [Fact]
    public async Task InitializeAsync_Should_Load_Invalid_Profile_For_Correction_Without_Applying_It()
    {
        ProfileLibraryEntry entry = CreateEntry(
            id: "profile-invalid",
            name: "Invalid Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule(string.Empty)]));
        WorkspaceProfileDto currentProfile = CreateProfile();
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: currentProfile);
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileLibraryListItemViewModel selectedProfile = Assert.Single(viewModel.Profiles);
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);

        Assert.Same(selectedProfile, viewModel.SelectedProfile);
        Assert.Equal(string.Empty, rule.Pattern);
        Assert.True(rule.HasValidationError);
        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));
        Assert.Equal(0, host.ApplyCallCount);
        Assert.Same(currentProfile, host.CaptureCurrentProfile());

        viewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal(0, host.ApplyCallCount);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Same(currentProfile, host.CaptureCurrentProfile());
        Assert.Same(selectedProfile, viewModel.SelectedProfile);
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.False(viewModel.IsDirty);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);

        rule.Pattern = "bin";

        Assert.True(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));

        viewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal(1, host.ApplyCallCount);
        Assert.Equal("Invalid Profile", host.CurrentProfileName);
        Assert.Null(host.CurrentProfileEntryId);
        Assert.Equal("bin", Assert.Single(host.CaptureCurrentProfile().FilterRules!).Pattern);
        Assert.Equal(string.Empty, Assert.Single(selectedProfile.Entry.Profile.FilterRules!).Pattern);
        Assert.True(viewModel.IsDirty);
        Assert.Null(context.LibraryService.LastSavedEntry);
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Not_Apply_While_Busy_Even_When_Executed_Directly()
    {
        ProfileLibraryEntry entry = CreateEntry(id: "profile-apply", name: "Apply Profile");
        WorkspaceProfileDto currentProfile = CreateProfile(includeHeaderComment: true);
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: currentProfile);
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        bool attemptedWhileBusy = false;
        bool? canApplyWhileBusy = null;
        bool? canExecuteWhileBusy = null;
        string? statusBeforeAttempt = null;
        string? statusAfterAttempt = null;

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(ProfileManagerViewModel.IsBusy) || !viewModel.IsBusy)
                return;

            attemptedWhileBusy = true;
            canApplyWhileBusy = viewModel.CanApply;
            canExecuteWhileBusy = viewModel.ApplyToCurrentSessionCommand.CanExecute(null);
            statusBeforeAttempt = viewModel.StatusMessage;

            viewModel.ApplyToCurrentSessionCommand.Execute(null);

            statusAfterAttempt = viewModel.StatusMessage;
        };

        await viewModel.InitializeAsync();

        Assert.True(attemptedWhileBusy);
        Assert.Equal(true, canApplyWhileBusy);
        Assert.Equal(false, canExecuteWhileBusy);
        Assert.Equal(statusBeforeAttempt, statusAfterAttempt);
        Assert.Equal(0, host.ApplyCallCount);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Same(currentProfile, host.CaptureCurrentProfile());
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));
    }

    [Fact]
    public async Task ApplyToCurrentSessionCommand_Should_Not_Apply_During_Profile_Creation()
    {
        ProfileLibraryEntry entry = CreateEntry(id: "profile-existing", name: "Existing Profile");
        WorkspaceProfileDto currentProfile = CreateProfile(includeHeaderComment: true);
        FakeCurrentSessionProfileHost host = new(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: currentProfile);
        TestContext context = CreateContext([entry], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        viewModel.StartBlankProfileDraftCommand.Execute(null);
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        string profileNameBefore = viewModel.Editor.WorkingProfileName;
        bool wasDirty = viewModel.IsDirty;

        Assert.True(viewModel.IsCreatingProfileDraft);
        Assert.True(viewModel.Editor.CanUseProfile);
        Assert.False(viewModel.ApplyToCurrentSessionCommand.CanExecute(null));

        viewModel.ApplyToCurrentSessionCommand.Execute(null);

        Assert.Equal(0, host.ApplyCallCount);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Same(currentProfile, host.CaptureCurrentProfile());
        Assert.True(viewModel.IsCreatingProfileDraft);
        Assert.Equal(wasDirty, viewModel.IsDirty);
        Assert.Equal(profileNameBefore, viewModel.Editor.WorkingProfileName);
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.Null(context.LibraryService.LastSavedEntry);
    }

    [Theory]
    [InlineData("user", false)]
    [InlineData("user", true)]
    [InlineData("duplicate", false)]
    [InlineData("duplicate", true)]
    [InlineData("built-in", false)]
    [InlineData("built-in", true)]
    [InlineData("read-only", false)]
    [InlineData("read-only", true)]
    public async Task UnsavedGuard_Should_Reject_Invalid_Draft_And_Save_Only_After_Correction(
        string profileKind,
        bool closeLibrary)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]),
            isBuiltIn: profileKind == "built-in",
            isReadOnly: profileKind == "read-only",
            filePath: @"D:\Profiles\source.filemerger.profile.json");
        ProfileLibraryEntry other = CreateEntry(id: "profile-other", name: "Other Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        WorkspaceProfileDto currentProfile = host.CaptureCurrentProfile();
        var promptService = new FakeUserPromptService
        {
            UnsavedChangesDecision = UnsavedChangesDecision.Save
        };
        TestContext context = CreateContext([source, other], host, userPromptService: promptService);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        if (profileKind == "duplicate")
        {
            viewModel.DuplicateCommand.Execute(null);
            Assert.Null(viewModel.SelectedProfile);
            Assert.False(viewModel.IsCreatingProfileDraft);
        }

        ProfileLibraryListItemViewModel? selectedBefore = viewModel.SelectedProfile;
        ProfileLibraryListItemViewModel[] profilesBefore = viewModel.Profiles.ToArray();
        ProfileLibraryEntry[] entriesBefore = context.LibraryService.Entries.ToArray();
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        viewModel.Editor.WorkingProfileName = "Edited Profile";
        viewModel.Description = "Unsaved description";
        rule.Pattern = string.Empty;
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        int loadCallsBefore = context.LibraryService.LoadCallCount;
        int getAllCallsBefore = context.LibraryService.GetAllCallCount;
        bool? selectedDirtyBefore = selectedBefore?.IsDirty;
        bool? selectedActiveBefore = selectedBefore?.IsUsedByCurrentSession;

        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.CanSave);
        Assert.False(viewModel.CanSaveAs);

        bool allowed = await RequestLeaveEditorAsync(viewModel, closeLibrary);

        Assert.False(allowed);
        Assert.Same(promptService, context.UserPromptService);
        Assert.Equal(1, promptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Equal(loadCallsBefore, context.LibraryService.LoadCallCount);
        Assert.Equal(getAllCallsBefore, context.LibraryService.GetAllCallCount);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Equal(entriesBefore, context.LibraryService.Entries.ToArray());
        Assert.Equal(profilesBefore, viewModel.Profiles.ToArray());
        Assert.Same(selectedBefore, viewModel.SelectedProfile);
        Assert.Equal(selectedDirtyBefore, viewModel.SelectedProfile?.IsDirty);
        Assert.Equal(selectedActiveBefore, viewModel.SelectedProfile?.IsUsedByCurrentSession);
        Assert.Equal("Edited Profile", viewModel.Editor.WorkingProfileName);
        Assert.Equal("Unsaved description", viewModel.Description);
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.Equal(string.Empty, rule.Pattern);
        Assert.True(rule.HasValidationError);
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);
        Assert.Equal($"Profile was not saved. {viewModel.Editor.DraftValidationMessage}", viewModel.StatusMessage);
        Assert.Equal(0, host.ApplyCallCount);
        Assert.Equal("Current Profile", host.CurrentProfileName);
        Assert.Equal("profile-current", host.CurrentProfileEntryId);
        Assert.Same(currentProfile, host.CaptureCurrentProfile());

        rule.Pattern = "obj";

        Assert.True(viewModel.CanSave);
        Assert.True(await RequestLeaveEditorAsync(viewModel, closeLibrary));

        bool saveAsNew = profileKind != "user";
        ProfileLibraryEntry saved = Assert.IsType<ProfileLibraryEntry>(context.LibraryService.LastSavedEntry);
        Assert.Equal(2, promptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(saveAsNew, context.LibraryService.LastSaveAsNew);
        Assert.Equal("Edited Profile", saved.DisplayName);
        Assert.Equal("Unsaved description", saved.Description);
        Assert.Equal("obj", Assert.Single(saved.Profile.FilterRules!).Pattern);
        Assert.Equal(saveAsNew ? 3 : 2, context.LibraryService.Entries.Count);
        Assert.False(saved.IsBuiltIn);
        Assert.False(saved.IsReadOnly);
        Assert.Equal(ProfileEntryKind.User, saved.Kind);

        if (saveAsNew)
        {
            Assert.NotEqual(source.Id, saved.Id);
            Assert.Null(saved.FilePath);
            Assert.Same(source, context.LibraryService.Entries.Single(x => x.Id == source.Id));
        }
        else
        {
            Assert.Equal(source.Id, saved.Id);
            Assert.Equal(source.FilePath, saved.FilePath);
            Assert.Equal(source.CreatedAtUtc, saved.CreatedAtUtc);
        }

        Assert.Equal(closeLibrary ? saved.Id : other.Id, viewModel.SelectedProfile?.Id);
        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);
        Assert.Same(currentProfile, host.CaptureCurrentProfile());
    }

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(false, " \t ")]
    [InlineData(true, " \t ")]
    public async Task UnsavedGuard_Should_Reject_Raw_Empty_Name_And_Save_After_Correction(
        bool closeLibrary,
        string invalidName)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]),
            filePath: @"D:\Profiles\source.filemerger.profile.json");
        ProfileLibraryEntry other = CreateEntry(id: "profile-other", name: "Other Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source, other], host);
        context.UserPromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileLibraryListItemViewModel? selectedBefore = viewModel.SelectedProfile;
        SetRawProfileNameForSaveBoundaryTest(viewModel.Editor, invalidName);
        viewModel.Description = "Unsaved description";
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        int loadCallsBefore = context.LibraryService.LoadCallCount;
        int getAllCallsBefore = context.LibraryService.GetAllCallCount;

        Assert.True(viewModel.Editor.CanUseProfile);
        Assert.False(viewModel.CanSave);
        Assert.False(await RequestLeaveEditorAsync(viewModel, closeLibrary));

        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Equal(loadCallsBefore, context.LibraryService.LoadCallCount);
        Assert.Equal(getAllCallsBefore, context.LibraryService.GetAllCallCount);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Equal(new[] { source, other }, context.LibraryService.Entries.ToArray());
        Assert.Same(selectedBefore, viewModel.SelectedProfile);
        Assert.Equal(invalidName, viewModel.Editor.WorkingProfileName);
        Assert.Equal("Unsaved description", viewModel.Description);
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.True(viewModel.IsDirty);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);
        Assert.Equal("Profile was not saved. Profile name cannot be empty.", viewModel.StatusMessage);

        viewModel.Editor.WorkingProfileName = "Corrected Profile";

        Assert.True(await RequestLeaveEditorAsync(viewModel, closeLibrary));

        ProfileLibraryEntry saved = Assert.IsType<ProfileLibraryEntry>(context.LibraryService.LastSavedEntry);
        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(false, context.LibraryService.LastSaveAsNew);
        Assert.Equal(source.Id, saved.Id);
        Assert.Equal(source.FilePath, saved.FilePath);
        Assert.Equal("Corrected Profile", saved.DisplayName);
        Assert.Equal("Unsaved description", saved.Description);
        Assert.Equal("bin", Assert.Single(saved.Profile.FilterRules!).Pattern);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsavedGuard_Should_Report_Raw_Name_Error_Before_Filter_Error(bool closeLibrary)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]));
        ProfileLibraryEntry other = CreateEntry(id: "profile-other", name: "Other Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source, other], host);
        context.UserPromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileLibraryListItemViewModel? selectedBefore = viewModel.SelectedProfile;
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        SetRawProfileNameForSaveBoundaryTest(viewModel.Editor, string.Empty);
        rule.Pattern = string.Empty;

        Assert.False(await RequestLeaveEditorAsync(viewModel, closeLibrary));
        Assert.Equal("Profile was not saved. Profile name cannot be empty.", viewModel.StatusMessage);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);

        viewModel.Editor.WorkingProfileName = "Corrected Profile";

        Assert.False(await RequestLeaveEditorAsync(viewModel, closeLibrary));
        Assert.Equal($"Profile was not saved. {viewModel.Editor.DraftValidationMessage}", viewModel.StatusMessage);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);
        Assert.Same(selectedBefore, viewModel.SelectedProfile);
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.True(viewModel.IsDirty);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Equal(new[] { source, other }, context.LibraryService.Entries.ToArray());

        rule.Pattern = "obj";

        Assert.True(await RequestLeaveEditorAsync(viewModel, closeLibrary));
        Assert.Equal(3, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal("Corrected Profile", context.LibraryService.LastSavedEntry!.DisplayName);
        Assert.Equal("obj", Assert.Single(context.LibraryService.LastSavedEntry.Profile.FilterRules!).Pattern);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false, RulePatternType.Exact)]
    [InlineData(true, RulePatternType.Exact)]
    [InlineData(false, RulePatternType.Regex)]
    [InlineData(true, RulePatternType.Regex)]
    public async Task SaveCommands_Should_Update_Availability_And_Persist_Corrected_Rules(
        bool saveAsNew,
        RulePatternType patternType)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(
                filterRules:
                [
                    ExcludeDirectoryRule("bin") with { PatternType = patternType }
                ]),
            filePath: @"D:\Profiles\source.filemerger.profile.json");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        List<bool> saveStates = [];
        List<bool> saveAsStates = [];
        viewModel.SaveCommand.CanExecuteChanged += (_, _) => saveStates.Add(viewModel.SaveCommand.CanExecute(null));
        viewModel.SaveAsCommand.CanExecuteChanged += (_, _) =>
            saveAsStates.Add(viewModel.SaveAsCommand.CanExecute(null));

        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.True(viewModel.SaveAsCommand.CanExecute(null));

        rule.Pattern = patternType == RulePatternType.Regex ? "[" : string.Empty;

        Assert.False(viewModel.CanSave);
        Assert.False(viewModel.CanSaveAs);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
        Assert.False(viewModel.SaveAsCommand.CanExecute(null));
        Assert.Contains(false, saveStates);
        Assert.Contains(false, saveAsStates);
        string statusBefore = viewModel.StatusMessage;

        viewModel.SaveCommand.Execute(null);
        viewModel.SaveAsCommand.Execute(null);

        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Equal(statusBefore, viewModel.StatusMessage);
        Assert.Same(source, Assert.Single(context.LibraryService.Entries));
        Assert.True(viewModel.IsDirty);

        saveStates.Clear();
        saveAsStates.Clear();
        rule.Pattern = "obj";

        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.True(viewModel.SaveAsCommand.CanExecute(null));
        Assert.Contains(true, saveStates);
        Assert.Contains(true, saveAsStates);
        Assert.Null(viewModel.Editor.DraftValidationMessage);

        AsyncRelayCommand command = saveAsNew ? viewModel.SaveAsCommand : viewModel.SaveCommand;
        command.Execute(null);

        ProfileLibraryEntry saved = Assert.IsType<ProfileLibraryEntry>(context.LibraryService.LastSavedEntry);
        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(saveAsNew, context.LibraryService.LastSaveAsNew);
        Assert.Equal(saveAsNew ? 2 : 1, context.LibraryService.Entries.Count);
        WorkspaceFileFilterRuleDto savedRule = Assert.Single(saved.Profile.FilterRules!);
        Assert.Equal("obj", savedRule.Pattern);
        Assert.Equal(patternType, savedRule.PatternType);
        Assert.Equal(FilterMode.Exclude, savedRule.Mode);
        Assert.Equal(FilterTarget.DirectorySegment, savedRule.Target);

        if (saveAsNew)
        {
            Assert.NotEqual(source.Id, saved.Id);
            Assert.Null(saved.FilePath);
            Assert.Same(source, context.LibraryService.Entries.Single(x => x.Id == source.Id));
        }
        else
        {
            Assert.Equal(source.Id, saved.Id);
            Assert.Equal(source.FilePath, saved.FilePath);
            Assert.Equal(source.CreatedAtUtc, saved.CreatedAtUtc);
        }

        Assert.Equal(saved.Id, viewModel.SelectedProfile?.Id);
        Assert.Equal("obj", Assert.Single(viewModel.Editor.FilterRules).Pattern);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.Equal(
            saveAsNew ? "Profile 'Source Profile' saved as new." : "Profile 'Source Profile' saved.",
            viewModel.StatusMessage);
        Assert.Equal(0, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task SaveCommands_Should_Not_Execute_With_Raw_Empty_Name(string invalidName)
    {
        ProfileLibraryEntry source = CreateEntry(id: "profile-source", name: "Source Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        SetRawProfileNameForSaveBoundaryTest(viewModel.Editor, invalidName);
        viewModel.Description = "Unsaved description";
        string statusBefore = viewModel.StatusMessage;
        int loadCallsBefore = context.LibraryService.LoadCallCount;
        int getAllCallsBefore = context.LibraryService.GetAllCallCount;

        Assert.True(viewModel.Editor.CanUseProfile);
        Assert.False(viewModel.CanSave);
        Assert.False(viewModel.CanSaveAs);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
        Assert.False(viewModel.SaveAsCommand.CanExecute(null));

        viewModel.SaveCommand.Execute(null);
        viewModel.SaveAsCommand.Execute(null);

        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Equal(loadCallsBefore, context.LibraryService.LoadCallCount);
        Assert.Equal(getAllCallsBefore, context.LibraryService.GetAllCallCount);
        Assert.Same(source, Assert.Single(context.LibraryService.Entries));
        Assert.Equal(invalidName, viewModel.Editor.WorkingProfileName);
        Assert.Equal(statusBefore, viewModel.StatusMessage);
        Assert.True(viewModel.IsDirty);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("built-in")]
    [InlineData("read-only")]
    public async Task SaveCommand_Should_Create_User_Copy_For_Unsaved_Or_ReadOnly_Profile(string profileKind)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]),
            isBuiltIn: profileKind == "built-in",
            isReadOnly: profileKind == "read-only",
            filePath: @"D:\Profiles\source.filemerger.profile.json");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        if (profileKind == "duplicate")
        {
            viewModel.DuplicateCommand.Execute(null);
            Assert.Null(viewModel.SelectedProfile);
        }

        viewModel.Editor.WorkingProfileName = "User Copy";
        viewModel.Description = "Edited description";
        Assert.Single(viewModel.Editor.FilterRules).Pattern = "obj";

        Assert.True(viewModel.SaveCommand.CanExecute(null));
        viewModel.SaveCommand.Execute(null);

        ProfileLibraryEntry saved = Assert.IsType<ProfileLibraryEntry>(context.LibraryService.LastSavedEntry);
        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(true, context.LibraryService.LastSaveAsNew);
        Assert.Equal(2, context.LibraryService.Entries.Count);
        Assert.Same(source, context.LibraryService.Entries.Single(x => x.Id == source.Id));
        Assert.NotEqual(source.Id, saved.Id);
        Assert.Null(saved.FilePath);
        Assert.Equal(ProfileEntryKind.User, saved.Kind);
        Assert.False(saved.IsBuiltIn);
        Assert.False(saved.IsReadOnly);
        Assert.Equal("User Copy", saved.DisplayName);
        Assert.Equal("Edited description", saved.Description);
        Assert.Equal("obj", Assert.Single(saved.Profile.FilterRules!).Pattern);
        Assert.Equal(saved.Id, viewModel.SelectedProfile?.Id);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateProfileDraftCommand_Should_Require_Valid_Content_And_Save_As_New(bool fromTemplate)
    {
        ProfileLibraryEntry template = CreateEntry(
            id: "profile-template",
            name: "Template Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]),
            isBuiltIn: true);
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([template], host);

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        StartCreationDraft(viewModel, fromTemplate);
        Assert.True(viewModel.IsCreatingProfileDraft);
        Assert.True(viewModel.CreateProfileDraftCommand.CanExecute(null));
        Assert.False(viewModel.CanSave);
        Assert.False(viewModel.CanSaveAs);

        viewModel.Editor.WorkingProfileName = "Created Profile";
        viewModel.Description = "Created description";
        viewModel.Editor.FilterRules.Clear();
        ProfileFilterRuleItemViewModel rule = new(ExcludeDirectoryRule("bin"));
        viewModel.Editor.FilterRules.Add(rule);
        List<bool> createStates = [];
        viewModel.CreateProfileDraftCommand.CanExecuteChanged += (_, _) =>
            createStates.Add(viewModel.CreateProfileDraftCommand.CanExecute(null));

        rule.Pattern = string.Empty;
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();

        Assert.False(viewModel.CanCreateProfileDraft);
        Assert.False(viewModel.CreateProfileDraftCommand.CanExecute(null));
        Assert.Contains(false, createStates);

        viewModel.CreateProfileDraftCommand.Execute(null);
        viewModel.SaveCommand.Execute(null);
        viewModel.SaveAsCommand.Execute(null);

        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Same(template, Assert.Single(context.LibraryService.Entries));
        Assert.True(viewModel.IsCreatingProfileDraft);
        Assert.True(viewModel.IsDirty);
        Assert.Null(viewModel.SelectedProfile);
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());

        rule.Pattern = "obj";
        SetRawProfileNameForSaveBoundaryTest(viewModel.Editor, " \t ");

        Assert.True(viewModel.Editor.CanUseProfile);
        Assert.False(viewModel.CreateProfileDraftCommand.CanExecute(null));

        viewModel.CreateProfileDraftCommand.Execute(null);

        Assert.Equal(0, context.LibraryService.SaveCallCount);
        createStates.Clear();
        viewModel.Editor.WorkingProfileName = "Created Profile";

        Assert.True(viewModel.CanCreateProfileDraft);
        Assert.True(viewModel.CreateProfileDraftCommand.CanExecute(null));
        Assert.Contains(true, createStates);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
        Assert.False(viewModel.SaveAsCommand.CanExecute(null));

        viewModel.SaveCommand.Execute(null);
        viewModel.SaveAsCommand.Execute(null);
        Assert.Equal(0, context.LibraryService.SaveCallCount);

        viewModel.CreateProfileDraftCommand.Execute(null);

        ProfileLibraryEntry saved = Assert.IsType<ProfileLibraryEntry>(context.LibraryService.LastSavedEntry);
        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(true, context.LibraryService.LastSaveAsNew);
        Assert.NotEqual(template.Id, saved.Id);
        Assert.Equal("Created Profile", saved.DisplayName);
        Assert.Equal("Created description", saved.Description);
        Assert.Equal("obj", Assert.Single(saved.Profile.FilterRules!).Pattern);
        Assert.Equal(ProfileEntryKind.User, saved.Kind);
        Assert.False(saved.IsReadOnly);
        Assert.Equal(2, context.LibraryService.Entries.Count);
        Assert.Same(template, context.LibraryService.Entries.Single(x => x.Id == template.Id));
        Assert.Equal(saved.Id, viewModel.SelectedProfile?.Id);
        Assert.False(viewModel.IsCreatingProfileDraft);
        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(string.Empty, viewModel.ProfileCreationSourceLabel);
        Assert.Equal("Profile 'Created Profile' created.", viewModel.StatusMessage);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.Equal(0, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CancelProfileDraftCommand_Should_Respect_Discard_Decision_For_Invalid_Draft(
        bool fromTemplate,
        bool discard)
    {
        ProfileLibraryEntry template = CreateEntry(id: "profile-template", name: "Template Profile", isBuiltIn: true);
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([template], host);
        context.UserPromptService.ConfirmResult = discard;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        StartCreationDraft(viewModel, fromTemplate);
        viewModel.Editor.WorkingProfileName = "Unsaved Draft";
        viewModel.Editor.FilterRules.Clear();
        ProfileFilterRuleItemViewModel rule = new(ExcludeDirectoryRule(string.Empty));
        viewModel.Editor.FilterRules.Add(rule);
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();

        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.CanCreateProfileDraft);
        Assert.True(viewModel.CancelProfileDraftCommand.CanExecute(null));

        viewModel.CancelProfileDraftCommand.Execute(null);

        Assert.Equal(1, context.UserPromptService.ConfirmCalls);
        Assert.Equal("Discard profile draft", context.UserPromptService.LastConfirmTitle);
        Assert.Equal("Yes", context.UserPromptService.LastConfirmButtonText);
        Assert.Equal("No", context.UserPromptService.LastCancelButtonText);
        Assert.Equal(0, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Same(template, Assert.Single(context.LibraryService.Entries));
        Assert.Equal(0, host.ApplyCallCount);

        if (discard)
        {
            Assert.False(viewModel.IsCreatingProfileDraft);
            Assert.False(viewModel.IsDirty);
            Assert.Equal(template.Id, viewModel.SelectedProfile?.Id);
            Assert.Equal(template.DisplayName, viewModel.Editor.WorkingProfileName);
        }
        else
        {
            Assert.True(viewModel.IsCreatingProfileDraft);
            Assert.True(viewModel.IsDirty);
            Assert.Null(viewModel.SelectedProfile);
            Assert.Equal("Unsaved Draft", viewModel.Editor.WorkingProfileName);
            Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
            Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        }
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task CreationDraftGuard_Should_Use_Discard_Confirmation_Without_Saving(
        bool fromTemplate,
        bool closeLibrary,
        bool discard)
    {
        ProfileLibraryEntry template = CreateEntry(id: "profile-template", name: "Template Profile", isBuiltIn: true);
        ProfileLibraryEntry other = CreateEntry(id: "profile-other", name: "Other Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([template, other], host);
        context.UserPromptService.ConfirmResult = discard;
        context.UserPromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        StartCreationDraft(viewModel, fromTemplate);
        viewModel.Editor.FilterRules.Clear();
        ProfileFilterRuleItemViewModel rule = new(ExcludeDirectoryRule(string.Empty));
        viewModel.Editor.FilterRules.Add(rule);
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();

        bool allowed = await RequestLeaveEditorAsync(viewModel, closeLibrary);

        Assert.Equal(discard, allowed);
        Assert.Equal(1, context.UserPromptService.ConfirmCalls);
        Assert.Equal("Discard profile draft", context.UserPromptService.LastConfirmTitle);
        Assert.Equal(0, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Equal(new[] { template, other }, context.LibraryService.Entries.ToArray());
        Assert.Equal(0, host.ApplyCallCount);

        if (!discard || closeLibrary)
        {
            Assert.True(viewModel.IsCreatingProfileDraft);
            Assert.True(viewModel.IsDirty);
            Assert.Null(viewModel.SelectedProfile);
            Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
            Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        }
        else
        {
            Assert.False(viewModel.IsCreatingProfileDraft);
            Assert.False(viewModel.IsDirty);
            Assert.Equal(other.Id, viewModel.SelectedProfile?.Id);
        }
    }

    [Theory]
    [InlineData(false, UnsavedChangesDecision.Discard)]
    [InlineData(true, UnsavedChangesDecision.Discard)]
    [InlineData(false, UnsavedChangesDecision.Cancel)]
    [InlineData(true, UnsavedChangesDecision.Cancel)]
    public async Task UnsavedGuard_Should_Respect_Discard_And_Cancel_With_Invalid_Draft(
        bool closeLibrary,
        UnsavedChangesDecision decision)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]));
        ProfileLibraryEntry other = CreateEntry(id: "profile-other", name: "Other Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source, other], host);
        context.UserPromptService.UnsavedChangesDecision = decision;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileLibraryListItemViewModel? selectedBefore = viewModel.SelectedProfile;
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        viewModel.Editor.WorkingProfileName = "Unsaved Profile";
        viewModel.Description = "Unsaved description";
        rule.Pattern = string.Empty;
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();

        bool allowed = await RequestLeaveEditorAsync(viewModel, closeLibrary);

        Assert.Equal(decision == UnsavedChangesDecision.Discard, allowed);
        Assert.Equal(1, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Equal(new[] { source, other }, context.LibraryService.Entries.ToArray());
        Assert.Equal(0, host.ApplyCallCount);

        if (decision == UnsavedChangesDecision.Cancel || closeLibrary)
        {
            Assert.Same(selectedBefore, viewModel.SelectedProfile);
            Assert.Equal("Unsaved Profile", viewModel.Editor.WorkingProfileName);
            Assert.Equal("Unsaved description", viewModel.Description);
            Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
            Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
            Assert.True(viewModel.IsDirty);
        }
        else
        {
            Assert.Equal(other.Id, viewModel.SelectedProfile?.Id);
            Assert.Equal(other.DisplayName, viewModel.Editor.WorkingProfileName);
            Assert.False(viewModel.IsDirty);
        }
    }

    [Theory]
    [InlineData("new")]
    [InlineData("blank")]
    [InlineData("template")]
    [InlineData("duplicate")]
    [InlineData("import")]
    public async Task EditorReplacementCommands_Should_Stop_When_Guard_Save_Rejects_Invalid_Draft(string action)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]));
        ProfileLibraryEntry template = CreateEntry(id: "profile-template", name: "Template Profile", isBuiltIn: true);
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext(
            [source, template],
            host,
            selectedImportFiles: [@"D:\Profiles\import.filemerger.profile.json"]);
        context.UserPromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;
        context.LibraryService.ImportResult = [CreateEntry(id: "profile-imported", name: "Imported Profile")];

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileLibraryListItemViewModel? selectedBefore = viewModel.SelectedProfile;
        ProfileManagerPage pageBefore = viewModel.SelectedPage;
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        viewModel.Editor.WorkingProfileName = "Unsaved Profile";
        viewModel.Description = "Unsaved description";
        rule.Pattern = string.Empty;
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        int loadCallsBefore = context.LibraryService.LoadCallCount;
        int getAllCallsBefore = context.LibraryService.GetAllCallCount;

        switch (action)
        {
            case "new":
                viewModel.NewCommand.Execute(null);
                break;
            case "blank":
                viewModel.StartBlankProfileDraftCommand.Execute(null);
                break;
            case "template":
                viewModel.StartProfileTemplateDraftCommand.Execute(Assert.Single(viewModel.ProfileTemplates));
                break;
            case "duplicate":
                viewModel.DuplicateCommand.Execute(null);
                break;
            case "import":
                viewModel.ImportCommand.Execute(null);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }

        Assert.Equal(1, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Equal(loadCallsBefore, context.LibraryService.LoadCallCount);
        Assert.Equal(getAllCallsBefore, context.LibraryService.GetAllCallCount);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Null(context.LibraryService.LastImportedFilePaths);
        Assert.Equal(0, context.OpenFileDialogService.SelectFilesCallCount);
        Assert.Equal(new[] { source, template }, context.LibraryService.Entries.ToArray());
        Assert.Same(selectedBefore, viewModel.SelectedProfile);
        Assert.Equal(pageBefore, viewModel.SelectedPage);
        Assert.False(viewModel.IsCreatingProfileDraft);
        Assert.Equal("Unsaved Profile", viewModel.Editor.WorkingProfileName);
        Assert.Equal("Unsaved description", viewModel.Description);
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);
        Assert.Equal($"Profile was not saved. {viewModel.Editor.DraftValidationMessage}", viewModel.StatusMessage);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreviouslySavedInvalidProfile_Should_Remain_Editable_And_Save_Only_After_Correction(
        bool closeLibrary)
    {
        ProfileLibraryEntry invalid = CreateEntry(
            id: "profile-invalid",
            name: "Invalid Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule(string.Empty)]),
            filePath: @"D:\Profiles\invalid.filemerger.profile.json");
        ProfileLibraryEntry other = CreateEntry(id: "profile-other", name: "Other Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([invalid, other], host);
        context.UserPromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        Assert.Equal(string.Empty, Assert.Single(viewModel.Editor.FilterRules).Pattern);
        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.Editor.CanUseProfile);
        Assert.Equal(0, context.LibraryService.SaveCallCount);

        Assert.True(await RequestLeaveEditorAsync(viewModel, closeLibrary));
        Assert.Equal(0, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, context.LibraryService.SaveCallCount);

        if (!closeLibrary)
        {
            await viewModel.RequestSelectProfileAsync(viewModel.Profiles.Single(x => x.Id == invalid.Id));
        }

        ProfileLibraryListItemViewModel? selectedBefore = viewModel.SelectedProfile;
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        Assert.Equal(string.Empty, rule.Pattern);
        Assert.True(rule.HasValidationError);
        Assert.False(viewModel.IsDirty);

        viewModel.Description = "Unsaved correction notes";
        int loadCallsBefore = context.LibraryService.LoadCallCount;
        int getAllCallsBefore = context.LibraryService.GetAllCallCount;

        Assert.False(await RequestLeaveEditorAsync(viewModel, closeLibrary));

        Assert.Equal(1, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Equal(loadCallsBefore, context.LibraryService.LoadCallCount);
        Assert.Equal(getAllCallsBefore, context.LibraryService.GetAllCallCount);
        Assert.Same(invalid, context.LibraryService.Entries.Single(x => x.Id == invalid.Id));
        Assert.Same(selectedBefore, viewModel.SelectedProfile);
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.Equal(string.Empty, rule.Pattern);
        Assert.Equal("Unsaved correction notes", viewModel.Description);
        Assert.True(viewModel.IsDirty);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);

        rule.Pattern = "bin";

        Assert.True(await RequestLeaveEditorAsync(viewModel, closeLibrary));

        ProfileLibraryEntry saved = Assert.IsType<ProfileLibraryEntry>(context.LibraryService.LastSavedEntry);
        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(false, context.LibraryService.LastSaveAsNew);
        Assert.Equal(invalid.Id, saved.Id);
        Assert.Equal(invalid.FilePath, saved.FilePath);
        Assert.Equal("Unsaved correction notes", saved.Description);
        Assert.Equal("bin", Assert.Single(saved.Profile.FilterRules!).Pattern);
        Assert.Equal(
            "bin",
            Assert.Single(context.LibraryService.Entries.Single(x => x.Id == invalid.Id).Profile.FilterRules!).Pattern);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsavedGuard_Should_Not_Start_Save_While_Busy_Or_Reset_Active_Status(bool invalidDraft)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]));
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source], host);
        context.UserPromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        viewModel.Description = "Unsaved description";
        Assert.Single(viewModel.Editor.FilterRules).Pattern = invalidDraft ? string.Empty : "obj";
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        Task<bool>? closeAttempt = null;
        bool attemptedWhileBusy = false;
        bool? isBusyAfterAttempt = null;
        string? statusBeforeAttempt = null;
        string? statusAfterAttempt = null;
        StatusSeverity? severityBeforeAttempt = null;
        StatusSeverity? severityAfterAttempt = null;
        int? loadCallsBeforeAttempt = null;
        int? loadCallsAfterAttempt = null;
        int? getAllCallsBeforeAttempt = null;
        int? getAllCallsAfterAttempt = null;

        viewModel.PropertyChanged += (_, e) =>
        {
            if (attemptedWhileBusy ||
                e.PropertyName != nameof(ProfileManagerViewModel.StatusMessage) ||
                !viewModel.IsBusy ||
                viewModel.StatusMessage != "Loading profile library.")
            {
                return;
            }

            attemptedWhileBusy = true;
            statusBeforeAttempt = viewModel.StatusMessage;
            severityBeforeAttempt = viewModel.StatusSeverity;
            loadCallsBeforeAttempt = context.LibraryService.LoadCallCount;
            getAllCallsBeforeAttempt = context.LibraryService.GetAllCallCount;

            closeAttempt = viewModel.CanCloseAsync();

            isBusyAfterAttempt = viewModel.IsBusy;
            statusAfterAttempt = viewModel.StatusMessage;
            severityAfterAttempt = viewModel.StatusSeverity;
            loadCallsAfterAttempt = context.LibraryService.LoadCallCount;
            getAllCallsAfterAttempt = context.LibraryService.GetAllCallCount;
        };

        viewModel.RefreshCommand.Execute(null);

        Assert.True(attemptedWhileBusy);
        Assert.NotNull(closeAttempt);
        Assert.False(await closeAttempt!);
        Assert.Equal(true, isBusyAfterAttempt);
        Assert.Equal(statusBeforeAttempt, statusAfterAttempt);
        Assert.Equal(severityBeforeAttempt, severityAfterAttempt);
        Assert.Equal(loadCallsBeforeAttempt, loadCallsAfterAttempt);
        Assert.Equal(getAllCallsBeforeAttempt, getAllCallsAfterAttempt);
        Assert.Equal(1, context.UserPromptService.ConfirmUnsavedChangesCalls);
        Assert.Equal(0, context.LibraryService.SaveCallCount);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Same(source, Assert.Single(context.LibraryService.Entries));
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.Equal("Unsaved description", viewModel.Description);
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("Loaded 1 profile(s).", viewModel.StatusMessage);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task UnsavedGuard_Should_Keep_Dirty_Draft_When_Persistence_Fails(
        bool closeLibrary,
        bool duplicateDraft)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]),
            filePath: @"D:\Profiles\source.filemerger.profile.json");
        ProfileLibraryEntry other = CreateEntry(id: "profile-other", name: "Other Profile");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source, other], host);
        context.UserPromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;
        context.LibraryService.SaveException = new InvalidOperationException("Storage is unavailable.");

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        if (duplicateDraft)
            viewModel.DuplicateCommand.Execute(null);

        ProfileLibraryListItemViewModel? selectedBefore = viewModel.SelectedProfile;
        viewModel.Editor.WorkingProfileName = "Edited Profile";
        viewModel.Description = "Unsaved description";
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        rule.Pattern = "obj";
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        int loadCallsBefore = context.LibraryService.LoadCallCount;
        int getAllCallsBefore = context.LibraryService.GetAllCallCount;

        Assert.True(viewModel.Editor.CanUseProfile);
        Assert.False(await RequestLeaveEditorAsync(viewModel, closeLibrary));

        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(loadCallsBefore, context.LibraryService.LoadCallCount);
        Assert.Equal(getAllCallsBefore, context.LibraryService.GetAllCallCount);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Equal(new[] { source, other }, context.LibraryService.Entries.ToArray());
        Assert.Same(selectedBefore, viewModel.SelectedProfile);
        Assert.Equal("Edited Profile", viewModel.Editor.WorkingProfileName);
        Assert.Equal("Unsaved description", viewModel.Description);
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.Equal("Failed to save profile: Storage is unavailable.", viewModel.StatusMessage);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);

        context.LibraryService.SaveException = null;

        Assert.True(await RequestLeaveEditorAsync(viewModel, closeLibrary));

        ProfileLibraryEntry saved = Assert.IsType<ProfileLibraryEntry>(context.LibraryService.LastSavedEntry);
        Assert.Equal(2, context.LibraryService.SaveCallCount);
        Assert.Equal(duplicateDraft, context.LibraryService.LastSaveAsNew);
        Assert.Equal("obj", Assert.Single(saved.Profile.FilterRules!).Pattern);
        Assert.Equal("Unsaved description", saved.Description);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveCommands_Should_Preserve_Dirty_Draft_When_Persistence_Fails(bool saveAsNew)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            profile: CreateProfile(filterRules: [ExcludeDirectoryRule("bin")]),
            filePath: @"D:\Profiles\source.filemerger.profile.json");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source], host);
        context.LibraryService.SaveException = new InvalidOperationException("Storage is unavailable.");

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        ProfileLibraryListItemViewModel? selectedBefore = viewModel.SelectedProfile;
        viewModel.Description = "Unsaved description";
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        rule.Pattern = "obj";
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        int loadCallsBefore = context.LibraryService.LoadCallCount;
        int getAllCallsBefore = context.LibraryService.GetAllCallCount;
        AsyncRelayCommand command = saveAsNew ? viewModel.SaveAsCommand : viewModel.SaveCommand;

        Assert.True(command.CanExecute(null));
        command.Execute(null);

        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(loadCallsBefore, context.LibraryService.LoadCallCount);
        Assert.Equal(getAllCallsBefore, context.LibraryService.GetAllCallCount);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Same(source, Assert.Single(context.LibraryService.Entries));
        Assert.Same(selectedBefore, viewModel.SelectedProfile);
        Assert.Same(rule, Assert.Single(viewModel.Editor.FilterRules));
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.Equal("Unsaved description", viewModel.Description);
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.IsBusy);
        Assert.True(command.CanExecute(null));
        Assert.Equal("Failed to save profile: Storage is unavailable.", viewModel.StatusMessage);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateProfileDraftCommand_Should_Keep_Creation_Draft_When_Persistence_Fails(bool fromTemplate)
    {
        ProfileLibraryEntry template = CreateEntry(id: "profile-template", name: "Template Profile", isBuiltIn: true);
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([template], host);
        context.LibraryService.SaveException = new InvalidOperationException("Storage is unavailable.");

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        StartCreationDraft(viewModel, fromTemplate);
        viewModel.Editor.WorkingProfileName = "Created Profile";
        viewModel.Description = "Unsaved description";
        PreviewProfileStateSnapshot draftBefore = viewModel.Editor.BuildPreviewProfileSnapshot();
        string sourceLabelBefore = viewModel.ProfileCreationSourceLabel;
        int loadCallsBefore = context.LibraryService.LoadCallCount;
        int getAllCallsBefore = context.LibraryService.GetAllCallCount;

        Assert.True(viewModel.CreateProfileDraftCommand.CanExecute(null));
        viewModel.CreateProfileDraftCommand.Execute(null);

        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(loadCallsBefore, context.LibraryService.LoadCallCount);
        Assert.Equal(getAllCallsBefore, context.LibraryService.GetAllCallCount);
        Assert.Null(context.LibraryService.LastSavedEntry);
        Assert.Same(template, Assert.Single(context.LibraryService.Entries));
        Assert.True(viewModel.IsCreatingProfileDraft);
        Assert.True(viewModel.IsDirty);
        Assert.False(viewModel.IsBusy);
        Assert.Null(viewModel.SelectedProfile);
        Assert.Equal("Created Profile", viewModel.Editor.WorkingProfileName);
        Assert.Equal("Unsaved description", viewModel.Description);
        Assert.Equal(sourceLabelBefore, viewModel.ProfileCreationSourceLabel);
        Assert.Equal(draftBefore, viewModel.Editor.BuildPreviewProfileSnapshot());
        Assert.True(viewModel.CreateProfileDraftCommand.CanExecute(null));
        Assert.Equal("Failed to save profile: Storage is unavailable.", viewModel.StatusMessage);
        Assert.Equal(StatusSeverity.Error, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);

        context.LibraryService.SaveException = null;
        viewModel.CreateProfileDraftCommand.Execute(null);

        Assert.Equal(2, context.LibraryService.SaveCallCount);
        Assert.Equal(true, context.LibraryService.LastSaveAsNew);
        Assert.Equal(2, context.LibraryService.Entries.Count);
        Assert.False(viewModel.IsCreatingProfileDraft);
        Assert.False(viewModel.IsDirty);
        Assert.Equal("Profile 'Created Profile' created.", viewModel.StatusMessage);
        Assert.Equal(StatusSeverity.Success, viewModel.StatusSeverity);
        Assert.Equal(0, host.ApplyCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task SaveGuard_Should_Preserve_Existing_Empty_Name_Normalization(string name)
    {
        ProfileLibraryEntry source = CreateEntry(
            id: "profile-source",
            name: "Source Profile",
            filePath: @"D:\Profiles\source.filemerger.profile.json");
        FakeCurrentSessionProfileHost host = CreateCurrentSessionHost();
        TestContext context = CreateContext([source], host);
        context.UserPromptService.UnsavedChangesDecision = UnsavedChangesDecision.Save;

        await context.ViewModel.InitializeAsync();

        ProfileManagerViewModel viewModel = context.ViewModel;
        viewModel.Editor.WorkingProfileName = name;

        Assert.Equal("Default", viewModel.Editor.WorkingProfileName);
        Assert.True(viewModel.CanSave);
        Assert.True(viewModel.IsDirty);
        Assert.True(await viewModel.CanCloseAsync());

        ProfileLibraryEntry saved = Assert.IsType<ProfileLibraryEntry>(context.LibraryService.LastSavedEntry);
        Assert.Equal("Default", saved.DisplayName);
        Assert.Equal(source.Id, saved.Id);
        Assert.Equal(source.FilePath, saved.FilePath);
        Assert.Equal(1, context.LibraryService.SaveCallCount);
        Assert.Equal(false, context.LibraryService.LastSaveAsNew);
        Assert.False(viewModel.IsDirty);
        Assert.Equal(0, host.ApplyCallCount);
    }

    private static async Task<bool> RequestLeaveEditorAsync(ProfileManagerViewModel viewModel, bool closeLibrary)
    {
        if (closeLibrary)
            return await viewModel.CanCloseAsync();

        ProfileLibraryListItemViewModel candidate = viewModel.Profiles.Single(x => x.Id == "profile-other");
        await viewModel.RequestSelectProfileAsync(candidate);

        return viewModel.SelectedProfile?.Id == candidate.Id;
    }

    private static void StartCreationDraft(ProfileManagerViewModel viewModel, bool fromTemplate)
    {
        if (fromTemplate)
        {
            ProfileTemplateItemViewModel template = Assert.Single(viewModel.ProfileTemplates);
            Assert.True(viewModel.StartProfileTemplateDraftCommand.CanExecute(template));
            viewModel.StartProfileTemplateDraftCommand.Execute(template);
        }
        else
        {
            Assert.True(viewModel.StartBlankProfileDraftCommand.CanExecute(null));
            viewModel.StartBlankProfileDraftCommand.Execute(null);
        }

        Assert.True(viewModel.IsCreatingProfileDraft);
    }

    private static void SetRawProfileNameForSaveBoundaryTest(ProfileEditorViewModel editor, string name)
    {
        FieldInfo? field = typeof(ProfileEditorViewModel).GetField(
            "_workingProfileName",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        field.SetValue(editor, name);
    }

    private static FakeCurrentSessionProfileHost CreateCurrentSessionHost()
    {
        return new FakeCurrentSessionProfileHost(
            currentProfileName: "Current Profile",
            currentProfileEntryId: "profile-current",
            profile: CreateProfile(includeHeaderComment: true));
    }

    private static TestContext CreateContext(
        IReadOnlyCollection<ProfileLibraryEntry> entries,
        FakeCurrentSessionProfileHost currentSessionProfileHost,
        IReadOnlyList<string>? selectedImportFiles = null,
        string? selectedExportPath = null,
        FakeUserPromptService? userPromptService = null,
        ProfileManagerContext context = ProfileManagerContext.CurrentSession)
    {
        var libraryService = new FakeProfileLibraryService(entries);
        var openFileDialogService = new FakeOpenFileDialogService
        {
            SelectedFiles = selectedImportFiles ?? []
        };
        var saveFileDialogService = new FakeSaveFileDialogService
        {
            SelectedPath = selectedExportPath
        };

        userPromptService ??= new FakeUserPromptService();

        ProfileManagerViewModel viewModel = new(
            libraryService,
            new FakeProfileEditorFactory(),
            currentSessionProfileHost,
            userPromptService,
            openFileDialogService,
            saveFileDialogService,
            new FakeClipboardService(),
            context);

        return new TestContext(viewModel, libraryService, userPromptService, openFileDialogService);
    }

    private static ProfileLibraryEntry CreateEntry(
        string id,
        string name,
        WorkspaceProfileDto? profile = null,
        bool isBuiltIn = false,
        bool isReadOnly = false,
        string? filePath = null)
    {
        return new ProfileLibraryEntry(
            Metadata: new ProfileMetadataDto(
                Id: id,
                Name: name,
                Description: null,
                CreatedAtUtc: DateTime.UtcNow,
                UpdatedAtUtc: DateTime.UtcNow,
                IsBuiltIn: isBuiltIn,
                IsReadOnly: isReadOnly),
            Profile: profile ?? CreateProfile(),
            FilePath: filePath,
            Kind: isBuiltIn ? ProfileEntryKind.BuiltIn : ProfileEntryKind.User);
    }

    private static WorkspaceProfileDto CreateProfile(
        bool includeHeaderComment = false,
        bool includeSourceExcludedFiles = false,
        SkippedFileCategorySelection? skippedFileCategories = null,
        List<WorkspaceFileFilterRuleDto>? filterRules = null)
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment: includeHeaderComment,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            FileTypes:
            [
                new WorkspaceFileTypeDto(
                    Extension: ".cs",
                    DisplayName: "C# source",
                    Kind: FileKind.CSharp,
                    IsEnabled: true,
                    SupportsLanguageSpecificProcessing: true)
            ],
            LineEndingMode: LineEndingMode.Preserve,
            SortMode: SortMode.ByRelativePathAscending,
            InputEncodingMode: InputEncodingMode.Auto,
            PreferredInputEncodingName: null,
            FallbackInputEncodingName: "windows-1251",
            FilterRules: filterRules,
            IncludeSourceExcludedFiles: includeSourceExcludedFiles,
            SkippedFileCategories: skippedFileCategories);
    }

    private static WorkspaceFileFilterRuleDto ExcludeDirectoryRule(string pattern)
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: pattern,
            IsEnabled: true,
            Description: "Exclude build output",
            IsUserEditable: true);
    }

    private static SkippedFileCategorySelection CreateSkippedFileCategorySelection(bool includeSourceExclusions = false)
    {
        return new SkippedFileCategorySelection(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: true,
            IncludeProfileExclusions: false,
            IncludeManualExclusions: true,
            IncludeSourceExclusions: includeSourceExclusions,
            IncludeProcessingFailures: true,
            IncludeOther: false);
    }

    private sealed record TestContext(
        ProfileManagerViewModel ViewModel,
        FakeProfileLibraryService LibraryService,
        FakeUserPromptService UserPromptService,
        FakeOpenFileDialogService OpenFileDialogService);

    private sealed class FakeProfileEditorFactory : IProfileEditorFactory
    {
        public ProfileEditorViewModel Create()
        {
            return new ProfileEditorViewModel(new BuiltInFileTypeCatalog(), new NoOpProfileFilterRulesDialogService());
        }
    }

    private sealed class NoOpProfileFilterRulesDialogService : IProfileFilterRulesDialogService
    {
        public void Show(ProfileEditorViewModel editor)
        {
        }
    }

    private sealed class FakeProfileLibraryService(IReadOnlyCollection<ProfileLibraryEntry> entries)
        : IProfileLibraryService
    {
        private readonly List<ProfileLibraryEntry> _entries = [.. entries];

        public IReadOnlyList<ProfileLibraryEntry> Entries => _entries;

        public int SaveCallCount { get; private set; }

        public int LoadCallCount { get; private set; }

        public int GetAllCallCount { get; private set; }

        public Exception? SaveException { get; set; }

        public ProfileLibraryEntry? LastSavedEntry { get; private set; }

        public bool? LastSaveAsNew { get; private set; }

        public IReadOnlyList<string>? LastImportedFilePaths { get; private set; }

        public IReadOnlyCollection<ProfileLibraryEntry> ImportResult { get; set; } = [];

        public ProfileLibraryEntry? LastExportedEntry { get; private set; }

        public string? LastExportTargetPath { get; private set; }

        public string GetPrimaryProfilesDirectory()
        {
            return @"D:\Profiles";
        }

        public Task<IReadOnlyCollection<ProfileLibraryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;

            IReadOnlyCollection<ProfileLibraryEntry> entries = [.. _entries];
            return Task.FromResult(entries);
        }

        public Task<ProfileLibraryEntry> LoadAsync(string id, CancellationToken cancellationToken = default)
        {
            LoadCallCount++;

            ProfileLibraryEntry entry =
                _entries.Single(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(entry);
        }

        public Task<ProfileLibraryEntry> SaveAsync(
            ProfileLibraryEntry entry,
            bool saveAsNew = false,
            CancellationToken cancellationToken = default)
        {
            SaveCallCount++;

            if (SaveException is not null)
                return Task.FromException<ProfileLibraryEntry>(SaveException);

            LastSavedEntry = entry;
            LastSaveAsNew = saveAsNew;

            int existingIndex = _entries.FindIndex(x => string.Equals(
                x.Id,
                entry.Id,
                StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
                _entries[existingIndex] = entry;
            else
                _entries.Add(entry);

            return Task.FromResult(entry);
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<ProfileLibraryEntry>> ImportAsync(
            IReadOnlyCollection<string> filePaths,
            CancellationToken cancellationToken = default)
        {
            LastImportedFilePaths = [.. filePaths];

            foreach (ProfileLibraryEntry importedEntry in ImportResult)
            {
                int existingIndex = _entries.FindIndex(x => string.Equals(
                    x.Id,
                    importedEntry.Id,
                    StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                    _entries[existingIndex] = importedEntry;
                else
                    _entries.Add(importedEntry);
            }

            return Task.FromResult(ImportResult);
        }

        public Task ExportAsync(
            ProfileLibraryEntry entry,
            string targetFilePath,
            CancellationToken cancellationToken = default)
        {
            LastExportedEntry = entry;
            LastExportTargetPath = targetFilePath;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentSessionProfileHost(
        string currentProfileName,
        string? currentProfileEntryId,
        WorkspaceProfileDto profile) : ICurrentSessionProfileHost
    {
        private WorkspaceProfileDto _profile = profile;

        public int ApplyCallCount { get; private set; }

        public string CurrentProfileName { get; private set; } = currentProfileName;

        public string? CurrentProfileEntryId { get; private set; } = currentProfileEntryId;

        public WorkspaceProfileDto CaptureCurrentProfile()
        {
            return _profile;
        }

        public void ApplyProfileToCurrentSession(
            string profileName,
            WorkspaceProfileDto profile,
            string? profileEntryId)
        {
            ApplyCallCount++;
            CurrentProfileName = profileName;
            CurrentProfileEntryId = profileEntryId;
            _profile = profile;
        }
    }
}