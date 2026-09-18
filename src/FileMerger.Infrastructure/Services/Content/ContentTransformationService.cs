using System.Text;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Infrastructure.Services.Content;

public sealed class ContentTransformationService : IContentTransformationService
{
    public string Transform(string content, InputFile file, MergeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(profile);

        string result = content;

        IEnumerable<ContentTransformationRule> orderedRules = profile.Transformations
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Order);

        foreach (ContentTransformationRule rule in orderedRules)
        {
            if (!AppliesToFile(rule, file))
                continue;

            result = ApplyTransformation(result, rule, profile);
        }

        if (profile.GeneralOptions.TrimTrailingEmptyLines)
        {
            result = result.TrimEnd('\r', '\n');
        }

        return result;
    }

    private static bool AppliesToFile(ContentTransformationRule rule, InputFile file)
    {
        return rule.AppliesTo.Count == 0 || rule.AppliesTo.Contains(file.Kind);
    }

    private static string ApplyTransformation(string content, ContentTransformationRule rule, MergeProfile profile)
    {
        return rule.Kind switch
        {
            TransformationKind.NormalizeLineEndings => NormalizeLineEndings(
                content,
                profile.GeneralOptions.LineEndingMode),

            TransformationKind.TrimTrailingEmptyLines => content.TrimEnd('\r', '\n'),

            TransformationKind.CollapseMultipleEmptyLines => CollapseMultipleEmptyLines(content),

            _ => content
        };
    }

    private static string NormalizeLineEndings(string content, LineEndingMode mode)
    {
        if (mode == LineEndingMode.Preserve)
            return content;

        string normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');

        return mode switch
        {
            LineEndingMode.CRLF => normalized.Replace("\n", "\r\n"),
            _ => normalized
        };
    }

    private static string CollapseMultipleEmptyLines(string content)
    {
        string normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        StringBuilder builder = new();

        bool previousWasEmpty = false;

        foreach (string line in normalized.Split('\n'))
        {
            bool currentIsEmpty = string.IsNullOrWhiteSpace(line);

            if (currentIsEmpty && previousWasEmpty)
                continue;

            builder.AppendLine(line);
            previousWasEmpty = currentIsEmpty;
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }
}