using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Filtering;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Profile.ViewModels;

public sealed class ProfileEditorViewModelTests
{
    [Fact]
    public void Constructor_Should_Select_General_Section_By_Default()
    {
        ProfileEditorViewModel editor = CreateEditor();

        Assert.Equal(ProfileEditorSection.General, editor.SelectedSection);
        Assert.True(editor.IsGeneralSectionSelected);
        Assert.False(editor.IsFormattingSectionSelected);
        Assert.False(editor.IsFilterRulesSectionSelected);
    }

    [Fact]
    public void SelectSectionCommand_Should_Select_Requested_Section()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.SelectSectionCommand.Execute(ProfileEditorSection.FilterRules);

        Assert.Equal(ProfileEditorSection.FilterRules, editor.SelectedSection);
        Assert.True(editor.IsFilterRulesSectionSelected);
        Assert.False(editor.IsGeneralSectionSelected);
    }

    [Fact]
    public void SelectSectionCommand_Should_Not_Select_CSharp_When_Not_Available()
    {
        ProfileEditorViewModel editor = CreateEditor();
        FileTypeOptionViewModel csharpFileType = editor.FileTypes.Single(x => x.Kind == FileKind.CSharp);

        csharpFileType.IsEnabled = false;

        Assert.False(editor.ShowCSharpOptions);
        Assert.False(editor.SelectSectionCommand.CanExecute(ProfileEditorSection.CSharpTransformations));

        editor.SelectSectionCommand.Execute(ProfileEditorSection.CSharpTransformations);

        Assert.Equal(ProfileEditorSection.General, editor.SelectedSection);
    }

    [Fact]
    public void FileTypes_Should_Move_From_CSharp_Section_When_CSharp_Disabled()
    {
        ProfileEditorViewModel editor = CreateEditor();
        FileTypeOptionViewModel csharpFileType = editor.FileTypes.Single(x => x.Kind == FileKind.CSharp);

        csharpFileType.IsEnabled = true;
        editor.SelectSectionCommand.Execute(ProfileEditorSection.CSharpTransformations);

        Assert.Equal(ProfileEditorSection.CSharpTransformations, editor.SelectedSection);

        csharpFileType.IsEnabled = false;

        Assert.Equal(ProfileEditorSection.FileTypes, editor.SelectedSection);
        Assert.True(editor.IsFileTypesSectionSelected);
        Assert.False(editor.IsCSharpTransformationsSectionSelected);
    }

    [Fact]
    public void ApplyProfile_Should_Preserve_FilterRules_In_CaptureProfile()
    {
        ProfileEditorViewModel editor = CreateEditor();

        WorkspaceFileFilterRuleDto rule = ExcludeDirectoryRule("Library");

        editor.ApplyProfile(CreateProfile(filterRules: [rule]));

        WorkspaceProfileDto captured = editor.CaptureProfile();

        WorkspaceFileFilterRuleDto capturedRule = Assert.Single(captured.FilterRules!);

        Assert.Equal(rule.Mode, capturedRule.Mode);
        Assert.Equal(rule.Target, capturedRule.Target);
        Assert.Equal(rule.PatternType, capturedRule.PatternType);
        Assert.Equal(rule.Pattern, capturedRule.Pattern);
        Assert.Equal(rule.IsEnabled, capturedRule.IsEnabled);
        Assert.Equal(rule.Description, capturedRule.Description);
        Assert.Equal(rule.IsUserEditable, capturedRule.IsUserEditable);
    }

    [Fact]
    public void BuildProfile_Should_Use_Profile_FilterRules()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library")
            ]));

        MergeProfile runtimeProfile = editor.BuildProfile();

        Assert.DoesNotContain(runtimeProfile.FilterRules, x =>
            x is { Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact, Pattern: "bin" });

        Assert.Contains(runtimeProfile.FilterRules, x =>
            x is { Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact, Pattern: "Library" });
    }

    [Fact]
    public void BuildPreviewProfileSnapshot_Should_Change_When_FilterRules_Change()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(filterRules: [ExcludeDirectoryRule("Library")]));
        PreviewProfileStateSnapshot first = editor.BuildPreviewProfileSnapshot();

        editor.ApplyProfile(CreateProfile(filterRules: [ExcludeDirectoryRule("Temp")]));
        PreviewProfileStateSnapshot second = editor.BuildPreviewProfileSnapshot();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void UnityProject_Profile_BuiltThroughEditor_Should_Exclude_Library_Files()
    {
        var provider = new BuiltInProfilePresetProvider(new BuiltInFileTypeCatalog());
        ProfileLibraryEntry unityEntry = provider.GetAll().Single(x => x.Id == "builtin.unity-project");

        ProfileEditorViewModel editor = CreateEditor();
        editor.ApplyProfile(unityEntry.Profile);

        MergeProfile runtimeProfile = editor.BuildProfile();

        var service = new FileFilterService();

        var file = new InputFile(
            fullPath: @"D:\Game\Library\PackageCache\package.json",
            relativePath: @"Library\PackageCache\package.json",
            extension: ".json",
            kind: FileKind.Json);

        InputFile result = service.ApplyFilters([file], runtimeProfile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("filter.rule.exclude", result.SkipReason!.Code);
        Assert.Equal("Exclude Unity Library directory", result.SkipReason.Description);
    }

    [Fact]
    public void AddFilterRuleCommand_Should_Add_Default_Rule()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.AddFilterRuleCommand.Execute(null);

        ProfileFilterRuleItemViewModel rule = Assert.Single(editor.FilterRules);

        Assert.Equal(FilterMode.Exclude, rule.Mode);
        Assert.Equal(FilterTarget.DirectorySegment, rule.Target);
        Assert.Equal(RulePatternType.Exact, rule.PatternType);
        Assert.Equal("Library", rule.Pattern);
        Assert.True(rule.IsEnabled);
        Assert.Same(rule, editor.SelectedFilterRule);
    }

    [Fact]
    public void DuplicateFilterRuleCommand_Should_Copy_Selected_Rule()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library")
            ]));

        editor.SelectedFilterRule = editor.FilterRules.Single();

        editor.DuplicateFilterRuleCommand.Execute(null);

        Assert.Equal(2, editor.FilterRules.Count);
        Assert.Equal("Library", editor.FilterRules[0].Pattern);
        Assert.Equal("Library", editor.FilterRules[1].Pattern);
        Assert.Same(editor.FilterRules[1], editor.SelectedFilterRule);
    }

    [Fact]
    public void RemoveFilterRuleCommand_Should_Remove_Selected_Rule()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                ExcludeDirectoryRule("Temp")
            ]));

        editor.SelectedFilterRule = editor.FilterRules[0];

        editor.RemoveFilterRuleCommand.Execute(null);

        Assert.Single(editor.FilterRules);
        Assert.Equal("Temp", editor.FilterRules.Single().Pattern);
    }

    [Fact]
    public void MoveFilterRuleUpCommand_Should_Reorder_Selected_Rule()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                ExcludeDirectoryRule("Temp")
            ]));

        editor.SelectedFilterRule = editor.FilterRules[1];

        editor.MoveFilterRuleUpCommand.Execute(null);

        Assert.Equal("Temp", editor.FilterRules[0].Pattern);
        Assert.Equal("Library", editor.FilterRules[1].Pattern);
    }

    [Fact]
    public void MoveFilterRuleDownCommand_Should_Reorder_Selected_Rule()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                ExcludeDirectoryRule("Temp")
            ]));

        editor.SelectedFilterRule = editor.FilterRules[0];

        editor.MoveFilterRuleDownCommand.Execute(null);

        Assert.Equal("Temp", editor.FilterRules[0].Pattern);
        Assert.Equal("Library", editor.FilterRules[1].Pattern);
    }

    [Fact]
    public void ClearFilterRulesCommand_Should_Remove_Editable_Rules()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                ExcludeDirectoryRule("Temp")
            ]));

        editor.ClearFilterRulesCommand.Execute(null);

        Assert.Empty(editor.FilterRules);
        Assert.False(editor.HasFilterRules);
    }

    [Fact]
    public void CaptureProfile_Should_Include_Edited_FilterRules()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library")
            ]));

        ProfileFilterRuleItemViewModel rule = editor.FilterRules.Single();
        rule.Pattern = "Temp";
        rule.Description = "Exclude Temp";

        WorkspaceProfileDto captured = editor.CaptureProfile();
        WorkspaceFileFilterRuleDto capturedRule = Assert.Single(captured.FilterRules!);

        Assert.Equal("Temp", capturedRule.Pattern);
        Assert.Equal("Exclude Temp", capturedRule.Description);
    }

    [Fact]
    public void BuildProfile_Should_Use_Edited_FilterRules()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library")
            ]));

        editor.FilterRules.Single().Pattern = "Temp";

        MergeProfile runtimeProfile = editor.BuildProfile();

        Assert.Contains(runtimeProfile.FilterRules, x =>
            x is { Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact, Pattern: "Temp" });
    }

    [Fact]
    public void HasInvalidFilterRules_Should_Update_When_Rule_Becomes_Invalid()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library")
            ]));

        Assert.False(editor.HasInvalidFilterRules);
        Assert.True(editor.CanUseProfile);

        editor.FilterRules.Single().Pattern = "";

        Assert.True(editor.HasInvalidFilterRules);
        Assert.False(editor.CanUseProfile);
    }

    [Fact]
    public void CaptureProfile_Should_Include_Unsupported_Text_Fallback_Options()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.IncludeUnsupportedTextFiles = true;
        editor.UnsupportedTextMaxFileSizeBytes = 2048;
        editor.UnsupportedTextProbeSizeBytes = 512;
        editor.UnsupportedTextMaxControlCharacterRatio = 0.20;

        WorkspaceProfileDto captured = editor.CaptureProfile();

        Assert.True(captured.IncludeUnsupportedTextFiles);
        Assert.Equal(2048, captured.UnsupportedTextMaxFileSizeBytes);
        Assert.Equal(512, captured.UnsupportedTextProbeSizeBytes);
        Assert.Equal(0.20, captured.UnsupportedTextMaxControlCharacterRatio);
    }

    [Fact]
    public void ApplyProfile_Should_Load_Unsupported_Text_Fallback_Options()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            includeUnsupportedTextFiles: true,
            unsupportedTextMaxFileSizeBytes: 2048,
            unsupportedTextProbeSizeBytes: 512,
            unsupportedTextMaxControlCharacterRatio: 0.20));

        Assert.True(editor.IncludeUnsupportedTextFiles);
        Assert.Equal(2048, editor.UnsupportedTextMaxFileSizeBytes);
        Assert.Equal(512, editor.UnsupportedTextProbeSizeBytes);
        Assert.Equal(0.20, editor.UnsupportedTextMaxControlCharacterRatio);
    }

    [Fact]
    public void BuildProfile_Should_Use_Unsupported_Text_Fallback_Options()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.IncludeUnsupportedTextFiles = true;
        editor.UnsupportedTextMaxFileSizeBytes = 2048;
        editor.UnsupportedTextProbeSizeBytes = 512;
        editor.UnsupportedTextMaxControlCharacterRatio = 0.20;

        MergeProfile runtimeProfile = editor.BuildProfile();

        UnsupportedTextFallbackOptions options =
            runtimeProfile.GeneralOptions.UnsupportedTextFallbackOptions;

        Assert.True(options.IsEnabled);
        Assert.Equal(2048, options.MaxFileSizeBytes);
        Assert.Equal(512, options.ProbeSizeBytes);
        Assert.Equal(0.20, options.MaxControlCharacterRatio);
    }

    [Fact]
    public void BuildPreviewProfileSnapshot_Should_Change_When_Unsupported_Text_Fallback_Changes()
    {
        ProfileEditorViewModel editor = CreateEditor();

        PreviewProfileStateSnapshot before = editor.BuildPreviewProfileSnapshot();

        editor.IncludeUnsupportedTextFiles = !editor.IncludeUnsupportedTextFiles;

        PreviewProfileStateSnapshot after = editor.BuildPreviewProfileSnapshot();

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void CaptureProfile_Should_Include_Output_Metadata_Options()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.IncludeBuildTimestampMetadata = false;
        editor.IncludeSessionNameMetadata = false;
        editor.IncludeOutputPathMetadata = false;
        editor.IncludeFileSummaryMetadata = false;
        editor.IncludeSourceExcludedFiles = true;
        editor.SkippedFilesMetadataMode = SkippedFilesMetadataMode.Detailed;

        WorkspaceProfileDto captured = editor.CaptureProfile();

        Assert.False(captured.IncludeBuildTimestampMetadata);
        Assert.False(captured.IncludeSessionNameMetadata);
        Assert.False(captured.IncludeOutputPathMetadata);
        Assert.False(captured.IncludeFileSummaryMetadata);
        Assert.True(captured.IncludeSourceExcludedFiles);
        Assert.Equal(SkippedFilesMetadataMode.Detailed, captured.SkippedFilesMetadataMode);
    }

    [Fact]
    public void ApplyProfile_Should_Load_Output_Metadata_Options()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            includeBuildTimestampMetadata: false,
            includeSessionNameMetadata: false,
            includeOutputPathMetadata: false,
            includeFileSummaryMetadata: false,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
            includeSourceExcludedFiles: true));

        Assert.False(editor.IncludeBuildTimestampMetadata);
        Assert.False(editor.IncludeSessionNameMetadata);
        Assert.False(editor.IncludeOutputPathMetadata);
        Assert.False(editor.IncludeFileSummaryMetadata);
        Assert.True(editor.IncludeSourceExcludedFiles);
        Assert.Equal(SkippedFilesMetadataMode.Simple, editor.SkippedFilesMetadataMode);
    }

    [Fact]
    public void BuildProfile_Should_Use_Output_Metadata_Options()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.IncludeBuildTimestampMetadata = false;
        editor.IncludeSessionNameMetadata = false;
        editor.IncludeOutputPathMetadata = false;
        editor.IncludeFileSummaryMetadata = false;
        editor.IncludeSourceExcludedFiles = true;
        editor.SkippedFilesMetadataMode = SkippedFilesMetadataMode.Detailed;

        MergeProfile runtimeProfile = editor.BuildProfile();

        OutputMetadataOptions options = runtimeProfile.GeneralOptions.OutputMetadataOptions;

        Assert.False(options.IncludeBuildTimestamp);
        Assert.False(options.IncludeSessionName);
        Assert.False(options.IncludeOutputPath);
        Assert.False(options.IncludeFileSummary);
        Assert.True(options.IncludeSourceExcludedFiles);
        Assert.Equal(SkippedFilesMetadataMode.Detailed, options.SkippedFilesMetadataMode);
    }

    [Fact]
    public void ApplyAndCaptureProfile_Should_Preserve_Skipped_File_Category_Selection()
    {
        ProfileEditorViewModel editor = CreateEditor();
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();

        editor.ApplyProfile(CreateProfile(
            skippedFileCategories: selection));

        WorkspaceProfileDto captured = editor.CaptureProfile();

        Assert.Equal(selection, captured.SkippedFileCategories);
    }

    [Fact]
    public void BuildProfile_Should_Use_Skipped_File_Category_Selection()
    {
        ProfileEditorViewModel editor = CreateEditor();
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();

        editor.ApplyProfile(CreateProfile(
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
            includeSourceExcludedFiles: true,
            skippedFileCategories: selection));

        OutputMetadataOptions options =
            editor.BuildProfile().GeneralOptions.OutputMetadataOptions;

        Assert.Equal(selection, options.SkippedFileCategories);
        Assert.Equal(selection, options.EffectiveSkippedFileCategories);
        Assert.True(options.IncludeSourceExcludedFiles);
    }

    [Fact]
    public void ApplyAndCaptureProfile_Should_Preserve_Null_Legacy_Skipped_File_Category_Selection()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: true,
            skippedFileCategories: null));

        WorkspaceProfileDto captured = editor.CaptureProfile();

        Assert.Null(captured.SkippedFileCategories);
        Assert.True(captured.IncludeSourceExcludedFiles);
    }

    [Fact]
    public void BuildProfile_Should_Use_Legacy_Source_Exclusion_Fallback_When_Category_Selection_Is_Null()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: true,
            skippedFileCategories: null));

        OutputMetadataOptions options =
            editor.BuildProfile().GeneralOptions.OutputMetadataOptions;

        Assert.Null(options.SkippedFileCategories);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeDisabledFileTypes);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeUnsupportedFiles);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeProfileExclusions);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeManualExclusions);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeSourceExclusions);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeProcessingFailures);
        Assert.True(options.EffectiveSkippedFileCategories.IncludeOther);
    }

    [Fact]
    public void ApplyProfile_Should_Expose_Explicit_Skipped_File_Category_Selection()
    {
        ProfileEditorViewModel editor = CreateEditor();
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();

        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: true,
            skippedFileCategories: selection));

        Assert.True(editor.IncludeDisabledFileTypesInSkippedMetadata);
        Assert.False(editor.IncludeUnsupportedFilesInSkippedMetadata);
        Assert.True(editor.IncludeProfileExclusionsInSkippedMetadata);
        Assert.False(editor.IncludeManualExclusionsInSkippedMetadata);
        Assert.False(editor.IncludeSourceExclusionsInSkippedMetadata);
        Assert.True(editor.IncludeProcessingFailuresInSkippedMetadata);
        Assert.False(editor.IncludeOtherInSkippedMetadata);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyProfile_Should_Expose_Legacy_Effective_Skipped_File_Category_Selection(
        bool includeSourceExcludedFiles)
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: includeSourceExcludedFiles,
            skippedFileCategories: null));

        Assert.True(editor.IncludeDisabledFileTypesInSkippedMetadata);
        Assert.True(editor.IncludeUnsupportedFilesInSkippedMetadata);
        Assert.True(editor.IncludeProfileExclusionsInSkippedMetadata);
        Assert.True(editor.IncludeManualExclusionsInSkippedMetadata);
        Assert.Equal(includeSourceExcludedFiles, editor.IncludeSourceExclusionsInSkippedMetadata);
        Assert.True(editor.IncludeProcessingFailuresInSkippedMetadata);
        Assert.True(editor.IncludeOtherInSkippedMetadata);
    }

    [Fact]
    public void Setting_Skipped_Category_To_Current_Effective_Value_Should_Not_Materialize_Legacy_Selection()
    {
        ProfileEditorViewModel editor = CreateEditor();
        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: false,
            skippedFileCategories: null));

        editor.IncludeUnsupportedFilesInSkippedMetadata = true;

        WorkspaceProfileDto captured = editor.CaptureProfile();

        Assert.Null(captured.SkippedFileCategories);
    }

    [Fact]
    public void Changing_Skipped_Category_Should_Materialize_Explicit_Selection_From_Legacy_Effective_State()
    {
        ProfileEditorViewModel editor = CreateEditor();
        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: true,
            skippedFileCategories: null));

        editor.IncludeUnsupportedFilesInSkippedMetadata = false;

        WorkspaceProfileDto captured = editor.CaptureProfile();
        OutputMetadataOptions runtimeOptions =
            editor.BuildProfile().GeneralOptions.OutputMetadataOptions;

        Assert.NotNull(captured.SkippedFileCategories);
        Assert.True(captured.SkippedFileCategories.IncludeDisabledFileTypes);
        Assert.False(captured.SkippedFileCategories.IncludeUnsupportedFiles);
        Assert.True(captured.SkippedFileCategories.IncludeProfileExclusions);
        Assert.True(captured.SkippedFileCategories.IncludeManualExclusions);
        Assert.True(captured.SkippedFileCategories.IncludeSourceExclusions);
        Assert.True(captured.SkippedFileCategories.IncludeProcessingFailures);
        Assert.True(captured.SkippedFileCategories.IncludeOther);
        Assert.Equal(captured.SkippedFileCategories, runtimeOptions.SkippedFileCategories);
    }

    [Fact]
    public void Explicit_Source_Category_Should_Take_Precedence_Over_Legacy_Source_Option_In_Editor()
    {
        ProfileEditorViewModel editor = CreateEditor();
        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection();

        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: true,
            skippedFileCategories: selection));

        Assert.True(editor.IncludeSourceExcludedFiles);
        Assert.False(editor.IncludeSourceExclusionsInSkippedMetadata);
    }

    [Fact]
    public void Changing_Source_Category_Should_Not_Overwrite_Legacy_Source_Option()
    {
        ProfileEditorViewModel editor = CreateEditor();
        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: false,
            skippedFileCategories: null));

        editor.IncludeSourceExclusionsInSkippedMetadata = true;

        WorkspaceProfileDto captured = editor.CaptureProfile();

        Assert.False(captured.IncludeSourceExcludedFiles);
        Assert.NotNull(captured.SkippedFileCategories);
        Assert.True(captured.SkippedFileCategories.IncludeSourceExclusions);
    }

    [Fact]
    public void BuildPreviewProfileSnapshot_Should_Change_When_Skipped_File_Category_Changes()
    {
        ProfileEditorViewModel editor = CreateEditor();

        PreviewProfileStateSnapshot before = editor.BuildPreviewProfileSnapshot();

        editor.IncludeUnsupportedFilesInSkippedMetadata = false;

        PreviewProfileStateSnapshot after = editor.BuildPreviewProfileSnapshot();

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void BuildPreviewProfileSnapshot_Should_Treat_Legacy_And_Equivalent_Explicit_Category_Selections_As_Equal()
    {
        ProfileEditorViewModel legacyEditor = CreateEditor();
        ProfileEditorViewModel explicitEditor = CreateEditor();
        var equivalentSelection =
            SkippedFileCategorySelection.ForCurrentBehavior(includeSourceExcludedFiles: true);

        legacyEditor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: true,
            skippedFileCategories: null));
        explicitEditor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: true,
            skippedFileCategories: equivalentSelection));

        Assert.Equal(
            legacyEditor.BuildPreviewProfileSnapshot(),
            explicitEditor.BuildPreviewProfileSnapshot());
    }

    [Fact]
    public void BuildPreviewProfileSnapshot_Should_Return_To_Equivalent_State_When_Category_Edit_Is_Reverted()
    {
        ProfileEditorViewModel editor = CreateEditor();
        editor.ApplyProfile(CreateProfile(
            includeSourceExcludedFiles: false,
            skippedFileCategories: null));

        PreviewProfileStateSnapshot before = editor.BuildPreviewProfileSnapshot();

        editor.IncludeUnsupportedFilesInSkippedMetadata = false;
        editor.IncludeUnsupportedFilesInSkippedMetadata = true;

        PreviewProfileStateSnapshot after = editor.BuildPreviewProfileSnapshot();

        Assert.Equal(before, after);
        Assert.NotNull(editor.CaptureProfile().SkippedFileCategories);
    }

    [Fact]
    public void BuildPreviewProfileSnapshot_Should_Change_When_Output_Metadata_Toggle_Changes()
    {
        ProfileEditorViewModel editor = CreateEditor();

        PreviewProfileStateSnapshot before = editor.BuildPreviewProfileSnapshot();

        editor.IncludeBuildTimestampMetadata = !editor.IncludeBuildTimestampMetadata;

        PreviewProfileStateSnapshot after = editor.BuildPreviewProfileSnapshot();

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void BuildPreviewProfileSnapshot_Should_Change_When_Skipped_Files_Metadata_Mode_Changes()
    {
        ProfileEditorViewModel editor = CreateEditor();

        PreviewProfileStateSnapshot before = editor.BuildPreviewProfileSnapshot();

        editor.SkippedFilesMetadataMode = SkippedFilesMetadataMode.Detailed;

        PreviewProfileStateSnapshot after = editor.BuildPreviewProfileSnapshot();

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Constructor_Should_Build_FileTypeGroups()
    {
        ProfileEditorViewModel editor = CreateEditor();

        Assert.NotEmpty(editor.FileTypeGroups);
        Assert.Contains(editor.FileTypeGroups, x => x.Title == "Source code");
        Assert.Contains(editor.FileTypeGroups, x => x.Title == "Data and configuration");
        Assert.Contains(editor.FileTypeGroups, x => x.Title == "Documents and plain text");
    }

    [Fact]
    public void FileTypeGroups_Should_Reuse_FileType_Items()
    {
        ProfileEditorViewModel editor = CreateEditor();

        List<FileTypeOptionViewModel> groupedFileTypes =
        [
            .. editor.FileTypeGroups.SelectMany(x => x.FileTypes)
        ];

        Assert.Equal(editor.FileTypes.Count, groupedFileTypes.Count);

        foreach (FileTypeOptionViewModel fileType in editor.FileTypes)
            Assert.Contains(groupedFileTypes, x => ReferenceEquals(x, fileType));
    }

    [Fact]
    public void FileTypeGroup_Should_Update_Summary_When_FileType_Toggled()
    {
        ProfileEditorViewModel editor = CreateEditor();

        ProfileFileTypeGroupViewModel sourceCodeGroup =
            editor.FileTypeGroups.Single(x => x.FileTypes.Any(y => y.Extension == ".cs"));

        FileTypeOptionViewModel csharpFileType =
            sourceCodeGroup.FileTypes.Single(x => x.Extension == ".cs");

        int initialEnabledCount = sourceCodeGroup.EnabledCount;

        csharpFileType.IsEnabled = !csharpFileType.IsEnabled;

        Assert.Equal(initialEnabledCount - 1, sourceCodeGroup.EnabledCount);
        Assert.Equal(
            $"{sourceCodeGroup.EnabledCount}/{sourceCodeGroup.TotalCount} enabled",
            sourceCodeGroup.SummaryLabel);
    }

    [Fact]
    public void FileTypeGroup_DisableAllCommand_Should_Update_Profile_FileTypes()
    {
        ProfileEditorViewModel editor = CreateEditor();

        ProfileFileTypeGroupViewModel sourceCodeGroup =
            editor.FileTypeGroups.Single(x => x.FileTypes.Any(y => y.Extension == ".cs"));

        sourceCodeGroup.DisableAllCommand.Execute(null);

        Assert.All(sourceCodeGroup.FileTypes, x => Assert.False(x.IsEnabled));

        MergeProfile runtimeProfile = editor.BuildProfile();

        foreach (FileTypeOptionViewModel fileType in sourceCodeGroup.FileTypes)
        {
            Assert.Contains(runtimeProfile.FileTypes, x =>
                string.Equals(x.Extension, fileType.Extension, StringComparison.OrdinalIgnoreCase) &&
                !x.IsEnabled);
        }
    }

    [Fact]
    public void ApplyProfile_Should_Rebuild_FileTypeGroups()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile());

        ProfileFileTypeGroupViewModel sourceCodeGroup =
            editor.FileTypeGroups.Single(x => x.Title == "Source code");

        ProfileFileTypeGroupViewModel dataGroup =
            editor.FileTypeGroups.Single(x => x.Title == "Data and configuration");

        ProfileFileTypeGroupViewModel dotNetGroup =
            editor.FileTypeGroups.Single(x => x.Title == ".NET / WPF / MSBuild");

        Assert.Contains(sourceCodeGroup.FileTypes, x => x.Extension == ".cs");
        Assert.Contains(dataGroup.FileTypes, x => x.Extension == ".json");
        Assert.Contains(dotNetGroup.FileTypes, x => x.Extension == ".xaml");
    }

    [Fact]
    public void ApplyProfile_Should_Place_Custom_FileTypes_In_Other_Group()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            fileTypes:
            [
                new WorkspaceFileTypeDto(
                    Extension: ".cs",
                    DisplayName: "C# source",
                    Kind: FileKind.CSharp,
                    IsEnabled: true,
                    SupportsLanguageSpecificProcessing: true),

                new WorkspaceFileTypeDto(
                    Extension: ".custom",
                    DisplayName: "Custom file",
                    Kind: FileKind.Text,
                    IsEnabled: true,
                    SupportsLanguageSpecificProcessing: false)
            ]));

        ProfileFileTypeGroupViewModel otherGroup =
            editor.FileTypeGroups.Single(x => x.Title == "Other / custom");

        Assert.Contains(otherGroup.FileTypes, x =>
            x is { Extension: ".custom", DisplayName: "Custom file", IsEnabled: true });
    }

    [Fact]
    public void FileTypeFilters_Should_Filter_By_Search_Text()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.FileTypeFilters.SearchText = ".json";

        List<FileTypeOptionViewModel> visibleFileTypes =
        [
            .. editor.FileTypeGroups.SelectMany(x => x.VisibleFileTypes)
        ];

        Assert.NotEmpty(visibleFileTypes);
        Assert.All(visibleFileTypes, x =>
            Assert.Contains(".json", x.Extension, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            $"{visibleFileTypes.Count} of {editor.FileTypes.Count} file types shown",
            editor.FileTypeFilterSummary);
    }

    [Fact]
    public void FileTypeFilters_Should_Filter_By_Enabled_Status()
    {
        ProfileEditorViewModel editor = CreateEditor();
        FileTypeOptionViewModel csharpFileType = editor.FileTypes.Single(x => x.Extension == ".cs");

        csharpFileType.IsEnabled = false;
        editor.FileTypeFilters.Mode = ProfileFileTypeFilterMode.Disabled;

        List<FileTypeOptionViewModel> visibleFileTypes =
        [
            .. editor.FileTypeGroups.SelectMany(x => x.VisibleFileTypes)
        ];

        Assert.Contains(visibleFileTypes, x => x.Extension == ".cs");
        Assert.All(visibleFileTypes, x => Assert.False(x.IsEnabled));
    }

    [Fact]
    public void FileTypeFilters_ClearCommand_Should_Reset_Filters()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.FileTypeFilters.SearchText = ".json";
        editor.FileTypeFilters.Mode = ProfileFileTypeFilterMode.Enabled;

        Assert.True(editor.FileTypeFilters.HasActiveFilters);

        editor.ClearFileTypeFiltersCommand.Execute(null);

        Assert.Equal(string.Empty, editor.FileTypeFilters.SearchText);
        Assert.Equal(ProfileFileTypeFilterMode.All, editor.FileTypeFilters.Mode);
        Assert.Equal(
            editor.FileTypes.Count,
            editor.FileTypeGroups.Sum(x => x.VisibleFileTypes.Count));
    }

    [Fact]
    public void FileTypeFilters_Should_Not_Change_Captured_Profile()
    {
        ProfileEditorViewModel editor = CreateEditor();
        int totalFileTypes = editor.FileTypes.Count;

        editor.FileTypeFilters.SearchText = ".json";
        editor.FileTypeFilters.Mode = ProfileFileTypeFilterMode.Enabled;

        WorkspaceProfileDto captured = editor.CaptureProfile();

        Assert.Equal(totalFileTypes, captured.FileTypes.Count);
    }

    [Fact]
    public void FileTypeGroup_EnableDisableCommands_Should_Affect_Visible_FileTypes()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.FileTypeFilters.SearchText = ".json";

        ProfileFileTypeGroupViewModel group =
            editor.FileTypeGroups.Single(x => x.VisibleFileTypes.Any(y => y.Extension == ".json"));

        group.DisableAllCommand.Execute(null);

        Assert.All(group.VisibleFileTypes, x => Assert.False(x.IsEnabled));
        Assert.Contains(group.FileTypes, x => x is { Extension: ".json", IsEnabled: false });
    }

    [Fact]
    public void FilterRuleFilters_Should_Filter_By_Search_Text()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                ExcludeDirectoryRule("Temp")
            ]));

        editor.FilterRuleFilters.SearchText = "Library";

        ProfileFilterRuleItemViewModel rule = Assert.Single(editor.VisibleFilterRules);

        Assert.Equal("Library", rule.Pattern);
        Assert.Equal(2, editor.FilterRules.Count);
    }

    [Fact]
    public void FilterRuleFilters_Should_Filter_By_Status()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library", isEnabled: true),
                ExcludeDirectoryRule("Temp", isEnabled: false)
            ]));

        editor.FilterRuleFilters.Status = ProfileFilterRuleStatusFilterMode.Disabled;

        ProfileFilterRuleItemViewModel rule = Assert.Single(editor.VisibleFilterRules);

        Assert.Equal("Temp", rule.Pattern);
        Assert.False(rule.IsEnabled);
    }

    [Fact]
    public void FilterRuleFilters_Should_Show_Invalid_Rules()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                new WorkspaceFileFilterRuleDto(
                    Mode: FilterMode.Exclude,
                    Target: FilterTarget.DirectorySegment,
                    PatternType: RulePatternType.Exact,
                    Pattern: string.Empty,
                    IsEnabled: true,
                    Description: "Invalid empty rule",
                    IsUserEditable: true)
            ]));

        editor.FilterRuleFilters.Status = ProfileFilterRuleStatusFilterMode.Invalid;

        ProfileFilterRuleItemViewModel rule = Assert.Single(editor.VisibleFilterRules);

        Assert.True(rule.HasValidationError);
        Assert.Equal("Pattern cannot be empty.", rule.ValidationMessage);
    }

    [Fact]
    public void FilterRuleFilters_Should_Reset_Selected_Rule_When_Hidden()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                ExcludeDirectoryRule("Temp")
            ]));

        ProfileFilterRuleItemViewModel libraryRule = editor.FilterRules.Single(x => x.Pattern == "Library");
        editor.SelectedFilterRule = libraryRule;

        editor.FilterRuleFilters.SearchText = "Temp";

        Assert.NotSame(libraryRule, editor.SelectedFilterRule);
        Assert.Equal("Temp", editor.SelectedFilterRule?.Pattern);
    }

    [Fact]
    public void FilterRuleFilters_ClearCommand_Should_Reset_Filters()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                ExcludeDirectoryRule("Temp")
            ]));

        editor.FilterRuleFilters.SearchText = "Library";
        editor.FilterRuleFilters.Status = ProfileFilterRuleStatusFilterMode.Enabled;

        Assert.True(editor.FilterRuleFilters.HasActiveFilters);

        editor.ClearFilterRuleFiltersCommand.Execute(null);

        Assert.Equal(string.Empty, editor.FilterRuleFilters.SearchText);
        Assert.Equal(ProfileFilterRuleStatusFilterMode.All, editor.FilterRuleFilters.Status);
        Assert.Equal(editor.FilterRules.Count, editor.VisibleFilterRules.Count);
    }

    [Fact]
    public void FilterRuleFilters_Should_Not_Change_Built_Profile()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.ApplyProfile(CreateProfile(
            filterRules:
            [
                ExcludeDirectoryRule("Library"),
                ExcludeDirectoryRule("Temp")
            ]));

        editor.FilterRuleFilters.SearchText = "Library";

        MergeProfile runtimeProfile = editor.BuildProfile();

        Assert.Equal(2, runtimeProfile.FilterRules.Count);
        Assert.Contains(runtimeProfile.FilterRules, x => x.Pattern == "Library");
        Assert.Contains(runtimeProfile.FilterRules, x => x.Pattern == "Temp");
    }

    [Fact]
    public void ApplyProfile_Should_Reset_Search_And_Filters()
    {
        ProfileEditorViewModel editor = CreateEditor();

        editor.FileTypeFilters.SearchText = ".json";
        editor.FileTypeFilters.Mode = ProfileFileTypeFilterMode.Enabled;
        editor.FilterRuleFilters.SearchText = "Library";
        editor.FilterRuleFilters.Status = ProfileFilterRuleStatusFilterMode.Disabled;

        editor.ApplyProfile(CreateProfile());

        Assert.Equal(string.Empty, editor.FileTypeFilters.SearchText);
        Assert.Equal(ProfileFileTypeFilterMode.All, editor.FileTypeFilters.Mode);
        Assert.Equal(string.Empty, editor.FilterRuleFilters.SearchText);
        Assert.Equal(ProfileFilterRuleStatusFilterMode.All, editor.FilterRuleFilters.Status);
    }

    [Fact]
    public void BuildPreviewProfileSnapshot_Should_Change_When_Source_Excluded_Files_Option_Changes()
    {
        ProfileEditorViewModel editor = CreateEditor();

        PreviewProfileStateSnapshot before = editor.BuildPreviewProfileSnapshot();

        editor.IncludeSourceExcludedFiles = true;

        PreviewProfileStateSnapshot after = editor.BuildPreviewProfileSnapshot();

        Assert.NotEqual(before, after);
    }

    private static ProfileEditorViewModel CreateEditor()
    {
        return new ProfileEditorViewModel(new BuiltInFileTypeCatalog());
    }

    private static WorkspaceProfileDto CreateProfile(
        List<WorkspaceFileTypeDto>? fileTypes = null,
        List<WorkspaceFileFilterRuleDto>? filterRules = null,
        bool includeUnsupportedTextFiles = false,
        long unsupportedTextMaxFileSizeBytes = UnsupportedTextFallbackOptions.DefaultMaxFileSizeBytes,
        int unsupportedTextProbeSizeBytes = UnsupportedTextFallbackOptions.DefaultProbeSizeBytes,
        double unsupportedTextMaxControlCharacterRatio = UnsupportedTextFallbackOptions.DefaultMaxControlCharacterRatio,
        bool includeBuildTimestampMetadata = true,
        bool includeSessionNameMetadata = true,
        bool includeOutputPathMetadata = true,
        bool includeFileSummaryMetadata = true,
        SkippedFilesMetadataMode skippedFilesMetadataMode = SkippedFilesMetadataMode.None,
        bool includeSourceExcludedFiles = false,
        SkippedFileCategorySelection? skippedFileCategories = null)
    {
        fileTypes ??=
        [
            new WorkspaceFileTypeDto(
                Extension: ".cs",
                DisplayName: "C# source",
                Kind: FileKind.CSharp,
                IsEnabled: true,
                SupportsLanguageSpecificProcessing: true),

            new WorkspaceFileTypeDto(
                Extension: ".json",
                DisplayName: "JSON",
                Kind: FileKind.Json,
                IsEnabled: true,
                SupportsLanguageSpecificProcessing: false)
        ];

        return new WorkspaceProfileDto(
            IncludeHeaderComment: false,
            IncludeFileSeparators: true,
            IncludeRelativePathInSeparator: true,
            TrimTrailingEmptyLines: true,
            RemoveUsingDirectives: false,
            FileTypes: fileTypes,
            LineEndingMode: LineEndingMode.Preserve,
            SortMode: SortMode.ByRelativePathAscending,
            InputEncodingMode: InputEncodingMode.Auto,
            PreferredInputEncodingName: null,
            FallbackInputEncodingName: "windows-1251",
            FilterRules: filterRules,
            IncludeUnsupportedTextFiles: includeUnsupportedTextFiles,
            UnsupportedTextMaxFileSizeBytes: unsupportedTextMaxFileSizeBytes,
            UnsupportedTextProbeSizeBytes: unsupportedTextProbeSizeBytes,
            UnsupportedTextMaxControlCharacterRatio: unsupportedTextMaxControlCharacterRatio,
            IncludeBuildTimestampMetadata: includeBuildTimestampMetadata,
            IncludeSessionNameMetadata: includeSessionNameMetadata,
            IncludeOutputPathMetadata: includeOutputPathMetadata,
            IncludeFileSummaryMetadata: includeFileSummaryMetadata,
            SkippedFilesMetadataMode: skippedFilesMetadataMode,
            IncludeSourceExcludedFiles: includeSourceExcludedFiles,
            SkippedFileCategories: skippedFileCategories);
    }

    private static SkippedFileCategorySelection CreateSkippedFileCategorySelection()
    {
        return new SkippedFileCategorySelection(
            IncludeDisabledFileTypes: true,
            IncludeUnsupportedFiles: false,
            IncludeProfileExclusions: true,
            IncludeManualExclusions: false,
            IncludeSourceExclusions: false,
            IncludeProcessingFailures: true,
            IncludeOther: false);
    }

    private static WorkspaceFileFilterRuleDto ExcludeDirectoryRule(
        string pattern,
        bool isEnabled = true)
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: pattern,
            IsEnabled: isEnabled,
            Description: $"Exclude {pattern}",
            IsUserEditable: true);
    }
}