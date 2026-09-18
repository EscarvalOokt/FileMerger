using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Features.Profile.Services;

public sealed class BuiltInProfilePresetProvider : IBuiltInProfilePresetProvider
{
    private readonly IFileTypeCatalog _fileTypeCatalog;

    public BuiltInProfilePresetProvider(IFileTypeCatalog fileTypeCatalog)
    {
        ArgumentNullException.ThrowIfNull(fileTypeCatalog);
        _fileTypeCatalog = fileTypeCatalog;
    }

    public IReadOnlyCollection<ProfileLibraryEntry> GetAll()
    {
        return
        [
            CreateBuiltInEntry(
                id: "builtin.default",
                name: "Default",
                description: "Balanced default profile for everyday source merging.",
                profile: BuildDefaultProfile()),

            CreateBuiltInEntry(
                id: "builtin.csharp-minimal",
                name: "C# Minimal",
                description: "Only C# source files, with common generated/build artifacts excluded.",
                profile: BuildCSharpMinimalProfile()),

            CreateBuiltInEntry(
                id: "builtin.docs-config",
                name: "Docs and Config",
                description: "Documentation, text and configuration-oriented files.",
                profile: BuildDocsAndConfigProfile()),

            CreateBuiltInEntry(
                id: "builtin.javascript-project",
                name: "JavaScript Project",
                description:
                "JavaScript/Node project files with dependency folders, caches and build outputs excluded.",
                profile: BuildJavaScriptProjectProfile()),

            CreateBuiltInEntry(
                id: "builtin.typescript-project",
                name: "TypeScript Project",
                description: "TypeScript project files with dependency folders, caches and build outputs excluded.",
                profile: BuildTypeScriptProjectProfile()),

            CreateBuiltInEntry(
                id: "builtin.wpf-app",
                name: "WPF Application",
                description: "WPF/.NET application files with common generated and build artifacts excluded.",
                profile: BuildWpfApplicationProfile()),

            CreateBuiltInEntry(
                id: "builtin.unity-project",
                name: "Unity Project",
                description:
                "Unity C#, project settings and serialized asset files. Use source exclusions for Library, Temp and build output folders.",
                profile: BuildUnityProjectProfile()),

            CreateBuiltInEntry(
                id: "builtin.full-source-dump",
                name: "Full Source Dump",
                description: "All supported file types with minimal filtering.",
                profile: BuildFullSourceDumpProfile())
        ];
    }

    private static ProfileLibraryEntry CreateBuiltInEntry(
        string id,
        string name,
        string description,
        WorkspaceProfileDto profile)
    {
        ProfileMetadataDto metadata = new(
            Id: id,
            Name: name,
            Description: description,
            CreatedAtUtc: null,
            UpdatedAtUtc: null,
            IsBuiltIn: true,
            IsReadOnly: true);

        return new ProfileLibraryEntry(
            Metadata: metadata,
            Profile: profile,
            FilePath: null,
            Kind: ProfileEntryKind.BuiltIn);
    }

    private WorkspaceProfileDto BuildDefaultProfile()
    {
        return BuildProfile(
            enabledExtensions: _fileTypeCatalog.GetDefault().Select(x => x.Extension),
            includeHeaderComment: false,
            includeFileSeparators: true,
            includeRelativePathInSeparator: true,
            trimTrailingEmptyLines: true,
            lineEndingMode: LineEndingMode.Preserve,
            sortMode: SortMode.ByRelativePathAscending,
            inputEncodingMode: InputEncodingMode.Auto,
            preferredInputEncodingName: null,
            fallbackInputEncodingName: "windows-1251",
            filterRules: BuildCSharpFilterRules());
    }

    private WorkspaceProfileDto BuildCSharpMinimalProfile()
    {
        return BuildProfile(
            enabledExtensions: [".cs"],
            includeHeaderComment: false,
            includeFileSeparators: true,
            includeRelativePathInSeparator: true,
            trimTrailingEmptyLines: true,
            lineEndingMode: LineEndingMode.Preserve,
            sortMode: SortMode.ByRelativePathAscending,
            inputEncodingMode: InputEncodingMode.Auto,
            preferredInputEncodingName: null,
            fallbackInputEncodingName: "windows-1251",
            filterRules: BuildCSharpFilterRules());
    }

    private WorkspaceProfileDto BuildDocsAndConfigProfile()
    {
        return BuildProfile(
            enabledExtensions:
            [
                ".txt",
                ".md",
                ".json",
                ".xml",
                ".yml",
                ".yaml",
                ".toml",
                ".ini",
                ".cfg",
                ".conf",
                ".config",
                KnownFileTypes.EditorConfig.Extension,
                ".props",
                ".targets",
                ".csproj",
                KnownFileTypes.SolutionXml.Extension
            ],
            includeHeaderComment: false,
            includeFileSeparators: true,
            includeRelativePathInSeparator: true,
            trimTrailingEmptyLines: true,
            lineEndingMode: LineEndingMode.Preserve,
            sortMode: SortMode.ByRelativePathAscending,
            inputEncodingMode: InputEncodingMode.Auto,
            preferredInputEncodingName: null,
            fallbackInputEncodingName: "windows-1251");
    }

    private WorkspaceProfileDto BuildJavaScriptProjectProfile()
    {
        return BuildProfile(
            enabledExtensions:
            [
                KnownFileTypes.JavaScript.Extension,
                KnownFileTypes.JavaScriptReact.Extension,
                KnownFileTypes.JavaScriptModule.Extension,
                KnownFileTypes.JavaScriptCommonJs.Extension,
                KnownFileTypes.Json.Extension,
                KnownFileTypes.Jsonc.Extension,
                KnownFileTypes.Markdown.Extension,
                KnownFileTypes.Yaml.Extension,
                KnownFileTypes.YamlAlt.Extension,
                KnownFileTypes.Css.Extension,
                KnownFileTypes.Scss.Extension,
                KnownFileTypes.Sass.Extension,
                KnownFileTypes.Less.Extension,
                KnownFileTypes.Html.Extension,
                KnownFileTypes.HtmlAlt.Extension,
                KnownFileTypes.GraphQl.Extension,
                KnownFileTypes.GraphQlAlt.Extension,
                KnownFileTypes.Lock.Extension
            ],
            includeHeaderComment: false,
            includeFileSeparators: true,
            includeRelativePathInSeparator: true,
            trimTrailingEmptyLines: true,
            lineEndingMode: LineEndingMode.Preserve,
            sortMode: SortMode.ByRelativePathAscending,
            inputEncodingMode: InputEncodingMode.Auto,
            preferredInputEncodingName: null,
            fallbackInputEncodingName: "windows-1251",
            filterRules: BuildJavaScriptProjectFilterRules());
    }

    private WorkspaceProfileDto BuildTypeScriptProjectProfile()
    {
        return BuildProfile(
            enabledExtensions:
            [
                KnownFileTypes.TypeScript.Extension,
                KnownFileTypes.TypeScriptReact.Extension,
                KnownFileTypes.TypeScriptModule.Extension,
                KnownFileTypes.TypeScriptCommonJs.Extension,
                KnownFileTypes.JavaScript.Extension,
                KnownFileTypes.JavaScriptReact.Extension,
                KnownFileTypes.JavaScriptModule.Extension,
                KnownFileTypes.JavaScriptCommonJs.Extension,
                KnownFileTypes.Json.Extension,
                KnownFileTypes.Jsonc.Extension,
                KnownFileTypes.Markdown.Extension,
                KnownFileTypes.Yaml.Extension,
                KnownFileTypes.YamlAlt.Extension,
                KnownFileTypes.Css.Extension,
                KnownFileTypes.Scss.Extension,
                KnownFileTypes.Sass.Extension,
                KnownFileTypes.Less.Extension,
                KnownFileTypes.Html.Extension,
                KnownFileTypes.HtmlAlt.Extension,
                KnownFileTypes.GraphQl.Extension,
                KnownFileTypes.GraphQlAlt.Extension,
                KnownFileTypes.Lock.Extension
            ],
            includeHeaderComment: false,
            includeFileSeparators: true,
            includeRelativePathInSeparator: true,
            trimTrailingEmptyLines: true,
            lineEndingMode: LineEndingMode.Preserve,
            sortMode: SortMode.ByRelativePathAscending,
            inputEncodingMode: InputEncodingMode.Auto,
            preferredInputEncodingName: null,
            fallbackInputEncodingName: "windows-1251",
            filterRules: BuildJavaScriptProjectFilterRules());
    }

    private WorkspaceProfileDto BuildWpfApplicationProfile()
    {
        return BuildProfile(
            enabledExtensions: KnownFileTypes.WpfApplication.Select(x => x.Extension),
            includeHeaderComment: false,
            includeFileSeparators: true,
            includeRelativePathInSeparator: true,
            trimTrailingEmptyLines: true,
            lineEndingMode: LineEndingMode.Preserve,
            sortMode: SortMode.ByRelativePathAscending,
            inputEncodingMode: InputEncodingMode.Auto,
            preferredInputEncodingName: null,
            fallbackInputEncodingName: "windows-1251",
            filterRules: CombineFilterRules(BuildCSharpFilterRules(), BuildWpfApplicationFilterRules()));
    }

    private WorkspaceProfileDto BuildUnityProjectProfile()
    {
        return BuildProfile(
            enabledExtensions: KnownFileTypes.UnityProject.Select(x => x.Extension),
            includeHeaderComment: false,
            includeFileSeparators: true,
            includeRelativePathInSeparator: true,
            trimTrailingEmptyLines: true,
            lineEndingMode: LineEndingMode.Preserve,
            sortMode: SortMode.ByRelativePathAscending,
            inputEncodingMode: InputEncodingMode.Auto,
            preferredInputEncodingName: null,
            fallbackInputEncodingName: "windows-1251",
            filterRules: CombineFilterRules(BuildCSharpFilterRules(), BuildUnityProjectFilterRules()));
    }

    private WorkspaceProfileDto BuildFullSourceDumpProfile()
    {
        return BuildProfile(
            enabledExtensions: _fileTypeCatalog.GetAll().Select(x => x.Extension),
            includeHeaderComment: false,
            includeFileSeparators: true,
            includeRelativePathInSeparator: true,
            trimTrailingEmptyLines: true,
            lineEndingMode: LineEndingMode.Preserve,
            sortMode: SortMode.ByRelativePathAscending,
            inputEncodingMode: InputEncodingMode.Auto,
            preferredInputEncodingName: null,
            fallbackInputEncodingName: "windows-1251");
    }

    private WorkspaceProfileDto BuildProfile(
        IEnumerable<string> enabledExtensions,
        bool includeHeaderComment,
        bool includeFileSeparators,
        bool includeRelativePathInSeparator,
        bool trimTrailingEmptyLines,
        LineEndingMode lineEndingMode,
        SortMode sortMode,
        InputEncodingMode inputEncodingMode,
        string? preferredInputEncodingName,
        string? fallbackInputEncodingName,
        IEnumerable<WorkspaceFileFilterRuleDto>? filterRules = null)
    {
        HashSet<string> enabled = new(enabledExtensions, StringComparer.OrdinalIgnoreCase);

        List<WorkspaceFileTypeDto> fileTypes =
        [
            .. _fileTypeCatalog.GetAll()
                .Select(x => new WorkspaceFileTypeDto(
                    Extension: x.Extension,
                    DisplayName: x.DisplayName,
                    Kind: x.Kind,
                    IsEnabled: enabled.Contains(x.Extension),
                    SupportsLanguageSpecificProcessing: x.SupportsLanguageSpecificProcessing))
        ];

        List<WorkspaceFileFilterRuleDto> normalizedFilterRules = filterRules is null ? [] : [.. filterRules];

        return new WorkspaceProfileDto(
            IncludeHeaderComment: includeHeaderComment,
            IncludeFileSeparators: includeFileSeparators,
            IncludeRelativePathInSeparator: includeRelativePathInSeparator,
            TrimTrailingEmptyLines: trimTrailingEmptyLines,
            FileTypes: fileTypes,
            LineEndingMode: lineEndingMode,
            SortMode: sortMode,
            InputEncodingMode: inputEncodingMode,
            PreferredInputEncodingName: preferredInputEncodingName,
            FallbackInputEncodingName: fallbackInputEncodingName,
            FilterRules: normalizedFilterRules);
    }

    private static IReadOnlyCollection<WorkspaceFileFilterRuleDto> BuildCSharpFilterRules()
    {
        return
        [
            ExcludeDirectory("bin", "Exclude bin directory"),
            ExcludeDirectory("obj", "Exclude obj directory"),
            ExcludeFileNameWildcard("*.Designer.cs", "Exclude designer files"),
            ExcludeFileNameExact("AssemblyInfo.cs", "Exclude AssemblyInfo")
        ];
    }

    private static IReadOnlyCollection<WorkspaceFileFilterRuleDto> BuildJavaScriptProjectFilterRules()
    {
        return
        [
            ExcludeDirectory("node_modules", "Exclude node_modules directory"),
            ExcludeDirectory("dist", "Exclude dist directory"),
            ExcludeDirectory("build", "Exclude build directory"),
            ExcludeDirectory("coverage", "Exclude coverage directory"),
            ExcludeDirectory(".cache", "Exclude cache directory"),
            ExcludeDirectory(".parcel-cache", "Exclude Parcel cache directory"),
            ExcludeDirectory(".turbo", "Exclude Turborepo cache directory"),
            ExcludeDirectory(".next", "Exclude Next.js build directory"),
            ExcludeDirectory(".nuxt", "Exclude Nuxt build directory"),
            ExcludeDirectory(".svelte-kit", "Exclude SvelteKit build directory"),
            ExcludeDirectory(".vite", "Exclude Vite cache directory"),
            ExcludeDirectory(".vercel", "Exclude Vercel metadata directory"),
            ExcludeDirectory(".netlify", "Exclude Netlify metadata directory"),
            ExcludeDirectory(".git", "Exclude Git directory"),
            ExcludeDirectory(".idea", "Exclude JetBrains IDE directory"),
            ExcludeDirectory(".vs", "Exclude Visual Studio directory"),
            ExcludeFileNameWildcard("*.map", "Exclude generated source map files")
        ];
    }

    private static IReadOnlyCollection<WorkspaceFileFilterRuleDto> BuildWpfApplicationFilterRules()
    {
        return
        [
            ExcludeFileNameWildcard("*.g.cs", "Exclude generated WPF .g.cs files"),
            ExcludeFileNameWildcard("*.g.i.cs", "Exclude generated WPF .g.i.cs files"),
            ExcludeFileNameWildcard("*.AssemblyAttributes.cs", "Exclude generated assembly attributes files")
        ];
    }

    private static IReadOnlyCollection<WorkspaceFileFilterRuleDto> BuildUnityProjectFilterRules()
    {
        return
        [
            ExcludeDirectory("Library", "Exclude Unity Library directory"),
            ExcludeDirectory("Temp", "Exclude Unity Temp directory"),
            ExcludeDirectory("Logs", "Exclude Unity Logs directory"),
            ExcludeDirectory("Build", "Exclude Unity Build directory"),
            ExcludeDirectory("Builds", "Exclude Unity Builds directory"),
            ExcludeDirectory("UserSettings", "Exclude Unity UserSettings directory"),
            ExcludeDirectory("MemoryCaptures", "Exclude Unity MemoryCaptures directory"),
            ExcludeDirectory("Recordings", "Exclude Unity Recordings directory"),
            ExcludeDirectory(".vs", "Exclude Visual Studio directory"),
            ExcludeDirectory(".idea", "Exclude JetBrains IDE directory"),
            ExcludeDirectory(".git", "Exclude Git directory")
        ];
    }

    private static List<WorkspaceFileFilterRuleDto> CombineFilterRules(
        params IEnumerable<WorkspaceFileFilterRuleDto>[] groups)
    {
        List<WorkspaceFileFilterRuleDto> result = [];

        foreach (WorkspaceFileFilterRuleDto rule in groups.SelectMany(x => x))
        {
            if (result.Any(x => HasSameRuleIdentity(x, rule)))
                continue;

            result.Add(rule);
        }

        return result;
    }

    private static bool HasSameRuleIdentity(WorkspaceFileFilterRuleDto left, WorkspaceFileFilterRuleDto right)
    {
        return left.Mode == right.Mode &&
               left.Target == right.Target &&
               left.PatternType == right.PatternType &&
               left.IsEnabled == right.IsEnabled &&
               string.Equals(left.Pattern, right.Pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static WorkspaceFileFilterRuleDto ExcludeDirectory(string pattern, string description)
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: pattern,
            IsEnabled: true,
            Description: description,
            IsUserEditable: true);
    }

    private static WorkspaceFileFilterRuleDto ExcludeFileNameExact(string pattern, string description)
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.FileName,
            PatternType: RulePatternType.Exact,
            Pattern: pattern,
            IsEnabled: true,
            Description: description,
            IsUserEditable: true);
    }

    private static WorkspaceFileFilterRuleDto ExcludeFileNameWildcard(string pattern, string description)
    {
        return new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.FileName,
            PatternType: RulePatternType.Wildcard,
            Pattern: pattern,
            IsEnabled: true,
            Description: description,
            IsUserEditable: true);
    }
}