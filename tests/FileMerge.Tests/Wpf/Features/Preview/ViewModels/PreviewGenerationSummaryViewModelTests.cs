using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Preview;
using FileMerger.Wpf.Features.Preview.ViewModels;

namespace FileMerger.Tests.Wpf.Features.Preview.ViewModels;

public sealed class PreviewGenerationSummaryViewModelTests
{
    [Fact]
    public void Empty_Should_Have_No_Summary()
    {
        PreviewGenerationSummaryViewModel summary = PreviewGenerationSummaryViewModel.Empty;

        Assert.False(summary.HasSummary);
        Assert.True(summary.HasNoSummary);
        Assert.Equal("Not generated", summary.GeneratedAtText);
        Assert.Equal("Not set", summary.OutputPathText);
    }

    [Fact]
    public void From_Should_Build_Discovery_Summary_From_Automatic_And_Current_State()
    {
        DateTime generatedAtUtc = new(2026, 6, 25, 10, 30, 0, DateTimeKind.Utc);

        MergeOutput output = new(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(
                filesScanned: 99,
                filesIncluded: 98,
                filesSkipped: 1,
                totalCharacters: 1200,
                duration: TimeSpan.FromMilliseconds(250)),
            generatedAtUtc: generatedAtUtc,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));

        InputFile included = CreateFile("Included.cs");
        InputFile fallbackIncluded = CreateFile(
            "Notes.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

        InputFile disabled = CreateFile(
            "Disabled.json",
            extension: ".json",
            kind: FileKind.Json,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.file-type-disabled",
                "File type is disabled."),
            isMergeCandidate: false);

        InputFile unsupported = CreateFile(
            "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is unsupported."),
            isMergeCandidate: false);

        InputFile profileExcluded = CreateFile(
            "ProfileExcluded.cs",
            isIncluded: false,
            skipReason: new SkipReason(
                "filter.rule.exclude",
                "Excluded by profile filter."));

        InputFile automaticallyExcludedButManuallyIncluded = CreateFile(
            "ManualInclude.cs",
            isIncluded: false,
            skipReason: new SkipReason(
                "filter.rule.exclude",
                "Excluded by profile filter."));

        InputFile automaticallyIncludedButManuallyExcluded = CreateFile(
            "ManualExclude.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

        InputFile otherExcluded = CreateFile(
            "Other.dat",
            extension: ".dat",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "custom.exclude",
                "Excluded for another reason."),
            isMergeCandidate: false);

        InputFile[] automaticFiles =
        [
            included,
            fallbackIncluded,
            disabled,
            unsupported,
            profileExcluded,
            automaticallyExcludedButManuallyIncluded,
            automaticallyIncludedButManuallyExcluded,
            otherExcluded
        ];

        InputFile[] currentFiles =
        [
            included,
            fallbackIncluded,
            profileExcluded,
            automaticallyExcludedButManuallyIncluded.Include(),
            automaticallyIncludedButManuallyExcluded.Exclude(new SkipReason(
                "manual.exclude",
                "Excluded manually by user."))
        ];

        InputFile sourceExcluded = CreateFile(
            "Generated.cs",
            isIncluded: false,
            skipReason: new SkipReason(
                "source.exclude",
                "Excluded by a source-specific exclusion."),
            isMergeCandidate: false);

        PreviewTextFormatResult previewText = new(
            Text: "preview",
            TotalCharacters: 1200,
            DisplayedCharacters: 1200,
            WasTruncated: false,
            OmittedCharacters: 0);

        var summary = PreviewGenerationSummaryViewModel.From(
            output,
            automaticFiles,
            currentFiles,
            [sourceExcluded],
            previewText);

        Assert.True(summary.HasSummary);
        Assert.False(summary.HasNoSummary);
        Assert.Equal(8, summary.FilesDiscovered);
        Assert.Equal(3, summary.FilesIncluded);
        Assert.Equal(5, summary.FilesNotIncluded);
        Assert.Equal(1, summary.DisabledFileTypeFiles);
        Assert.Equal(1, summary.UnsupportedFiles);
        Assert.Equal(1, summary.ProfileExcludedFiles);
        Assert.Equal(1, summary.ManuallyExcludedFiles);
        Assert.Equal(1, summary.OtherNotIncludedFiles);
        Assert.Equal(1, summary.FallbackTextFiles);
        Assert.Equal(1, summary.SourceExcludedFiles);
        Assert.Equal(summary.FilesDiscovered, summary.FilesIncluded + summary.FilesNotIncluded);
        Assert.Equal(
            summary.FilesNotIncluded,
            summary.DisabledFileTypeFiles
            + summary.UnsupportedFiles
            + summary.ProfileExcludedFiles
            + summary.ManuallyExcludedFiles
            + summary.OtherNotIncludedFiles);
        Assert.Equal(1200, summary.TotalCharacters);
        Assert.Equal(1200, summary.DisplayedCharacters);
        Assert.False(summary.HasTruncationNotice);
        Assert.Equal(@"D:\Output\merged.txt", summary.OutputPathText);
        Assert.Contains("Discovered: 8", summary.CompactTooltip);
        Assert.Contains("source excluded: 1", summary.CompactTooltip);
    }

    [Fact]
    public void From_Should_Use_Final_Inclusion_State_Over_Automatic_Filter_State()
    {
        InputFile automaticFile = CreateFile(
            "Filtered.cs",
            isIncluded: false,
            skipReason: new SkipReason(
                "filter.rule.exclude",
                "Excluded by profile filter."));

        InputFile currentFile = automaticFile.Include();

        var summary = PreviewGenerationSummaryViewModel.From(
            CreateOutput(),
            [automaticFile],
            [currentFile],
            [],
            CreatePreviewText());

        Assert.Equal(1, summary.FilesDiscovered);
        Assert.Equal(1, summary.FilesIncluded);
        Assert.Equal(0, summary.FilesNotIncluded);
        Assert.Equal(0, summary.ProfileExcludedFiles);
    }

    [Fact]
    public void From_Should_Show_Truncation_Notice_When_Preview_Is_Truncated()
    {
        MergeOutput output = new(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(
                filesScanned: 1,
                filesIncluded: 1,
                filesSkipped: 0,
                totalCharacters: 600000,
                duration: TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: null);

        PreviewTextFormatResult previewText = new(
            Text: "preview",
            TotalCharacters: 600000,
            DisplayedCharacters: 500000,
            WasTruncated: true,
            OmittedCharacters: 100000);

        var summary = PreviewGenerationSummaryViewModel.From(
            output,
            [],
            [],
            [],
            previewText);

        Assert.True(summary.WasTruncated);
        Assert.True(summary.HasTruncationNotice);
        Assert.Contains("Preview is truncated", summary.TruncationNotice);
        Assert.Equal("Not set", summary.OutputPathText);
    }

    [Fact]
    public void From_Should_Throw_When_Arguments_Are_Null()
    {
        MergeOutput output = CreateOutput();
        PreviewTextFormatResult previewText = CreatePreviewText();

        Assert.Throws<ArgumentNullException>(() =>
            PreviewGenerationSummaryViewModel.From(null!, [], [], [], previewText));

        Assert.Throws<ArgumentNullException>(() =>
            PreviewGenerationSummaryViewModel.From(output, null!, [], [], previewText));

        Assert.Throws<ArgumentNullException>(() =>
            PreviewGenerationSummaryViewModel.From(output, [], null!, [], previewText));

        Assert.Throws<ArgumentNullException>(() =>
            PreviewGenerationSummaryViewModel.From(output, [], [], null!, previewText));

        Assert.Throws<ArgumentNullException>(() =>
            PreviewGenerationSummaryViewModel.From(output, [], [], [], null!));
    }

    private static InputFile CreateFile(
        string relativePath,
        string extension = ".cs",
        FileKind kind = FileKind.CSharp,
        bool isIncluded = true,
        SkipReason? skipReason = null,
        bool isFallbackText = false,
        bool isMergeCandidate = true)
    {
        return new InputFile(
            fullPath: $@"D:\Project\{relativePath}",
            relativePath: relativePath,
            extension: extension,
            kind: kind,
            isIncluded: isIncluded,
            skipReason: skipReason,
            isFallbackText: isFallbackText,
            isMergeCandidate: isMergeCandidate);
    }

    private static MergeOutput CreateOutput()
    {
        return new MergeOutput(
            content: string.Empty,
            sections: [],
            statistics: new MergeStatistics(0, 0, 0, 0, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow);
    }

    private static PreviewTextFormatResult CreatePreviewText()
    {
        return new PreviewTextFormatResult(
            Text: string.Empty,
            TotalCharacters: 0,
            DisplayedCharacters: 0,
            WasTruncated: false,
            OmittedCharacters: 0);
    }
}