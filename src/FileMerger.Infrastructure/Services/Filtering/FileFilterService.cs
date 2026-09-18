using System.Text.RegularExpressions;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Infrastructure.Services.Filtering;

public sealed class FileFilterService : IFileFilterService
{
    private static readonly TimeSpan _regexMatchTimeout = TimeSpan.FromMilliseconds(250);

    public IReadOnlyCollection<InputFile> ApplyFilters(IReadOnlyCollection<InputFile> files, MergeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(profile);

        List<InputFile> result = [];

        foreach (InputFile file in files)
        {
            InputFile current = file;

            foreach (FileFilterRule rule in profile.FilterRules.Where(x => x.IsEnabled))
            {
                if (!IsMatch(current, rule))
                    continue;

                if (rule.Mode == FilterMode.Exclude)
                {
                    current = current.Exclude(CreateFilterRuleSkipReason(rule));
                    break;
                }

                if (rule.Mode == FilterMode.Include)
                {
                    current = current.Include();
                }
            }

            result.Add(current);
        }

        return ApplySorting(result, profile.GeneralOptions.SortMode);
    }

    private static InputFile[] ApplySorting(IEnumerable<InputFile> files, SortMode sortMode)
    {
        return sortMode switch
        {
            SortMode.ByRelativePathAscending =>
                [.. files.OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)],

            SortMode.ByRelativePathDescending =>
                [.. files.OrderByDescending(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)],

            _ => [.. files]
        };
    }

    private static bool IsMatch(InputFile file, FileFilterRule rule)
    {
        if (rule.Target == FilterTarget.DirectorySegment)
            return MatchDirectorySegment(file.FullPath, rule);

        string candidate = rule.Target switch
        {
            FilterTarget.FileName => Path.GetFileName(file.FullPath),
            FilterTarget.Extension => file.Extension,
            FilterTarget.RelativePath => file.RelativePath,
            _ => string.Empty
        };

        return MatchValue(candidate, rule.Pattern, rule.PatternType);
    }

    private static bool MatchDirectorySegment(string fullPath, FileFilterRule rule)
    {
        string? directoryPath = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;

        string[] segments = directoryPath.Replace('/', '\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(segment => MatchValue(segment, rule.Pattern, rule.PatternType));
    }

    private static bool MatchValue(string value, string pattern, RulePatternType patternType)
    {
        return patternType switch
        {
            RulePatternType.Exact => string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase),

            RulePatternType.Contains => value.Contains(pattern, StringComparison.OrdinalIgnoreCase),

            RulePatternType.Wildcard => Regex.IsMatch(
                value,
                WildcardToRegex(pattern),
                RegexOptions.IgnoreCase,
                _regexMatchTimeout),

            RulePatternType.Regex => Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase, _regexMatchTimeout),

            _ => false
        };
    }

    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
    }

    private static SkipReason CreateFilterRuleSkipReason(FileFilterRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new SkipReason(
            code: "filter.rule.exclude",
            description: rule.Description ?? $"Excluded by rule: {rule.Pattern}",
            ruleDetails: new SkipRuleDetails(
                mode: rule.Mode,
                target: rule.Target,
                patternType: rule.PatternType,
                pattern: rule.Pattern,
                description: rule.Description));
    }
}