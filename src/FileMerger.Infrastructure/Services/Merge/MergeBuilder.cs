using System.Text;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Infrastructure.Services.Merge;

public sealed class MergeBuilder : IMergeBuilder
{
    public MergeOutput Build(
        MergeSession session,
        IReadOnlyCollection<MergeSection> sections,
        TimeSpan totalDuration,
        IReadOnlyCollection<InputFile>? sourceExcludedFiles = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(sections);

        sourceExcludedFiles ??= [];

        DateTime generatedAtUtc = DateTime.UtcNow;
        StringBuilder builder = new();

        if (session.Profile.GeneralOptions.IncludeHeaderComment)
        {
            AppendHeader(builder, session, sourceExcludedFiles, generatedAtUtc);
        }

        MergeSection[] orderedSections = [.. sections.OrderBy(x => x.Order)];

        for (int i = 0; i < orderedSections.Length; i++)
        {
            MergeSection section = orderedSections[i];

            if (!string.IsNullOrWhiteSpace(section.HeaderText))
            {
                builder.AppendLine("// -----------------------------------------------");
                builder.AppendLine(section.HeaderText);
                builder.AppendLine("// -----------------------------------------------");
                builder.AppendLine();
            }

            builder.Append(section.Content);

            if (i < orderedSections.Length - 1)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
        }

        string mergedContent = builder.ToString();

        MergeStatistics statistics = new(
            filesScanned: session.Files.Count,
            filesIncluded: session.Files.Count(x => x.IsIncluded),
            filesSkipped: session.Files.Count(x => !x.IsIncluded),
            totalCharacters: mergedContent.Length,
            duration: totalDuration);

        return new MergeOutput(
            content: mergedContent,
            sections: orderedSections,
            statistics: statistics,
            generatedAtUtc: generatedAtUtc,
            outputTarget: session.OutputTarget);
    }

    private static void AppendHeader(
        StringBuilder builder,
        MergeSession session,
        IReadOnlyCollection<InputFile> sourceExcludedFiles,
        DateTime generatedAtUtc)
    {
        OutputMetadataOptions metadataOptions = session.Profile.GeneralOptions.OutputMetadataOptions;

        builder.AppendLine("// ===============================================");
        builder.AppendLine("// Auto-generated merged source file");

        if (metadataOptions.IncludeBuildTimestamp)
            builder.AppendLine($"// Date (UTC): {generatedAtUtc:yyyy-MM-dd HH:mm:ss}");

        if (metadataOptions.IncludeSessionName)
            builder.AppendLine($"// Session: {session.Name}");

        if (metadataOptions.IncludeOutputPath)
            builder.AppendLine($"// Output: {session.OutputTarget.Path}");

        if (metadataOptions.IncludeFileSummary)
        {
            builder.AppendLine($"// Files included: {session.Files.Count(x => x.IsIncluded)}");
            builder.AppendLine($"// Files skipped: {session.Files.Count(x => !x.IsIncluded)}");
        }

        AppendSkippedFilesMetadata(builder, session, sourceExcludedFiles, metadataOptions);

        builder.AppendLine("// ===============================================");
        builder.AppendLine();
    }

    private static void AppendSkippedFilesMetadata(
        StringBuilder builder,
        MergeSession session,
        IReadOnlyCollection<InputFile> sourceExcludedFiles,
        OutputMetadataOptions metadataOptions)
    {
        SkippedFilesMetadataMode mode = metadataOptions.SkippedFilesMetadataMode;

        if (mode == SkippedFilesMetadataMode.None)
            return;

        SkippedFileCategorySelection categorySelection = metadataOptions.EffectiveSkippedFileCategories;

        IEnumerable<InputFile> skippedFileCandidates =
            session.Files.Where(x => !x.IsIncluded && ShouldIncludeSkippedFile(x, categorySelection));

        if (categorySelection.Includes(SkippedFileCategory.SourceExclusion))
        {
            skippedFileCandidates = skippedFileCandidates.Concat(sourceExcludedFiles);
        }

        InputFile[] skippedFiles =
        [
            .. skippedFileCandidates.GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
        ];

        if (skippedFiles.Length == 0)
        {
            builder.AppendLine("// Skipped files: none");
            return;
        }

        builder.AppendLine("// Skipped files:");

        foreach (InputFile file in skippedFiles)
        {
            if (mode == SkippedFilesMetadataMode.Simple)
            {
                builder.AppendLine($"// - {file.RelativePath}");
                continue;
            }

            AppendDetailedSkippedFile(builder, file);
        }
    }

    private static bool ShouldIncludeSkippedFile(InputFile file, SkippedFileCategorySelection categorySelection)
    {
        SkippedFileCategory category = file.SkipReason?.Category ?? SkippedFileCategory.Other;

        return categorySelection.Includes(category);
    }

    private static void AppendDetailedSkippedFile(StringBuilder builder, InputFile file)
    {
        builder.AppendLine($"// - {file.RelativePath}");

        if (file.SkipReason is null)
        {
            builder.AppendLine("//   Reason: unknown — No skip reason available.");
            return;
        }

        builder.AppendLine($"//   Reason: {file.SkipReason.Code} — {file.SkipReason.Description}");

        if (file.SkipReason.RuleDetails is not null)
        {
            builder.AppendLine($"//   Rule: {FormatRuleDetails(file.SkipReason.RuleDetails)}");
        }
    }

    private static string FormatRuleDetails(SkipRuleDetails ruleDetails)
    {
        string mode = ruleDetails.Mode.ToString().ToLowerInvariant();

        return $"{mode} {ruleDetails.Target} {ruleDetails.PatternType} \"{ruleDetails.Pattern}\"";
    }
}