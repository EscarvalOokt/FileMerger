using System.Globalization;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Preview.ViewModels;

public sealed class PreviewGenerationSummaryViewModel : ViewModelBase
{
    private PreviewGenerationSummaryViewModel()
    {
        HasSummary = false;
        GeneratedAtText = "Not generated";
        OutputPathText = "Not set";
        DurationText = "—";
        TruncationNotice = string.Empty;
    }

    private PreviewGenerationSummaryViewModel(
        int filesDiscovered,
        int filesIncluded,
        int filesNotIncluded,
        int disabledFileTypeFiles,
        int unsupportedFiles,
        int profileExcludedFiles,
        int manuallyExcludedFiles,
        int otherNotIncludedFiles,
        int fallbackTextFiles,
        int sourceExcludedFiles,
        int totalCharacters,
        int displayedCharacters,
        bool wasTruncated,
        int omittedCharacters,
        TimeSpan duration,
        DateTime generatedAtUtc,
        string? outputPath)
    {
        HasSummary = true;
        FilesDiscovered = filesDiscovered;
        FilesIncluded = filesIncluded;
        FilesNotIncluded = filesNotIncluded;
        DisabledFileTypeFiles = disabledFileTypeFiles;
        UnsupportedFiles = unsupportedFiles;
        ProfileExcludedFiles = profileExcludedFiles;
        ManuallyExcludedFiles = manuallyExcludedFiles;
        OtherNotIncludedFiles = otherNotIncludedFiles;
        FallbackTextFiles = fallbackTextFiles;
        SourceExcludedFiles = sourceExcludedFiles;
        TotalCharacters = totalCharacters;
        DisplayedCharacters = displayedCharacters;
        WasTruncated = wasTruncated;
        OmittedCharacters = omittedCharacters;

        GeneratedAtText = generatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);

        string normalizedOutputPath = outputPath ?? string.Empty;

        OutputPathText = string.IsNullOrWhiteSpace(normalizedOutputPath) ? "Not set" : normalizedOutputPath;

        DurationText = FormatDuration(duration);

        TruncationNotice = wasTruncated
            ? $"Preview is truncated. Showing {displayedCharacters:N0} of {totalCharacters:N0} characters; {omittedCharacters:N0} character(s) omitted. Save still uses the full output."
            : string.Empty;
    }

    public static PreviewGenerationSummaryViewModel Empty { get; } = new();

    public bool HasSummary { get; }
    public bool HasNoSummary => !HasSummary;

    public int FilesDiscovered { get; }
    public int FilesIncluded { get; }
    public int FilesNotIncluded { get; }
    public int DisabledFileTypeFiles { get; }
    public int UnsupportedFiles { get; }
    public int ProfileExcludedFiles { get; }
    public int ManuallyExcludedFiles { get; }
    public int OtherNotIncludedFiles { get; }
    public int FallbackTextFiles { get; }
    public int SourceExcludedFiles { get; }
    public int TotalCharacters { get; }
    public int DisplayedCharacters { get; }
    public bool WasTruncated { get; }
    public int OmittedCharacters { get; }

    public bool HasDisabledFileTypeFiles => DisabledFileTypeFiles > 0;
    public bool HasUnsupportedFiles => UnsupportedFiles > 0;
    public bool HasProfileExcludedFiles => ProfileExcludedFiles > 0;
    public bool HasManuallyExcludedFiles => ManuallyExcludedFiles > 0;
    public bool HasOtherNotIncludedFiles => OtherNotIncludedFiles > 0;
    public bool HasFallbackTextFiles => FallbackTextFiles > 0;
    public bool HasSourceExcludedFiles => SourceExcludedFiles > 0;
    public bool HasTruncationNotice => !string.IsNullOrWhiteSpace(TruncationNotice);

    public string GeneratedAtText { get; }
    public string OutputPathText { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string DurationText { get; }
    public string TruncationNotice { get; }

    public string FilesDiscoveredText => FilesDiscovered.ToString("N0", CultureInfo.CurrentCulture);
    public string FilesIncludedText => FilesIncluded.ToString("N0", CultureInfo.CurrentCulture);
    public string FilesNotIncludedText => FilesNotIncluded.ToString("N0", CultureInfo.CurrentCulture);
    public string DisabledFileTypeFilesText => DisabledFileTypeFiles.ToString("N0", CultureInfo.CurrentCulture);
    public string UnsupportedFilesText => UnsupportedFiles.ToString("N0", CultureInfo.CurrentCulture);
    public string ProfileExcludedFilesText => ProfileExcludedFiles.ToString("N0", CultureInfo.CurrentCulture);
    public string ManuallyExcludedFilesText => ManuallyExcludedFiles.ToString("N0", CultureInfo.CurrentCulture);
    public string OtherNotIncludedFilesText => OtherNotIncludedFiles.ToString("N0", CultureInfo.CurrentCulture);
    public string FallbackTextFilesText => FallbackTextFiles.ToString("N0", CultureInfo.CurrentCulture);
    public string SourceExcludedFilesText => SourceExcludedFiles.ToString("N0", CultureInfo.CurrentCulture);
    public string TotalCharactersText => TotalCharacters.ToString("N0", CultureInfo.CurrentCulture);
    public string DisplayedCharactersText => DisplayedCharacters.ToString("N0", CultureInfo.CurrentCulture);

    public string CompactTooltip =>
        HasSummary ? BuildCompactTooltip() : "Preview generation summary is not available yet.";

    public static PreviewGenerationSummaryViewModel From(
        MergeOutput output,
        IReadOnlyCollection<InputFile> automaticFiles,
        IReadOnlyCollection<InputFile> currentFiles,
        IReadOnlyCollection<InputFile> sourceExcludedFiles,
        PreviewTextFormatResult previewText)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(automaticFiles);
        ArgumentNullException.ThrowIfNull(currentFiles);
        ArgumentNullException.ThrowIfNull(sourceExcludedFiles);
        ArgumentNullException.ThrowIfNull(previewText);

        var currentFilesByPath = currentFiles.GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        int filesIncluded = 0;
        int disabledFileTypeFiles = 0;
        int unsupportedFiles = 0;
        int profileExcludedFiles = 0;
        int manuallyExcludedFiles = 0;
        int otherNotIncludedFiles = 0;
        int fallbackTextFiles = 0;

        foreach (InputFile automaticFile in automaticFiles)
        {
            InputFile effectiveFile = currentFilesByPath.GetValueOrDefault(automaticFile.FullPath, automaticFile);

            if (effectiveFile.IsIncluded)
            {
                filesIncluded++;

                if (effectiveFile.IsFallbackText)
                    fallbackTextFiles++;

                continue;
            }

            switch (effectiveFile.SkipReason?.Category)
            {
                case SkippedFileCategory.DisabledFileType:
                    disabledFileTypeFiles++;
                    break;

                case SkippedFileCategory.UnsupportedFile:
                    unsupportedFiles++;
                    break;

                case SkippedFileCategory.ProfileExclusion:
                    profileExcludedFiles++;
                    break;

                case SkippedFileCategory.ManualExclusion:
                    manuallyExcludedFiles++;
                    break;

                default:
                    otherNotIncludedFiles++;
                    break;
            }
        }

        int filesDiscovered = automaticFiles.Count;
        int filesNotIncluded = filesDiscovered - filesIncluded;

        return new PreviewGenerationSummaryViewModel(
            filesDiscovered: filesDiscovered,
            filesIncluded: filesIncluded,
            filesNotIncluded: filesNotIncluded,
            disabledFileTypeFiles: disabledFileTypeFiles,
            unsupportedFiles: unsupportedFiles,
            profileExcludedFiles: profileExcludedFiles,
            manuallyExcludedFiles: manuallyExcludedFiles,
            otherNotIncludedFiles: otherNotIncludedFiles,
            fallbackTextFiles: fallbackTextFiles,
            sourceExcludedFiles: sourceExcludedFiles.Count,
            totalCharacters: previewText.TotalCharacters,
            displayedCharacters: previewText.DisplayedCharacters,
            wasTruncated: previewText.WasTruncated,
            omittedCharacters: previewText.OmittedCharacters,
            duration: output.Statistics.Duration,
            generatedAtUtc: output.GeneratedAtUtc,
            outputPath: output.OutputTarget?.Path);
    }

    private string BuildCompactTooltip()
    {
        string tooltip =
            $"Discovered: {FilesDiscovered:N0}, included: {FilesIncluded:N0}, not included: {FilesNotIncluded:N0}, characters: {TotalCharacters:N0}";

        return SourceExcludedFiles > 0 ? $"{tooltip}, source excluded: {SourceExcludedFiles:N0}" : tooltip;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:N0} ms";

        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:N1} sec";

        return $"{duration.TotalMinutes:N1} min";
    }
}