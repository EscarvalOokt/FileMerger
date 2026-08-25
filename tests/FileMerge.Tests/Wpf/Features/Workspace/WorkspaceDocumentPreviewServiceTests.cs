using System.Windows.Threading;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.UseCases.BuildPreview;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentPreviewServiceTests
{
    private const int CustomDisplayLimit = 10_000;

    [Fact]
    public async Task BuildPreviewAsync_Should_Use_PreviewDisplayCharacterLimit_From_Preferences()
    {
        string fullContent = new('x', CustomDisplayLimit + 2_000);

        WorkspaceDocumentViewModel document =
            await BuildPreviewAsync(fullContent);

        Assert.True(document.PreviewContent.Length < fullContent.Length);
        Assert.StartsWith(fullContent[..CustomDisplayLimit], document.PreviewContent);
        Assert.Contains(
            "Save still writes the full output.",
            document.PreviewContent);
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Keep_LastOutput_Content_Full_When_Preview_Is_Truncated()
    {
        string fullContent = new('x', CustomDisplayLimit + 2_000);

        WorkspaceDocumentViewModel document =
            await BuildPreviewAsync(fullContent);

        Assert.NotNull(document.LastOutput);
        Assert.Equal(fullContent, document.LastOutput.Content);
    }

    [Fact]
    public async Task BuildPreviewAsync_Should_Report_Truncation_In_Summary_With_Custom_Limit()
    {
        string fullContent = new('x', CustomDisplayLimit + 2_000);

        WorkspaceDocumentViewModel document =
            await BuildPreviewAsync(fullContent);

        Assert.Equal(fullContent.Length, document.PreviewSummary.TotalCharacters);
        Assert.Equal(
            CustomDisplayLimit,
            document.PreviewSummary.DisplayedCharacters);
        Assert.Equal(
            fullContent.Length - CustomDisplayLimit,
            document.PreviewSummary.OmittedCharacters);
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
                skipReason: new SkipReason(
                    "discovery.file-type-disabled",
                    "File type is disabled."),
                isMergeCandidate: false);

            InputFile filtered = new(
                fullPath: @"D:\Project\Filtered.cs",
                relativePath: "Filtered.cs",
                extension: ".cs",
                kind: FileKind.CSharp,
                isIncluded: false,
                skipReason: new SkipReason(
                    "filter.rule.exclude",
                    "Excluded by profile filter."));

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
                skipReason: new SkipReason(
                    "source.exclude",
                    "Excluded by a source-specific exclusion."),
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
                manuallyExcluded.Exclude(new SkipReason(
                    "manual.exclude",
                    "Excluded manually by user."))
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

            WorkspaceDocumentViewModel document =
                WorkspaceDocumentTestFactory.CreateDocument();

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

    private static async Task<WorkspaceDocumentViewModel> BuildPreviewAsync(
        string fullContent)
    {
        return await RunOnStaThreadAsync(async () =>
        {
            FakeApplicationPreferencesStore preferencesStore = new();
            preferencesStore.SetCurrent(new ApplicationPreferences(
                isPreviewLineWrapEnabledByDefault: false,
                previewDisplayCharacterLimit: CustomDisplayLimit,
                crashLogRetentionLimit:
                ApplicationPreferences.DefaultCrashLogRetentionLimit));

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

            WorkspaceDocumentViewModel document =
                WorkspaceDocumentTestFactory.CreateDocument();

            await service.BuildPreviewAsync(document);

            Assert.True(
                document.LastOutput is not null,
                document.OperationStatus.StatusMessage);
            Assert.True(
                document.OperationStatus.StatusMessage == "Preview built successfully.",
                document.OperationStatus.StatusMessage);

            return document;
        });
    }

    private static Task<T> RunOnStaThreadAsync<T>(Func<Task<T>> action)
    {
        TaskCompletionSource<T> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Thread thread = new(() =>
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(dispatcher));

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
        public IReadOnlyCollection<ValidationIssue> Validate(MergeSession session)
        {
            return [];
        }
    }

    private sealed class EmptyFileDiscoveryService : IFileDiscoveryService
    {
        public FileDiscoveryResult DiscoverFiles(
            IReadOnlyCollection<MergeSource> sources,
            MergeProfile profile,
            IProgress<FileDiscoveryProgress>? progress = null)
        {
            return new FileDiscoveryResult([]);
        }
    }

    private sealed class PassThroughFileFilterService : IFileFilterService
    {
        public IReadOnlyCollection<InputFile> ApplyFilters(
            IReadOnlyCollection<InputFile> files,
            MergeProfile profile)
        {
            return files;
        }
    }

    private sealed class PassThroughFileInclusionOverrideService :
        IFileInclusionOverrideService
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
            throw new InvalidOperationException(
                "No files should be read in this test.");
        }
    }

    private sealed class UnusedContentTransformationService :
        IContentTransformationService
    {
        public string Transform(
            string content,
            InputFile file,
            MergeProfile profile)
        {
            throw new InvalidOperationException(
                "No content should be transformed in this test.");
        }
    }

    private sealed class FixedFileDiscoveryService(FileDiscoveryResult result) : IFileDiscoveryService
    {
        private readonly FileDiscoveryResult _result = result;

        public FileDiscoveryResult DiscoverFiles(
            IReadOnlyCollection<MergeSource> sources,
            MergeProfile profile,
            IProgress<FileDiscoveryProgress>? progress = null)
        {
            return _result;
        }
    }

    private sealed class FixedFileFilterService(
        IReadOnlyCollection<InputFile> result) : IFileFilterService
    {
        private readonly IReadOnlyCollection<InputFile> _result = result;

        public IReadOnlyCollection<InputFile> ApplyFilters(
            IReadOnlyCollection<InputFile> files,
            MergeProfile profile)
        {
            return _result;
        }
    }

    private sealed class FixedFileInclusionOverrideService(
        IReadOnlyCollection<InputFile> result) : IFileInclusionOverrideService
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
        public ContentReadResult Read(InputFile file, InputReadOptions options)
        {
            return ContentReadResult.Success("content", "utf-8");
        }
    }

    private sealed class PassThroughContentTransformationService :
        IContentTransformationService
    {
        public string Transform(
            string content,
            InputFile file,
            MergeProfile profile)
        {
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
}