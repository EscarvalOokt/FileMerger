using System.Text.RegularExpressions;
using System.Windows.Threading;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.UseCases.BuildPreview;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Filtering;
using FileMerger.Infrastructure.Services.Merge;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Files.ViewModels;
using FileMerger.Wpf.Features.Preview;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Preview.ViewModels;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Features.Validation.ViewModels;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentPreviewServiceTests
{
    private const int CustomDisplayLimit = 10_000;
    private const string RegularFilePath = @"D:\Project\Program.cs";
    private const string GeneratedFilePath = @"D:\Project\bin\generated.cs";
    private const string GeneratedRelativePath = "bin/generated.cs";
    private const string GeneratedContent = "// generated source marker must not enter the output";

    [Fact]
    public async Task BuildPreviewAsync_Should_Use_PreviewDisplayCharacterLimit_From_Preferences()
    {
        string fullContent = new('x', CustomDisplayLimit + 2_000);

        WorkspaceDocumentViewModel document = await BuildPreviewAsync(fullContent);

        Assert.True(document.PreviewContent.Length < fullContent.Length);
        Assert.StartsWith(fullContent[..CustomDisplayLimit], document.PreviewContent);
        Assert.Contains("Save still writes the full output.", document.PreviewContent);
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Keep_LastOutput_Content_Full_When_Preview_Is_Truncated()
    {
        string fullContent = new('x', CustomDisplayLimit + 2_000);

        WorkspaceDocumentViewModel document = await BuildPreviewAsync(fullContent);

        Assert.NotNull(document.LastOutput);
        Assert.Equal(fullContent, document.LastOutput.Content);
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Report_Truncation_In_Summary_With_Custom_Limit()
    {
        string fullContent = new('x', CustomDisplayLimit + 2_000);

        WorkspaceDocumentViewModel document = await BuildPreviewAsync(fullContent);

        Assert.Equal(fullContent.Length, document.PreviewSummary.TotalCharacters);
        Assert.Equal(CustomDisplayLimit, document.PreviewSummary.DisplayedCharacters);
        Assert.Equal(fullContent.Length - CustomDisplayLimit, document.PreviewSummary.OmittedCharacters);
        Assert.True(document.PreviewSummary.WasTruncated);
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Build_Summary_From_Complete_Discovery_State()
    {
        await RunOnStaThreadAsync(async () =>
        {
            InputFile included = new(
                fullPath: @"D:\Project\Included.cs",
                relativePath: "Included.cs",
                extension: ".cs",
                kind: FileKind.CSharp);

            InputFile disabled = new(
                fullPath: @"D:\Project\Disabled.json",
                relativePath: "Disabled.json",
                extension: ".json",
                kind: FileKind.Json,
                isIncluded: false,
                skipReason: new SkipReason("discovery.file-type-disabled", "File type is disabled."),
                isMergeCandidate: false);

            InputFile filtered = new(
                fullPath: @"D:\Project\Filtered.cs",
                relativePath: "Filtered.cs",
                extension: ".cs",
                kind: FileKind.CSharp,
                isIncluded: false,
                skipReason: new SkipReason("filter.rule.exclude", "Excluded by profile filter."));

            InputFile manuallyExcluded = new(
                fullPath: @"D:\Project\Manual.cs",
                relativePath: "Manual.cs",
                extension: ".cs",
                kind: FileKind.CSharp);

            InputFile sourceExcluded = new(
                fullPath: @"D:\Project\Generated.cs",
                relativePath: "Generated.cs",
                extension: ".cs",
                kind: FileKind.Unknown,
                isIncluded: false,
                skipReason: new SkipReason("source.exclude", "Excluded by a source-specific exclusion."),
                isMergeCandidate: false);

            FileDiscoveryResult discoveryResult = new(
                [included, disabled, filtered, manuallyExcluded],
                [sourceExcluded]);

            InputFile[] filteredFiles =
            [
                included,
                filtered,
                manuallyExcluded
            ];

            InputFile[] currentFiles =
            [
                included,
                filtered.Include(),
                manuallyExcluded.Exclude(new SkipReason("manual.exclude", "Excluded manually by user."))
            ];

            FakeApplicationPreferencesStore preferencesStore = new();
            BuildMergePreviewUseCase useCase = new(
                new PassingMergeSessionValidator(),
                new FixedFileDiscoveryService(discoveryResult),
                new FixedFileFilterService(filteredFiles),
                new FixedFileInclusionOverrideService(currentFiles),
                new SuccessfulContentReader(),
                new PassThroughContentTransformationService(),
                new FixedOutputMergeBuilder("merged"));

            MainStateFactory mainStateFactory = new();
            WorkspaceDocumentPreviewService service = new(
                useCase,
                mainStateFactory,
                new WorkspaceDocumentDirtyStateService(mainStateFactory),
                preferencesStore);

            WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDocument();

            await service.BuildPreviewAsync(document);

            Assert.True(document.HasPreviewSummary);
            Assert.Equal(4, document.PreviewSummary.FilesDiscovered);
            Assert.Equal(2, document.PreviewSummary.FilesIncluded);
            Assert.Equal(2, document.PreviewSummary.FilesNotIncluded);
            Assert.Equal(1, document.PreviewSummary.DisabledFileTypeFiles);
            Assert.Equal(0, document.PreviewSummary.ProfileExcludedFiles);
            Assert.Equal(1, document.PreviewSummary.ManuallyExcludedFiles);
            Assert.Equal(1, document.PreviewSummary.SourceExcludedFiles);

            return true;
        });
    }

    [Theory]
    [InlineData(RulePatternType.Exact, "")]
    [InlineData(RulePatternType.Regex, "[")]
    public async Task BuildPreviewAsync_Should_Reject_Invalid_Profile_Before_Session_And_Pipeline(
        RulePatternType patternType,
        string pattern)
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDocumentViewModel document = CreatePreviewDocument();
            ProfileFilterRuleItemViewModel rule = Assert.Single(document.ProfileEditor.FilterRules);
            rule.PatternType = patternType;
            rule.Pattern = pattern;

            document.ValidationPane.Load(
            [
                new ValidationIssue(ValidationSeverity.Warning, "test.previous", "Previous validation result.")
            ]);
            WorkspaceDirtyStateTracker unchangedState = CreateStateWitness(document);
            string previousCharacterCount = document.PreviewCharacterCountText;
            List<(bool IsBusy, bool HasErrors)> callbackStates = [];

            for (int attempt = 0; attempt < 2; attempt++)
            {
                callbackStates.Clear();
                await context.Service.BuildPreviewAsync(
                    document,
                    stateChanged: () => callbackStates.Add(
                        (document.OperationStatus.IsBusy, document.ValidationPane.HasErrors)));

                Assert.Equal(new PipelineCallCounts(0, 0, 0, 0, 0, 0, 0, 0), context.CaptureCallCounts());
                AssertProfileValidationFailure(document);
                Assert.Contains((true, true), callbackStates);
                Assert.Equal((false, true), callbackStates.Last());
                AssertBuildStopped(document);
                AssertStateUnchanged(unchangedState, document);
                Assert.Same(rule, Assert.Single(document.ProfileEditor.FilterRules));
                Assert.Equal(pattern, rule.Pattern);
                Assert.Empty(document.FilesPane.Files);
                Assert.Null(document.LastOutput);
                Assert.Empty(document.PreviewContent);
                Assert.Equal(previousCharacterCount, document.PreviewCharacterCountText);
                Assert.False(document.HasPreviewSummary);
                Assert.False(document.HasPreviewNotice);
                Assert.False(document.AppliedPreviewFileStateStore.HasAppliedState);
                Assert.False(document.PreviewDirtyTracker.HasAppliedPreview);
                Assert.True(document.PreviewDirtyTracker.IsPreviewDirty);
                Assert.Equal(PreviewDirtyReason.NeverBuilt, document.PreviewDirtyTracker.PreviewDirtyReason);
            }

            document.CancelBuildPreview();
            return true;
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BuildPreviewAsync_Should_Reject_Empty_Output_Path_Before_Session_And_Pipeline(string outputPath)
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDocumentViewModel document = CreatePreviewDocument();
            document.SessionSettings.OutputPath = outputPath;

            document.ValidationPane.Load(
            [
                new ValidationIssue(ValidationSeverity.Warning, "test.previous", "Previous validation result.")
            ]);
            WorkspaceDirtyStateTracker unchangedState = CreateStateWitness(document);
            string previousCharacterCount = document.PreviewCharacterCountText;
            List<(bool IsBusy, bool HasErrors)> callbackStates = [];

            await context.Service.BuildPreviewAsync(
                document,
                stateChanged: () => callbackStates.Add(
                    (document.OperationStatus.IsBusy, document.ValidationPane.HasErrors)));

            Assert.Equal(new PipelineCallCounts(0, 0, 0, 0, 0, 0, 0, 0), context.CaptureCallCounts());
            AssertOutputPathValidationFailure(document);
            Assert.Contains((true, true), callbackStates);
            Assert.Equal((false, true), callbackStates.Last());
            AssertBuildStopped(document);
            AssertStateUnchanged(unchangedState, document);
            Assert.Empty(document.FilesPane.Files);
            Assert.Null(document.LastOutput);
            Assert.Empty(document.PreviewContent);
            Assert.Equal(previousCharacterCount, document.PreviewCharacterCountText);
            Assert.False(document.HasPreviewSummary);
            Assert.False(document.HasPreviewNotice);
            Assert.False(document.AppliedPreviewFileStateStore.HasAppliedState);
            Assert.False(document.PreviewDirtyTracker.HasAppliedPreview);
            Assert.True(document.PreviewDirtyTracker.IsPreviewDirty);
            Assert.Equal(PreviewDirtyReason.NeverBuilt, document.PreviewDirtyTracker.PreviewDirtyReason);

            document.CancelBuildPreview();
            return true;
        });
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Build_After_Output_Path_Is_Corrected()
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDocumentViewModel document = CreatePreviewDocument();
            document.SessionSettings.OutputPath = string.Empty;

            await context.Service.BuildPreviewAsync(document);

            Assert.Equal(new PipelineCallCounts(0, 0, 0, 0, 0, 0, 0, 0), context.CaptureCallCounts());
            AssertOutputPathValidationFailure(document);
            AssertBuildStopped(document);

            document.SessionSettings.OutputPath = WorkspaceDocumentTestFactory.DefaultOutputPath;

            await context.Service.BuildPreviewAsync(document);

            Assert.Equal(new PipelineCallCounts(1, 1, 1, 1, 1, 1, 1, 1), context.CaptureCallCounts());
            AssertSuccessfulPreview(document);
            Assert.Empty(document.ValidationPane.Issues);
            Assert.NotNull(document.LastOutput);
            Assert.False(document.HasPreviewNotice);
            Assert.True(document.PreviewDirtyTracker.HasAppliedPreview);
            Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);
            Assert.Equal(PreviewDirtyReason.None, document.PreviewDirtyTracker.PreviewDirtyReason);
            AssertBuildStopped(document);

            return true;
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BuildPreviewAsync_Should_Preserve_Last_Output_Files_And_Dirty_Baselines_When_Profile_Is_Invalid(
        bool workspaceDirty,
        bool hasPendingOverride)
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDocumentViewModel document = CreatePreviewDocument();

            await context.Service.BuildPreviewAsync(document);
            AssertSuccessfulPreview(document);
            context.DirtyStateService.MarkWorkspaceSaved(document);

            ProfileFilterRuleItemViewModel rule = Assert.Single(document.ProfileEditor.FilterRules);
            InputFileItemViewModel regularFile = document.FilesPane.Files.Single(x => x.FullPath == RegularFilePath);
            document.FilesPane.ReplaceSelectedFiles([regularFile]);
            document.FilesPane.ContextFile = regularFile;

            if (hasPendingOverride)
                regularFile.IsIncluded = false;

            rule.Pattern = string.Empty;
            context.DirtyStateService.RefreshWorkspaceDirtyState(document);

            if (!workspaceDirty)
                context.DirtyStateService.MarkWorkspaceSaved(document);

            Assert.Equal(workspaceDirty, document.IsWorkspaceDirty);
            Assert.Equal(hasPendingOverride, regularFile.HasPendingPreviewState);
            Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);

            MergeOutput? previousOutput = document.LastOutput;
            string previousContent = document.PreviewContent;
            string previousCharacterCount = document.PreviewCharacterCountText;
            PreviewGenerationSummaryViewModel previousSummary = document.PreviewSummary;
            IReadOnlyDictionary<string, bool> previousAppliedState = document.AppliedPreviewFileStateStore.Current;
            InputFileItemViewModel[] previousFiles = [.. document.FilesPane.Files];
            bool[] previousInclusion = [.. previousFiles.Select(x => x.IsIncluded)];
            bool[] previousPendingState = [.. previousFiles.Select(x => x.HasPendingPreviewState)];
            WorkspaceDirtyStateTracker unchangedState = CreateStateWitness(document);
            PipelineCallCounts previousCalls = context.CaptureCallCounts();

            await context.Service.BuildPreviewAsync(document);

            Assert.Equal(previousCalls, context.CaptureCallCounts());
            AssertProfileValidationFailure(document);
            AssertBuildStopped(document);
            Assert.Same(previousOutput, document.LastOutput);
            Assert.Equal(previousContent, document.PreviewContent);
            Assert.Equal(previousCharacterCount, document.PreviewCharacterCountText);
            Assert.Same(previousSummary, document.PreviewSummary);
            Assert.Same(previousAppliedState, document.AppliedPreviewFileStateStore.Current);
            Assert.Equal(previousFiles, document.FilesPane.Files.ToArray());
            Assert.Equal(previousInclusion, document.FilesPane.Files.Select(x => x.IsIncluded).ToArray());
            Assert.Equal(
                previousPendingState,
                document.FilesPane.Files.Select(x => x.HasPendingPreviewState).ToArray());
            Assert.Same(regularFile, Assert.Single(document.FilesPane.SelectedFiles));
            Assert.Same(regularFile, document.FilesPane.ContextFile);
            AssertStateUnchanged(unchangedState, document);
            Assert.Equal(workspaceDirty, document.IsWorkspaceDirty);
            Assert.Same(rule, Assert.Single(document.ProfileEditor.FilterRules));
            Assert.Equal(string.Empty, rule.Pattern);
            Assert.True(document.PreviewDirtyTracker.HasAppliedPreview);
            Assert.True(document.PreviewDirtyTracker.IsPreviewDirty);
            Assert.True(document.PreviewDirtyTracker.PreviewDirtyReason.HasFlag(PreviewDirtyReason.ProfileChanged));
            Assert.Equal(
                hasPendingOverride,
                document.PreviewDirtyTracker.PreviewDirtyReason.HasFlag(PreviewDirtyReason.FileOverridesChanged));
            Assert.Contains("last successfully built preview", document.PreviewNotice);

            context.DirtyStateService.RefreshWorkspaceDirtyState(document);
            Assert.Equal(workspaceDirty, document.IsWorkspaceDirty);

            rule.Pattern = "bin";
            if (hasPendingOverride)
                document.FilesPane.ResetAllFileOverridesCommand.Execute(null);

            context.DirtyStateService.RefreshPreviewDirtyState(document);
            context.DirtyStateService.RefreshWorkspaceDirtyState(document);

            Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);
            Assert.Equal(PreviewDirtyReason.None, document.PreviewDirtyTracker.PreviewDirtyReason);
            Assert.Equal(!workspaceDirty, document.IsWorkspaceDirty);
            Assert.Equal(previousCalls, context.CaptureCallCounts());

            return true;
        });
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Preserve_Files_And_Overrides_When_Filter_Regex_Times_Out()
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDocumentViewModel document = CreatePreviewDocument();

            await context.Service.BuildPreviewAsync(document);
            AssertSuccessfulPreview(document);

            InputFileItemViewModel regularFile = document.FilesPane.Files.Single(x => x.FullPath == RegularFilePath);
            InputFileItemViewModel generatedFile =
                document.FilesPane.Files.Single(x => x.FullPath == GeneratedFilePath);
            document.FilesPane.ReplaceSelectedFiles([regularFile]);
            document.FilesPane.ContextFile = regularFile;
            regularFile.IsIncluded = false;
            generatedFile.IsIncluded = true;

            MergeOutput? previousOutput = document.LastOutput;
            string previousContent = document.PreviewContent;
            string previousCharacterCount = document.PreviewCharacterCountText;
            PreviewGenerationSummaryViewModel previousSummary = document.PreviewSummary;
            IReadOnlyDictionary<string, bool> previousAppliedState = document.AppliedPreviewFileStateStore.Current;
            InputFileItemViewModel[] previousFiles = [.. document.FilesPane.Files];
            bool[] previousInclusion = [.. previousFiles.Select(x => x.IsIncluded)];
            bool[] previousPendingState = [.. previousFiles.Select(x => x.HasPendingPreviewState)];
            var previousOverrides = document.FilesPane.BuildOverrides()
                .ToDictionary(x => x.FullPath, x => x.IsIncluded, StringComparer.OrdinalIgnoreCase);

            Assert.Equal(2, previousOverrides.Count);
            Assert.False(previousOverrides[RegularFilePath]);
            Assert.True(previousOverrides[GeneratedFilePath]);

            context.Filter.ThrowRegexMatchTimeout = true;

            await context.Service.BuildPreviewAsync(document);

            ValidationIssueItemViewModel issue = Assert.Single(document.ValidationPane.Issues);
            Assert.Equal("profile.filterRules.invalid", issue.Code);
            Assert.Equal(ValidationSeverity.Error, issue.SeverityValue);
            Assert.Contains("250 ms timeout", issue.Message);
            Assert.Equal(StatusSeverity.Error, document.OperationStatus.StatusSeverity);
            Assert.Equal("Preview build failed due to validation errors.", document.OperationStatus.StatusMessage);
            AssertBuildStopped(document);
            Assert.Same(previousOutput, document.LastOutput);
            Assert.Equal(previousContent, document.PreviewContent);
            Assert.Equal(previousCharacterCount, document.PreviewCharacterCountText);
            Assert.Same(previousSummary, document.PreviewSummary);
            Assert.Same(previousAppliedState, document.AppliedPreviewFileStateStore.Current);
            Assert.Equal(previousFiles, document.FilesPane.Files.ToArray());
            Assert.Equal(previousInclusion, document.FilesPane.Files.Select(x => x.IsIncluded).ToArray());
            Assert.Equal(
                previousPendingState,
                document.FilesPane.Files.Select(x => x.HasPendingPreviewState).ToArray());
            Assert.Same(regularFile, Assert.Single(document.FilesPane.SelectedFiles));
            Assert.Same(regularFile, document.FilesPane.ContextFile);

            var retainedOverrides = document.FilesPane.BuildOverrides()
                .ToDictionary(x => x.FullPath, x => x.IsIncluded, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(2, retainedOverrides.Count);
            Assert.False(retainedOverrides[RegularFilePath]);
            Assert.True(retainedOverrides[GeneratedFilePath]);
            Assert.True(document.PreviewDirtyTracker.IsPreviewDirty);
            Assert.True(
                document.PreviewDirtyTracker.PreviewDirtyReason.HasFlag(PreviewDirtyReason.FileOverridesChanged));
            Assert.Contains("last successfully built preview", document.PreviewNotice);

            context.Filter.ThrowRegexMatchTimeout = false;

            await context.Service.BuildPreviewAsync(document);

            AssertSuccessfulPreview(document);
            InputFileItemViewModel rebuiltRegularFile =
                document.FilesPane.Files.Single(x => x.FullPath == RegularFilePath);
            InputFileItemViewModel rebuiltGeneratedFile =
                document.FilesPane.Files.Single(x => x.FullPath == GeneratedFilePath);
            Assert.False(rebuiltRegularFile.IsIncluded);
            Assert.True(rebuiltRegularFile.HasManualOverride);
            Assert.False(rebuiltRegularFile.HasPendingPreviewState);
            Assert.True(rebuiltGeneratedFile.IsIncluded);
            Assert.True(rebuiltGeneratedFile.HasManualOverride);
            Assert.False(rebuiltGeneratedFile.HasPendingPreviewState);
            Assert.False(document.AppliedPreviewFileStateStore.Current[RegularFilePath]);
            Assert.True(document.AppliedPreviewFileStateStore.Current[GeneratedFilePath]);
            Assert.Contains(GeneratedContent, document.LastOutput!.Content);

            var rebuiltOverrides = document.FilesPane.BuildOverrides()
                .ToDictionary(x => x.FullPath, x => x.IsIncluded, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(2, rebuiltOverrides.Count);
            Assert.False(rebuiltOverrides[RegularFilePath]);
            Assert.True(rebuiltOverrides[GeneratedFilePath]);

            return true;
        });
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Rebuild_With_Bin_Excluded_After_Invalid_Rule_Is_Corrected()
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDocumentViewModel document = CreatePreviewDocument();

            await context.Service.BuildPreviewAsync(document);

            AssertSuccessfulPreview(document);
            AssertGeneratedContentExcluded(document);
            Assert.Equal<string>([RegularFilePath], context.Reader.ReadPaths);
            Assert.Equal(new PipelineCallCounts(1, 1, 1, 1, 1, 1, 1, 1), context.CaptureCallCounts());

            MergeOutput? previousOutput = document.LastOutput;
            ProfileFilterRuleItemViewModel rule = Assert.Single(document.ProfileEditor.FilterRules);
            rule.Pattern = string.Empty;

            await context.Service.BuildPreviewAsync(document);

            AssertProfileValidationFailure(document);
            Assert.Same(previousOutput, document.LastOutput);
            Assert.Equal(new PipelineCallCounts(1, 1, 1, 1, 1, 1, 1, 1), context.CaptureCallCounts());

            rule.Pattern = "bin";
            const string correctedContent = "// regular source after correction";
            context.Reader.ContentFactory = file =>
                file.FullPath == GeneratedFilePath ? GeneratedContent : correctedContent;

            await context.Service.BuildPreviewAsync(document);

            AssertSuccessfulPreview(document);
            AssertGeneratedContentExcluded(document);
            Assert.NotSame(previousOutput, document.LastOutput);
            Assert.Contains(correctedContent, document.LastOutput!.Content);
            Assert.Contains(correctedContent, document.PreviewContent);
            Assert.Equal(new PipelineCallCounts(2, 2, 2, 2, 2, 2, 2, 2), context.CaptureCallCounts());
            Assert.Equal<string>([RegularFilePath, RegularFilePath], context.Reader.ReadPaths);
            Assert.Empty(document.ValidationPane.Issues);
            Assert.False(document.ValidationPane.IsDetailsExpanded);
            Assert.False(document.HasPreviewNotice);
            Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);
            Assert.True(document.PreviewDirtyTracker.HasAppliedPreview);
            Assert.True(document.AppliedPreviewFileStateStore.Current[RegularFilePath]);
            Assert.False(document.AppliedPreviewFileStateStore.Current[GeneratedFilePath]);

            return true;
        });
    }

    [Theory]
    [InlineData(RulePatternType.Exact, "")]
    [InlineData(RulePatternType.Regex, "[")]
    public async Task BuildPreviewAsync_Should_Reject_Invalid_Profile_Loaded_From_Workspace(
        RulePatternType patternType,
        string pattern)
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDto savedWorkspace = WorkspaceDocumentMapper.Capture(CreatePreviewDocument());
            WorkspaceDto invalidWorkspace = new(
                savedWorkspace.Document with
                {
                    Profile = savedWorkspace.Document.Profile with
                    {
                        FilterRules =
                        [
                            CreateBinExclusionRule() with { PatternType = patternType, Pattern = pattern }
                        ]
                    },
                    InclusionOverrides = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                    {
                        [RegularFilePath] = false
                    }
                });

            WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDefaultDocument();
            WorkspaceDocumentMapper.Apply(document, invalidWorkspace);
            document.WorkspaceFilePath = @"D:\Workspaces\legacy.filemerger.workspace.json";
            context.DirtyStateService.MarkWorkspaceSaved(document);
            WorkspaceDirtyStateTracker unchangedState = CreateStateWitness(document);

            Assert.False(document.ProfileEditor.CanUseProfile);
            Assert.Equal(pattern, Assert.Single(document.ProfileEditor.FilterRules).Pattern);

            await context.Service.BuildPreviewAsync(document);

            Assert.Equal(new PipelineCallCounts(0, 0, 0, 0, 0, 0, 0, 0), context.CaptureCallCounts());
            AssertProfileValidationFailure(document);
            AssertBuildStopped(document);
            AssertStateUnchanged(unchangedState, document);
            Assert.False(document.IsWorkspaceDirty);
            Assert.Null(document.LastOutput);
            Assert.Empty(document.FilesPane.Files);
            Assert.False(document.HasPreviewSummary);
            Assert.False(document.AppliedPreviewFileStateStore.HasAppliedState);
            Assert.False(document.PreviewDirtyTracker.HasAppliedPreview);
            Assert.Equal(@"D:\Workspaces\legacy.filemerger.workspace.json", document.WorkspaceFilePath);
            Assert.False(document.FilesPane.CaptureOverridesDictionary()[RegularFilePath]);

            document.ProfileEditor.FilterRules[0].Pattern = "bin";

            await context.Service.BuildPreviewAsync(document);

            AssertSuccessfulPreview(document);
            Assert.Empty(document.ValidationPane.Issues);
            Assert.Empty(document.LastOutput!.Sections);
            Assert.False(document.FilesPane.CaptureOverridesDictionary()[RegularFilePath]);
            Assert.True(document.AppliedPreviewFileStateStore.HasAppliedState);
            Assert.False(document.PreviewDirtyTracker.IsPreviewDirty);
            Assert.Equal(new PipelineCallCounts(1, 1, 1, 1, 1, 0, 0, 1), context.CaptureCallCounts());

            return true;
        });
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Not_Change_Another_Document_On_Profile_Validation_Failure()
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDocumentViewModel otherDocument = CreatePreviewDocument();

            await context.Service.BuildPreviewAsync(otherDocument);
            AssertSuccessfulPreview(otherDocument);
            context.DirtyStateService.MarkWorkspaceSaved(otherDocument);

            otherDocument.SessionSettings.SessionName = "Other document edited";
            otherDocument.FilesPane.Files.Single(x => x.FullPath == RegularFilePath).IsIncluded = false;
            otherDocument.ValidationPane.Load(
            [
                new ValidationIssue(ValidationSeverity.Warning, "test.other", "Keep this document's warning.")
            ]);
            context.DirtyStateService.RefreshWorkspaceDirtyState(otherDocument);
            context.DirtyStateService.RefreshPreviewDirtyState(otherDocument);

            WorkspaceDirtyStateTracker unchangedState = CreateStateWitness(otherDocument);
            MergeOutput? previousOutput = otherDocument.LastOutput;
            string previousContent = otherDocument.PreviewContent;
            string previousCharacterCount = otherDocument.PreviewCharacterCountText;
            string previousNotice = otherDocument.PreviewNotice;
            string previousStatus = otherDocument.OperationStatus.StatusMessage;
            StatusSeverity previousSeverity = otherDocument.OperationStatus.StatusSeverity;
            PreviewGenerationSummaryViewModel previousSummary = otherDocument.PreviewSummary;
            PreviewDirtyReason previousReason = otherDocument.PreviewDirtyTracker.PreviewDirtyReason;
            ValidationIssueItemViewModel previousIssue = Assert.Single(otherDocument.ValidationPane.Issues);
            IReadOnlyDictionary<string, bool> previousAppliedState = otherDocument.AppliedPreviewFileStateStore.Current;
            InputFileItemViewModel[] previousFiles = [.. otherDocument.FilesPane.Files];
            PipelineCallCounts previousCalls = context.CaptureCallCounts();

            WorkspaceDocumentViewModel invalidDocument = CreatePreviewDocument();
            invalidDocument.ProfileEditor.FilterRules[0].Pattern = string.Empty;

            await context.Service.BuildPreviewAsync(invalidDocument);

            AssertProfileValidationFailure(invalidDocument);
            Assert.Equal(previousCalls, context.CaptureCallCounts());
            AssertStateUnchanged(unchangedState, otherDocument);
            Assert.True(otherDocument.IsWorkspaceDirty);
            Assert.True(otherDocument.PreviewDirtyTracker.IsPreviewDirty);
            Assert.Equal(previousReason, otherDocument.PreviewDirtyTracker.PreviewDirtyReason);
            Assert.Same(previousOutput, otherDocument.LastOutput);
            Assert.Equal(previousContent, otherDocument.PreviewContent);
            Assert.Equal(previousCharacterCount, otherDocument.PreviewCharacterCountText);
            Assert.Equal(previousNotice, otherDocument.PreviewNotice);
            Assert.Same(previousSummary, otherDocument.PreviewSummary);
            Assert.Same(previousAppliedState, otherDocument.AppliedPreviewFileStateStore.Current);
            Assert.Equal(previousFiles, otherDocument.FilesPane.Files.ToArray());
            Assert.Same(previousIssue, Assert.Single(otherDocument.ValidationPane.Issues));
            Assert.True(otherDocument.ValidationPane.IsDetailsExpanded);
            Assert.Equal(previousStatus, otherDocument.OperationStatus.StatusMessage);
            Assert.Equal(previousSeverity, otherDocument.OperationStatus.StatusSeverity);
            AssertBuildStopped(invalidDocument);
            AssertBuildStopped(otherDocument);

            return true;
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task BuildPreviewAsync_Should_Honor_PreCanceled_Token_Before_Profile_Validation(
        bool hasSuccessfulPreview,
        bool hasInvalidProfile)
    {
        await RunOnStaThreadAsync(async () =>
        {
            PreviewTestContext context = CreatePreviewTestContext();
            WorkspaceDocumentViewModel document = CreatePreviewDocument();

            if (hasSuccessfulPreview)
            {
                await context.Service.BuildPreviewAsync(document);
                AssertSuccessfulPreview(document);
            }

            if (hasInvalidProfile)
                document.ProfileEditor.FilterRules[0].Pattern = string.Empty;

            context.DirtyStateService.RefreshPreviewDirtyState(document);
            document.ValidationPane.Load(
            [
                new ValidationIssue(ValidationSeverity.Warning, "test.previous", "Previous validation result.")
            ]);

            ValidationIssueItemViewModel previousIssue = Assert.Single(document.ValidationPane.Issues);
            WorkspaceDirtyStateTracker unchangedState = CreateStateWitness(document);
            PipelineCallCounts previousCalls = context.CaptureCallCounts();
            MergeOutput? previousOutput = document.LastOutput;
            string previousContent = document.PreviewContent;
            string previousCharacterCount = document.PreviewCharacterCountText;
            PreviewGenerationSummaryViewModel previousSummary = document.PreviewSummary;
            IReadOnlyDictionary<string, bool> previousAppliedState = document.AppliedPreviewFileStateStore.Current;
            PreviewDirtyReason previousReason = document.PreviewDirtyTracker.PreviewDirtyReason;
            List<bool> callbackBusyStates = [];

            using CancellationTokenSource cancellation = new();
            // Arrange an already-canceled token before the service registers any callbacks.
            // ReSharper disable once MethodHasAsyncOverload
            cancellation.Cancel();

            await context.Service.BuildPreviewAsync(
                document,
                stateChanged: () => callbackBusyStates.Add(document.OperationStatus.IsBusy),
                cancellationToken: cancellation.Token);

            Assert.Equal(previousCalls, context.CaptureCallCounts());
            Assert.Equal("Preview build canceled.", document.OperationStatus.StatusMessage);
            Assert.Equal(StatusSeverity.Warning, document.OperationStatus.StatusSeverity);
            Assert.Same(previousIssue, Assert.Single(document.ValidationPane.Issues));
            Assert.False(document.ValidationPane.HasErrors);
            AssertStateUnchanged(unchangedState, document);
            Assert.Same(previousOutput, document.LastOutput);
            Assert.Equal(previousContent, document.PreviewContent);
            Assert.Equal(previousCharacterCount, document.PreviewCharacterCountText);
            Assert.Same(previousSummary, document.PreviewSummary);
            Assert.Same(previousAppliedState, document.AppliedPreviewFileStateStore.Current);
            Assert.Equal(previousReason, document.PreviewDirtyTracker.PreviewDirtyReason);
            Assert.Equal(hasSuccessfulPreview, document.HasPreviewNotice);
            Assert.Equal(hasSuccessfulPreview, document.PreviewDirtyTracker.HasAppliedPreview);
            if (hasSuccessfulPreview)
                Assert.Contains("canceled", document.PreviewNotice);

            Assert.False(callbackBusyStates.Last());
            AssertBuildStopped(document);
            document.CancelBuildPreview();

            return true;
        });
    }

    private static PreviewTestContext CreatePreviewTestContext()
    {
        CountingMainStateFactory mainStateFactory = new();
        WorkspaceDocumentDirtyStateService dirtyStateService = new(mainStateFactory);
        PassingMergeSessionValidator validator = new();
        FixedFileDiscoveryService discovery = new(
            new FileDiscoveryResult(
            [
                new InputFile(RegularFilePath, "Program.cs", ".cs", FileKind.CSharp),
                new InputFile(GeneratedFilePath, GeneratedRelativePath, ".cs", FileKind.CSharp)
            ]));
        CountingFileFilterService filter = new();
        CountingFileInclusionOverrideService overrides = new();
        SuccessfulContentReader reader = new()
        {
            ContentFactory = file => file.FullPath == GeneratedFilePath ? GeneratedContent : "// regular source"
        };
        PassThroughContentTransformationService transformations = new();
        CountingMergeBuilder builder = new();
        BuildMergePreviewUseCase useCase = new(
            validator,
            discovery,
            filter,
            overrides,
            reader,
            transformations,
            builder);
        WorkspaceDocumentPreviewService service = new(
            useCase,
            mainStateFactory,
            dirtyStateService,
            new FakeApplicationPreferencesStore());

        return new PreviewTestContext(
            service,
            mainStateFactory,
            dirtyStateService,
            validator,
            discovery,
            filter,
            overrides,
            reader,
            transformations,
            builder);
    }

    private static WorkspaceDocumentViewModel CreatePreviewDocument()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDocument();
        document.SourcesPane.LoadSources(
        [
            new MergeSource(path: @"D:\Project", type: MergeSourceType.Directory)
        ]);
        document.ProfileEditor.FilterRules.Clear();
        document.ProfileEditor.FilterRules.Add(new ProfileFilterRuleItemViewModel(CreateBinExclusionRule()));
        document.ProfileEditor.IncludeHeaderComment = true;
        document.ProfileEditor.IncludeBuildTimestampMetadata = false;
        document.ProfileEditor.SkippedFilesMetadataMode = SkippedFilesMetadataMode.Simple;
        document.ProfileEditor.IncludeProfileExclusionsInSkippedMetadata = true;
        document.ProfileEditor.WorkingProfileName = "Preview test profile";
        document.CurrentProfileEntryId = "custom.preview-test";
        document.ProfileOriginEntryId = "custom.preview-test";
        document.ProfileOriginDisplayName = "Preview test profile";

        return document;
    }

    private static WorkspaceFileFilterRuleDto CreateBinExclusionRule()
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: "bin",
            IsEnabled: true,
            Description: "Exclude build output",
            IsUserEditable: true);
    }

    private static WorkspaceDirtyStateTracker CreateStateWitness(WorkspaceDocumentViewModel document)
    {
        WorkspaceDirtyStateTracker tracker = new();
        tracker.MarkWorkspaceSaved(WorkspaceDocumentStateSnapshotFactory.Capture(document));
        return tracker;
    }

    private static void AssertStateUnchanged(WorkspaceDirtyStateTracker witness, WorkspaceDocumentViewModel document)
    {
        witness.Refresh(WorkspaceDocumentStateSnapshotFactory.Capture(document));
        Assert.False(witness.IsWorkspaceDirty);
    }

    private static void AssertProfileValidationFailure(WorkspaceDocumentViewModel document)
    {
        ValidationIssueItemViewModel issue = Assert.Single(document.ValidationPane.Issues);
        Assert.Equal("profile.filterRules.invalid", issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.SeverityValue);
        Assert.Equal(document.ProfileEditor.DraftValidationMessage, issue.Message);
        Assert.True(document.ValidationPane.HasErrors);
        Assert.Equal(1, document.ValidationPane.ErrorCount);
        Assert.True(document.ValidationPane.IsDetailsExpanded);
        Assert.True(document.ValidationPane.HasVisibleDetails);
        Assert.Equal(StatusSeverity.Error, document.OperationStatus.StatusSeverity);
        Assert.Equal(
            $"Preview build failed due to validation errors. {document.ProfileEditor.DraftValidationMessage}",
            document.OperationStatus.StatusMessage);
    }

    private static void AssertOutputPathValidationFailure(WorkspaceDocumentViewModel document)
    {
        const string validationMessage = "Output path cannot be empty. Choose an output file in Session settings.";

        ValidationIssueItemViewModel issue = Assert.Single(document.ValidationPane.Issues);
        Assert.Equal("output.path.empty", issue.Code);
        Assert.Equal(ValidationSeverity.Error, issue.SeverityValue);
        Assert.Equal(validationMessage, issue.Message);
        Assert.True(document.ValidationPane.HasErrors);
        Assert.Equal(1, document.ValidationPane.ErrorCount);
        Assert.True(document.ValidationPane.IsDetailsExpanded);
        Assert.True(document.ValidationPane.HasVisibleDetails);
        Assert.Equal(StatusSeverity.Error, document.OperationStatus.StatusSeverity);
        Assert.Equal(
            $"Preview build failed due to validation errors. {validationMessage}",
            document.OperationStatus.StatusMessage);
    }

    private static void AssertSuccessfulPreview(WorkspaceDocumentViewModel document)
    {
        Assert.True(document.LastOutput is not null, document.OperationStatus.StatusMessage);
        Assert.Equal("Preview built successfully.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Success, document.OperationStatus.StatusSeverity);
    }

    private static void AssertBuildStopped(WorkspaceDocumentViewModel document)
    {
        Assert.False(document.OperationStatus.IsBusy);
        Assert.False(document.OperationStatus.IsCancelable);
        Assert.False(document.OperationStatus.IsProgressVisible);
        Assert.False(document.OperationStatus.CancelCommand.CanExecute(null));
    }

    private static void AssertGeneratedContentExcluded(WorkspaceDocumentViewModel document)
    {
        Assert.NotNull(document.LastOutput);
        MergeSection section = Assert.Single(document.LastOutput.Sections);
        Assert.Equal(RegularFilePath, section.SourceFile.FullPath);
        Assert.DoesNotContain(document.LastOutput.Sections, x => x.SourceFile.FullPath == GeneratedFilePath);
        Assert.DoesNotContain(GeneratedContent, document.LastOutput.Content);
        Assert.DoesNotContain(GeneratedContent, document.PreviewContent);
        Assert.Contains(GeneratedRelativePath, document.LastOutput.Content);
        Assert.False(document.FilesPane.Files.Single(x => x.FullPath == GeneratedFilePath).IsIncluded);
    }

    private static async Task<WorkspaceDocumentViewModel> BuildPreviewAsync(string fullContent)
    {
        return await RunOnStaThreadAsync(async () =>
        {
            FakeApplicationPreferencesStore preferencesStore = new();
            preferencesStore.SetCurrent(
                new ApplicationPreferences(
                    isPreviewLineWrapEnabledByDefault: false,
                    previewDisplayCharacterLimit: CustomDisplayLimit,
                    crashLogRetentionLimit: ApplicationPreferences.DefaultCrashLogRetentionLimit));

            BuildMergePreviewUseCase useCase = new(
                new PassingMergeSessionValidator(),
                new EmptyFileDiscoveryService(),
                new PassThroughFileFilterService(),
                new PassThroughFileInclusionOverrideService(),
                new UnusedContentReader(),
                new UnusedContentTransformationService(),
                new FixedOutputMergeBuilder(fullContent));

            MainStateFactory mainStateFactory = new();
            WorkspaceDocumentPreviewService service = new(
                useCase,
                mainStateFactory,
                new WorkspaceDocumentDirtyStateService(mainStateFactory),
                preferencesStore);

            WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDocument();

            await service.BuildPreviewAsync(document);

            Assert.True(document.LastOutput is not null, document.OperationStatus.StatusMessage);
            Assert.True(
                document.OperationStatus.StatusMessage == "Preview built successfully.",
                document.OperationStatus.StatusMessage);

            return document;
        });
    }

    private static Task<T> RunOnStaThreadAsync<T>(Func<Task<T>> action)
    {
        TaskCompletionSource<T> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Thread thread = new(() =>
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            _ = dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    completion.SetResult(await action());
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
                finally
                {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }
            });

            Dispatcher.Run();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completion.Task;
    }

    private sealed class PassingMergeSessionValidator : IMergeSessionValidator
    {
        public int CallCount { get; private set; }

        public IReadOnlyCollection<ValidationIssue> Validate(MergeSession session)
        {
            CallCount++;
            return [];
        }
    }

    private sealed class EmptyFileDiscoveryService : IFileDiscoveryService
    {
        public FileDiscoveryResult DiscoverFiles(
            IReadOnlyCollection<MergeSource> sources,
            MergeProfile profile,
            IProgress<FileDiscoveryProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return new FileDiscoveryResult([]);
        }
    }

    private sealed class PassThroughFileFilterService : IFileFilterService
    {
        public IReadOnlyCollection<InputFile> ApplyFilters(IReadOnlyCollection<InputFile> files, MergeProfile profile)
        {
            return files;
        }
    }

    private sealed class PassThroughFileInclusionOverrideService : IFileInclusionOverrideService
    {
        public IReadOnlyCollection<InputFile> ApplyOverrides(
            IReadOnlyCollection<InputFile> files,
            IReadOnlyCollection<FileInclusionOverride> overrides)
        {
            return files;
        }
    }

    private sealed class UnusedContentReader : IContentReader
    {
        public ContentReadResult Read(InputFile file, InputReadOptions options)
        {
            throw new InvalidOperationException("No files should be read in this test.");
        }
    }

    private sealed class UnusedContentTransformationService : IContentTransformationService
    {
        public string Transform(string content, InputFile file, MergeProfile profile)
        {
            throw new InvalidOperationException("No content should be transformed in this test.");
        }
    }

    private sealed class FixedFileDiscoveryService(FileDiscoveryResult result) : IFileDiscoveryService
    {
        private readonly FileDiscoveryResult _result = result;

        public int CallCount { get; private set; }

        public FileDiscoveryResult DiscoverFiles(
            IReadOnlyCollection<MergeSource> sources,
            MergeProfile profile,
            IProgress<FileDiscoveryProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return _result;
        }
    }

    private sealed class FixedFileFilterService(IReadOnlyCollection<InputFile> result) : IFileFilterService
    {
        private readonly IReadOnlyCollection<InputFile> _result = result;

        public IReadOnlyCollection<InputFile> ApplyFilters(IReadOnlyCollection<InputFile> files, MergeProfile profile)
        {
            return _result;
        }
    }

    private sealed class FixedFileInclusionOverrideService(IReadOnlyCollection<InputFile> result)
        : IFileInclusionOverrideService
    {
        private readonly IReadOnlyCollection<InputFile> _result = result;

        public IReadOnlyCollection<InputFile> ApplyOverrides(
            IReadOnlyCollection<InputFile> files,
            IReadOnlyCollection<FileInclusionOverride> overrides)
        {
            return _result;
        }
    }

    private sealed class SuccessfulContentReader : IContentReader
    {
        public List<string> ReadPaths { get; } = [];
        public Func<InputFile, string> ContentFactory { get; set; } = _ => "content";

        public ContentReadResult Read(InputFile file, InputReadOptions options)
        {
            ReadPaths.Add(file.FullPath);
            return ContentReadResult.Success(ContentFactory(file), "utf-8");
        }
    }

    private sealed class PassThroughContentTransformationService : IContentTransformationService
    {
        public int CallCount { get; private set; }

        public string Transform(string content, InputFile file, MergeProfile profile)
        {
            CallCount++;
            return content;
        }
    }

    private sealed class FixedOutputMergeBuilder(string content) : IMergeBuilder
    {
        private readonly string _content = content;

        public MergeOutput Build(
            MergeSession session,
            IReadOnlyCollection<MergeSection> sections,
            TimeSpan totalDuration,
            IReadOnlyCollection<InputFile>? sourceExcludedFiles = null)
        {
            return new MergeOutput(
                content: _content,
                sections: sections,
                statistics: new MergeStatistics(
                    filesScanned: 0,
                    filesIncluded: 0,
                    filesSkipped: 0,
                    totalCharacters: _content.Length,
                    duration: totalDuration),
                generatedAtUtc: DateTime.UtcNow,
                outputTarget: session.OutputTarget);
        }
    }

    private sealed class CountingMainStateFactory : IMainStateFactory
    {
        private readonly MainStateFactory _inner = new();

        public int BuildSessionCallCount { get; private set; }

        public MergeSession BuildSession(WorkspaceDocumentViewModel document)
        {
            BuildSessionCallCount++;
            return _inner.BuildSession(document);
        }

        public PreviewStateSnapshot BuildPreviewState(WorkspaceDocumentViewModel document)
        {
            return _inner.BuildPreviewState(document);
        }

        public string? BuildInitialOutputPath(WorkspaceDocumentViewModel document)
        {
            return _inner.BuildInitialOutputPath(document);
        }
    }

    private sealed class CountingFileFilterService : IFileFilterService
    {
        private readonly FileFilterService _inner = new();

        public int CallCount { get; private set; }
        public bool ThrowRegexMatchTimeout { get; set; }

        public IReadOnlyCollection<InputFile> ApplyFilters(IReadOnlyCollection<InputFile> files, MergeProfile profile)
        {
            CallCount++;

            if (ThrowRegexMatchTimeout)
                throw new RegexMatchTimeoutException();

            return _inner.ApplyFilters(files, profile);
        }
    }

    private sealed class CountingFileInclusionOverrideService : IFileInclusionOverrideService
    {
        private readonly FileInclusionOverrideService _inner = new();

        public int CallCount { get; private set; }

        public IReadOnlyCollection<InputFile> ApplyOverrides(
            IReadOnlyCollection<InputFile> files,
            IReadOnlyCollection<FileInclusionOverride> overrides)
        {
            CallCount++;
            return _inner.ApplyOverrides(files, overrides);
        }
    }

    private sealed class CountingMergeBuilder : IMergeBuilder
    {
        private readonly MergeBuilder _inner = new();

        public int CallCount { get; private set; }

        public MergeOutput Build(
            MergeSession session,
            IReadOnlyCollection<MergeSection> sections,
            TimeSpan totalDuration,
            IReadOnlyCollection<InputFile>? sourceExcludedFiles = null)
        {
            CallCount++;
            return _inner.Build(session, sections, totalDuration, sourceExcludedFiles);
        }
    }

    // All counters participate in the generated record equality used by Assert.Equal.
    // ReSharper disable NotAccessedPositionalProperty.Local
    private sealed record PipelineCallCounts(
        int Sessions,
        int Validations,
        int Discoveries,
        int Filters,
        int Overrides,
        int Reads,
        int Transformations,
        int Builds);
    // ReSharper restore NotAccessedPositionalProperty.Local

    private sealed record PreviewTestContext(
        WorkspaceDocumentPreviewService Service,
        CountingMainStateFactory StateFactory,
        WorkspaceDocumentDirtyStateService DirtyStateService,
        PassingMergeSessionValidator Validator,
        FixedFileDiscoveryService Discovery,
        CountingFileFilterService Filter,
        CountingFileInclusionOverrideService Overrides,
        SuccessfulContentReader Reader,
        PassThroughContentTransformationService Transformations,
        CountingMergeBuilder Builder)
    {
        public PipelineCallCounts CaptureCallCounts()
        {
            return new PipelineCallCounts(
                StateFactory.BuildSessionCallCount,
                Validator.CallCount,
                Discovery.CallCount,
                Filter.CallCount,
                Overrides.CallCount,
                Reader.ReadPaths.Count,
                Transformations.CallCount,
                Builder.CallCount);
        }
    }
}