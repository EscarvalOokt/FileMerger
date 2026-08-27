using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileSummaryViewModel : ViewModelBase
{
    private string _fileTypes = "No file types enabled.";
    private string _formatting = string.Empty;
    private string _cSharpOptions = "No C# transformations enabled.";
    private string _filterRules = "No filter rules.";
    private string _encoding = string.Empty;
    private string _fileTypesHeadline = "0 enabled file types";
    private string _filterRulesHeadline = "No filter rules";
    private string _outputMetadata = string.Empty;
    private string _unsupportedTextFallback = "Disabled";

    public string FileTypes
    {
        get => _fileTypes;
        private set => SetProperty(ref _fileTypes, value);
    }

    public string Formatting
    {
        get => _formatting;
        private set => SetProperty(ref _formatting, value);
    }

    public string CSharpOptions
    {
        get => _cSharpOptions;
        private set => SetProperty(ref _cSharpOptions, value);
    }

    public string FilterRules
    {
        get => _filterRules;
        private set => SetProperty(ref _filterRules, value);
    }

    public string Encoding
    {
        get => _encoding;
        private set => SetProperty(ref _encoding, value);
    }

    public string FileTypesHeadline
    {
        get => _fileTypesHeadline;
        private set => SetProperty(ref _fileTypesHeadline, value);
    }

    public string FilterRulesHeadline
    {
        get => _filterRulesHeadline;
        private set => SetProperty(ref _filterRulesHeadline, value);
    }

    public string OutputMetadata
    {
        get => _outputMetadata;
        private set => SetProperty(ref _outputMetadata, value);
    }

    public string UnsupportedTextFallback
    {
        get => _unsupportedTextFallback;
        private set => SetProperty(ref _unsupportedTextFallback, value);
    }

    public void Apply(WorkspaceProfileDto profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        FileTypes = BuildFileTypesSummary(profile);
        Formatting = BuildFormattingSummary(profile);
        CSharpOptions = BuildCSharpSummary(profile);
        FilterRules = BuildFilterRulesSummary(profile);
        Encoding = BuildEncodingSummary(profile);

        FileTypesHeadline = BuildFileTypesHeadline(profile);
        FilterRulesHeadline = BuildFilterRulesHeadline(profile);
        OutputMetadata = BuildOutputMetadataSummary(profile);
        UnsupportedTextFallback = BuildUnsupportedTextFallbackSummary(profile);
    }

    private static string BuildFileTypesSummary(WorkspaceProfileDto profile)
    {
        string[] enabled =
        [
            .. profile.FileTypes
                .Where(x => x.IsEnabled)
                .Select(x => x.Extension)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        ];

        return enabled.Length == 0
            ? "No file types enabled."
            : string.Join(", ", enabled);
    }

    private static string BuildFormattingSummary(WorkspaceProfileDto profile)
    {
        List<string> parts =
        [
            $"Line endings: {profile.LineEndingMode}",
            $"Sort: {profile.SortMode}"
        ];

        if (profile.IncludeHeaderComment)
            parts.Add("Header comment");

        if (profile.IncludeFileSeparators)
        {
            parts.Add(profile.IncludeRelativePathInSeparator
                ? "Separators with relative path"
                : "Separators");
        }

        if (profile.TrimTrailingEmptyLines)
            parts.Add("Trim trailing empty lines");

        return string.Join(" · ", parts);
    }

    private static string BuildCSharpSummary(WorkspaceProfileDto profile)
    {
        bool showCSharpOptions =
            profile.FileTypes
                .Any(x => x is { IsEnabled: true, SupportsLanguageSpecificProcessing: true });

        if (!showCSharpOptions)
            return "C# transformations are not applicable for the currently enabled file types.";

        return profile.RemoveUsingDirectives
            ? "Remove using directives"
            : "No C# transformations enabled.";
    }

    private static string BuildFilterRulesSummary(WorkspaceProfileDto profile)
    {
        WorkspaceFileFilterRuleDto[] rules =
        [
            .. profile.FilterRules ?? []
        ];

        if (rules.Length == 0)
            return "No include/exclude filtering will be applied.";

        int enabled = rules.Count(x => x.IsEnabled);
        int disabled = rules.Length - enabled;

        if (disabled == 0)
            return "All configured rules are enabled.";

        return disabled switch
        {
            1 => "1 disabled rule will be ignored.",
            _ => $"{disabled} disabled rules will be ignored."
        };
    }

    private static string BuildEncodingSummary(WorkspaceProfileDto profile)
    {
        List<string> parts =
        [
            $"Mode: {profile.InputEncodingMode}"
        ];

        if (!string.IsNullOrWhiteSpace(profile.PreferredInputEncodingName))
            parts.Add($"Preferred: {profile.PreferredInputEncodingName}");

        if (!string.IsNullOrWhiteSpace(profile.FallbackInputEncodingName))
            parts.Add($"Fallback: {profile.FallbackInputEncodingName}");

        return string.Join(" · ", parts);
    }

    private static string BuildFileTypesHeadline(WorkspaceProfileDto profile)
    {
        int enabledCount = profile.FileTypes.Count(x => x.IsEnabled);

        return enabledCount switch
        {
            0 => "0 enabled file types",
            1 => "1 enabled file type",
            _ => $"{enabledCount} enabled file types"
        };
    }

    private static string BuildFilterRulesHeadline(WorkspaceProfileDto profile)
    {
        WorkspaceFileFilterRuleDto[] rules = [.. profile.FilterRules ?? []];

        if (rules.Length == 0)
            return "No filter rules";

        int enabled = rules.Count(x => x.IsEnabled);
        return $"{enabled} enabled / {rules.Length} total";
    }

    private static string BuildOutputMetadataSummary(WorkspaceProfileDto profile)
    {
        List<string> parts = [];

        if (profile.IncludeBuildTimestampMetadata)
            parts.Add("Build timestamp");

        if (profile.IncludeSessionNameMetadata)
            parts.Add("Session name");

        if (profile.IncludeOutputPathMetadata)
            parts.Add("Output path");

        if (profile.IncludeFileSummaryMetadata)
            parts.Add("File summary");

        parts.Add($"Skipped files: {profile.SkippedFilesMetadataMode}");

        SkippedFileCategorySelection selection =
            profile.SkippedFileCategories ??
            SkippedFileCategorySelection.ForCurrentBehavior(profile.IncludeSourceExcludedFiles);

        string[] selectedCategories =
        [
            .. BuildSkippedCategoryLabels(selection)
        ];

        parts.Add(selectedCategories.Length == 0
            ? "Skipped categories: none"
            : $"Skipped categories: {string.Join(", ", selectedCategories)}");

        return parts.Count == 0
            ? "No output metadata."
            : string.Join(" · ", parts);
    }


    private static IEnumerable<string> BuildSkippedCategoryLabels(
        SkippedFileCategorySelection selection)
    {
        if (selection.IncludeDisabledFileTypes)
            yield return "Disabled file types";

        if (selection.IncludeUnsupportedFiles)
            yield return "Unsupported files";

        if (selection.IncludeProfileExclusions)
            yield return "Profile exclusions";

        if (selection.IncludeManualExclusions)
            yield return "Manual exclusions";

        if (selection.IncludeSourceExclusions)
            yield return "Source exclusions";

        if (selection.IncludeProcessingFailures)
            yield return "Processing failures";

        if (selection.IncludeOther)
            yield return "Other";
    }

    private static string BuildUnsupportedTextFallbackSummary(WorkspaceProfileDto profile)
    {
        if (!profile.IncludeUnsupportedTextFiles)
            return "Disabled";

        return
            $"Enabled · Max size: {profile.UnsupportedTextMaxFileSizeBytes} bytes · " +
            $"Probe: {profile.UnsupportedTextProbeSizeBytes} bytes · " +
            $"Max control chars: {profile.UnsupportedTextMaxControlCharacterRatio:P0}";
    }
}