using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;
using FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;

namespace FileMerger.Tests.Wpf.Features.Workspace.Configuration;

public sealed class WorkspaceLocalProfileDialogViewModelTests
{
    [Fact]
    public void Constructor_Should_Create_Detached_Profile_Editor_Copy()
    {
        WorkspaceDocumentViewModel source = WorkspaceDocumentTestFactory.CreateDocument();
        source.ProfileEditor.WorkingProfileName = "Source Profile";
        source.ProfileEditor.IncludeHeaderComment = true;
        WorkspaceProfileDto initialProfile = source.ProfileEditor.CaptureProfile();
        bool initialFirstFileTypeEnabled = source.ProfileEditor.FileTypes[0].IsEnabled;

        WorkspaceLocalProfileDialogViewModel viewModel = CreateViewModel("Source Profile", initialProfile);

        viewModel.Editor.WorkingProfileName = "Edited Profile";
        viewModel.Editor.IncludeHeaderComment = false;
        viewModel.Editor.FileTypes[0].IsEnabled = !viewModel.Editor.FileTypes[0].IsEnabled;

        Assert.Equal("Source Profile", source.ProfileEditor.WorkingProfileName);
        Assert.True(source.ProfileEditor.IncludeHeaderComment);
        Assert.Equal(initialFirstFileTypeEnabled, source.ProfileEditor.FileTypes[0].IsEnabled);
        Assert.Equal(initialProfile.FileTypes.Count, source.ProfileEditor.FileTypes.Count);
        Assert.Equal(initialProfile.FilterRules?.Count ?? 0, source.ProfileEditor.FilterRules.Count);
    }

    [Fact]
    public void Cancel_Should_Close_Without_Result()
    {
        WorkspaceDocumentViewModel source = WorkspaceDocumentTestFactory.CreateDocument();
        WorkspaceLocalProfileDialogViewModel viewModel = CreateViewModel(
            "Source Profile",
            source.ProfileEditor.CaptureProfile());
        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.Editor.WorkingProfileName = "Discarded Profile";
        viewModel.Editor.IncludeHeaderComment = !viewModel.Editor.IncludeHeaderComment;
        viewModel.CancelCommand.Execute(null);

        Assert.Equal(false, closeResult);
        Assert.Null(viewModel.Result);
    }

    [Fact]
    public void Ok_Should_Return_Complete_Confirmed_Profile()
    {
        WorkspaceDocumentViewModel source = WorkspaceDocumentTestFactory.CreateDocument();
        WorkspaceLocalProfileDialogViewModel viewModel = CreateViewModel(
            "Source Profile",
            source.ProfileEditor.CaptureProfile());
        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.Editor.WorkingProfileName = "Confirmed Profile";
        viewModel.Editor.IncludeHeaderComment = true;
        viewModel.Editor.TrimTrailingEmptyLines = false;
        viewModel.Editor.IncludeUnsupportedTextFiles = true;
        viewModel.Editor.IncludeBuildTimestampMetadata = false;

        viewModel.OkCommand.Execute(null);

        Assert.Equal(true, closeResult);
        WorkspaceLocalProfileEditResult result = Assert.IsType<WorkspaceLocalProfileEditResult>(viewModel.Result);
        Assert.Equal("Confirmed Profile", result.ProfileName);
        Assert.True(result.Profile.IncludeHeaderComment);
        Assert.False(result.Profile.TrimTrailingEmptyLines);
        Assert.True(result.Profile.IncludeUnsupportedTextFiles);
        Assert.False(result.Profile.IncludeBuildTimestampMetadata);
    }

    [Fact]
    public void OkCommand_Should_Be_Disabled_Until_Invalid_Filter_Rule_Is_Corrected()
    {
        WorkspaceDocumentViewModel source = WorkspaceDocumentTestFactory.CreateDocument();
        WorkspaceProfileDto invalidProfile = source.ProfileEditor.CaptureProfile() with
        {
            FilterRules =
            [
                new WorkspaceFileFilterRuleDto(
                    Mode: FilterMode.Exclude,
                    Target: FilterTarget.DirectorySegment,
                    PatternType: RulePatternType.Exact,
                    Pattern: string.Empty,
                    IsEnabled: true,
                    Description: "Invalid rule",
                    IsUserEditable: true)
            ]
        };
        WorkspaceLocalProfileDialogViewModel viewModel = CreateViewModel("Invalid Profile", invalidProfile);
        ProfileFilterRuleItemViewModel rule = Assert.Single(viewModel.Editor.FilterRules);
        List<bool> canExecuteStates = [];
        viewModel.OkCommand.CanExecuteChanged += (_, _) => canExecuteStates.Add(viewModel.OkCommand.CanExecute(null));

        Assert.False(viewModel.OkCommand.CanExecute(null));
        Assert.Equal("Filter Rules, rule 1: Pattern cannot be empty.", viewModel.Editor.DraftValidationMessage);

        rule.Pattern = "obj";

        Assert.True(viewModel.OkCommand.CanExecute(null));
        Assert.Contains(true, canExecuteStates);
    }

    private static WorkspaceLocalProfileDialogViewModel CreateViewModel(string profileName, WorkspaceProfileDto profile)
    {
        IProfileEditorFactory factory = new ProfileEditorFactory(
            new BuiltInFileTypeCatalog(),
            new NoOpProfileFilterRulesDialogService());

        return new WorkspaceLocalProfileDialogViewModel(profileName, profile, factory);
    }

    private sealed class NoOpProfileFilterRulesDialogService : IProfileFilterRulesDialogService
    {
        public void Show(ProfileEditorViewModel editor)
        {
        }
    }
}