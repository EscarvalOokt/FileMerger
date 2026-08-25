using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileEditorViewModel : ViewModelBase
{
    private const string SourceCodeGroupKey = "source-code";
    private const string WebAndStylesGroupKey = "web-and-styles";
    private const string DataAndConfigurationGroupKey = "data-and-configuration";
    private const string DotNetGroupKey = "dotnet";
    private const string UnityGroupKey = "unity";
    private const string DocumentsGroupKey = "documents";
    private const string OtherGroupKey = "other";

    private static readonly IReadOnlyList<FileTypeGroupDefinition> FileTypeGroupDefinitions =
    [
        new(
            SourceCodeGroupKey,
            "Source code",
            "Programming language source files."),

        new(
            WebAndStylesGroupKey,
            "Web and styles",
            "Web markup, stylesheets, and GraphQL files."),

        new(
            DataAndConfigurationGroupKey,
            "Data and configuration",
            "Structured data, configuration, lock, and environment-style text files."),

        new(
            DotNetGroupKey,
            ".NET / WPF / MSBuild",
            ".NET project, solution, WPF, MSBuild, resource, and publish files."),

        new(
            UnityGroupKey,
            "Unity project files",
            "Unity scenes, assets, metadata, shaders, input actions, and editor-related files."),

        new(
            DocumentsGroupKey,
            "Documents and plain text",
            "Plain text and Markdown documentation files."),

        new(
            OtherGroupKey,
            "Other / custom",
            "File types that are not part of the built-in grouping rules.")
    ];

    private static readonly HashSet<string> SourceCodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".js",
        ".jsx",
        ".mjs",
        ".cjs",
        ".ts",
        ".tsx",
        ".mts",
        ".cts"
    };

    private static readonly HashSet<string> WebAndStylesExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html",
        ".htm",
        ".css",
        ".scss",
        ".sass",
        ".less",
        ".graphql",
        ".gql"
    };

    private static readonly HashSet<string> DataAndConfigurationExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xml",
        ".json",
        ".jsonc",
        ".yml",
        ".yaml",
        ".toml",
        ".ini",
        ".cfg",
        ".conf",
        ".config",
        ".editorconfig",
        ".lock"
    };

    private static readonly HashSet<string> DotNetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xaml",
        ".csproj",
        ".sln",
        ".slnx",
        ".props",
        ".targets",
        ".resx",
        ".settings",
        ".manifest",
        ".pubxml",
        ".ruleset"
    };

    private static readonly HashSet<string> UnityExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".asmdef",
        ".asmref",
        ".unity",
        ".prefab",
        ".asset",
        ".meta",
        ".mat",
        ".controller",
        ".anim",
        ".overrideController",
        ".playable",
        ".physicMaterial",
        ".physicsMaterial2D",
        ".spriteatlas",
        ".inputactions",
        ".shader",
        ".compute",
        ".hlsl",
        ".cginc",
        ".uxml",
        ".uss",
        ".shadergraph",
        ".vfx"
    };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md"
    };

    private readonly IFileTypeCatalog _fileTypeCatalog;
    private readonly ObservableCollection<FileTypeOptionViewModel> _fileTypes = [];
    private readonly ObservableCollection<ProfileFileTypeGroupViewModel> _fileTypeGroups = [];
    private readonly ObservableCollection<ProfileFilterRuleItemViewModel> _filterRules = [];
    private readonly ObservableCollection<ProfileFilterRuleItemViewModel> _visibleFilterRules = [];

    private bool _hasVisibleFileTypeGroups;
    private bool _hasFileTypeFilterEmptyState;
    private bool _hasVisibleFilterRules;
    private bool _hasFilterRuleFilterEmptyState;
    private string _fileTypeFilterSummary = "0 of 0 file types shown";
    private string _filterRuleFilterSummary = "0 of 0 rules shown";

    private bool _isResettingFileTypes;
    private ProfileFilterRuleItemViewModel? _selectedFilterRule;
    private ProfileEditorSection _selectedSection = ProfileEditorSection.General;
    private string _workingProfileName = "Default";

    private bool _includeHeaderComment;
    private bool _includeFileSeparators = true;
    private bool _includeRelativePathInSeparator = true;
    private bool _trimTrailingEmptyLines = true;

    private bool _removeUsingDirectives;

    private LineEndingMode _lineEndingMode = LineEndingMode.Preserve;
    private SortMode _sortMode = SortMode.ByRelativePathAscending;

    private InputEncodingMode _inputEncodingMode = InputEncodingMode.Auto;
    private string? _preferredInputEncodingName;
    private string? _fallbackInputEncodingName = "windows-1251";

    private bool _includeUnsupportedTextFiles;
    private long _unsupportedTextMaxFileSizeBytes = UnsupportedTextFallbackOptions.DefaultMaxFileSizeBytes;
    private int _unsupportedTextProbeSizeBytes = UnsupportedTextFallbackOptions.DefaultProbeSizeBytes;
    private double _unsupportedTextMaxControlCharacterRatio = UnsupportedTextFallbackOptions.DefaultMaxControlCharacterRatio;

    private bool _includeBuildTimestampMetadata = true;
    private bool _includeSessionNameMetadata = true;
    private bool _includeOutputPathMetadata = true;
    private bool _includeFileSummaryMetadata = true;
    private bool _includeSourceExcludedFiles;
    private SkippedFilesMetadataMode _skippedFilesMetadataMode;

    public ProfileEditorViewModel(IFileTypeCatalog fileTypeCatalog)
    {
        ArgumentNullException.ThrowIfNull(fileTypeCatalog);

        _fileTypeCatalog = fileTypeCatalog;

        FileTypeFilters = new ProfileFileTypeFiltersViewModel();
        FilterRuleFilters = new ProfileFilterRuleFiltersViewModel();

        FileTypeFilters.PropertyChanged += FileTypeFilters_PropertyChanged;
        FilterRuleFilters.PropertyChanged += FilterRuleFilters_PropertyChanged;

        _fileTypes.CollectionChanged += FileTypes_CollectionChanged;
        _filterRules.CollectionChanged += FilterRules_CollectionChanged;

        FileTypeFilterModeOptions =
        [
            new EnumOptionViewModel<ProfileFileTypeFilterMode>(ProfileFileTypeFilterMode.All, "All"),
            new EnumOptionViewModel<ProfileFileTypeFilterMode>(ProfileFileTypeFilterMode.Enabled, "Enabled"),
            new EnumOptionViewModel<ProfileFileTypeFilterMode>(ProfileFileTypeFilterMode.Disabled, "Disabled")
        ];

        FilterRuleStatusFilterOptions =
        [
            new EnumOptionViewModel<ProfileFilterRuleStatusFilterMode>(ProfileFilterRuleStatusFilterMode.All, "All"),
            new EnumOptionViewModel<ProfileFilterRuleStatusFilterMode>(ProfileFilterRuleStatusFilterMode.Enabled, "Enabled"),
            new EnumOptionViewModel<ProfileFilterRuleStatusFilterMode>(ProfileFilterRuleStatusFilterMode.Disabled, "Disabled"),
            new EnumOptionViewModel<ProfileFilterRuleStatusFilterMode>(ProfileFilterRuleStatusFilterMode.Invalid, "Invalid")
        ];

        FilterModeOptions =
        [
            new EnumOptionViewModel<FilterMode>(FilterMode.Include, "Include"),
            new EnumOptionViewModel<FilterMode>(FilterMode.Exclude, "Exclude")
        ];

        FilterTargetOptions =
        [
            new EnumOptionViewModel<FilterTarget>(FilterTarget.FileName, "File name"),
            new EnumOptionViewModel<FilterTarget>(FilterTarget.Extension, "Extension"),
            new EnumOptionViewModel<FilterTarget>(FilterTarget.RelativePath, "Relative path"),
            new EnumOptionViewModel<FilterTarget>(FilterTarget.DirectorySegment, "Directory segment")
        ];

        RulePatternTypeOptions =
        [
            new EnumOptionViewModel<RulePatternType>(RulePatternType.Exact, "Exact"),
            new EnumOptionViewModel<RulePatternType>(RulePatternType.Contains, "Contains"),
            new EnumOptionViewModel<RulePatternType>(RulePatternType.Wildcard, "Wildcard"),
            new EnumOptionViewModel<RulePatternType>(RulePatternType.Regex, "Regex")
        ];

        SelectSectionCommand = new RelayCommand<ProfileEditorSection>(SelectSection, CanSelectSection);

        ClearFileTypeFiltersCommand = new RelayCommand(ClearFileTypeFilters, () => FileTypeFilters.HasActiveFilters);
        ClearFilterRuleFiltersCommand = new RelayCommand(ClearFilterRuleFilters, () => FilterRuleFilters.HasActiveFilters);

        AddFilterRuleCommand = new RelayCommand(AddFilterRule);
        DuplicateFilterRuleCommand = new RelayCommand(DuplicateFilterRule, () => CanDuplicateFilterRule);
        RemoveFilterRuleCommand = new RelayCommand(RemoveSelectedFilterRule, () => CanRemoveFilterRule);
        MoveFilterRuleUpCommand = new RelayCommand(MoveSelectedFilterRuleUp, () => CanMoveFilterRuleUp);
        MoveFilterRuleDownCommand = new RelayCommand(MoveSelectedFilterRuleDown, () => CanMoveFilterRuleDown);
        ClearFilterRulesCommand = new RelayCommand(ClearFilterRules, () => CanClearFilterRules);

        ResetFileTypes(_fileTypeCatalog.GetDefault());
    }

    public string WorkingProfileName
    {
        get => _workingProfileName;
        set => SetProperty(ref _workingProfileName, NormalizeProfileName(value));
    }

    public bool IncludeHeaderComment
    {
        get => _includeHeaderComment;
        set => SetProperty(ref _includeHeaderComment, value);
    }

    public bool IncludeFileSeparators
    {
        get => _includeFileSeparators;
        set => SetProperty(ref _includeFileSeparators, value);
    }

    public bool IncludeRelativePathInSeparator
    {
        get => _includeRelativePathInSeparator;
        set => SetProperty(ref _includeRelativePathInSeparator, value);
    }

    public bool TrimTrailingEmptyLines
    {
        get => _trimTrailingEmptyLines;
        set => SetProperty(ref _trimTrailingEmptyLines, value);
    }

    public bool RemoveUsingDirectives
    {
        get => _removeUsingDirectives;
        set => SetProperty(ref _removeUsingDirectives, value);
    }

    public ObservableCollection<FileTypeOptionViewModel> FileTypes => _fileTypes;

    public ObservableCollection<ProfileFileTypeGroupViewModel> FileTypeGroups => _fileTypeGroups;

    public ObservableCollection<ProfileFilterRuleItemViewModel> FilterRules => _filterRules;

    public ObservableCollection<ProfileFilterRuleItemViewModel> VisibleFilterRules => _visibleFilterRules;

    public ProfileFileTypeFiltersViewModel FileTypeFilters { get; }

    public ProfileFilterRuleFiltersViewModel FilterRuleFilters { get; }

    public IReadOnlyCollection<EnumOptionViewModel<ProfileFileTypeFilterMode>> FileTypeFilterModeOptions { get; }

    public IReadOnlyCollection<EnumOptionViewModel<ProfileFilterRuleStatusFilterMode>> FilterRuleStatusFilterOptions { get; }

    public bool HasVisibleFileTypeGroups
    {
        get => _hasVisibleFileTypeGroups;
        private set => SetProperty(ref _hasVisibleFileTypeGroups, value);
    }

    public bool HasFileTypeFilterEmptyState
    {
        get => _hasFileTypeFilterEmptyState;
        private set => SetProperty(ref _hasFileTypeFilterEmptyState, value);
    }

    public bool HasVisibleFilterRules
    {
        get => _hasVisibleFilterRules;
        private set => SetProperty(ref _hasVisibleFilterRules, value);
    }

    public bool HasFilterRuleFilterEmptyState
    {
        get => _hasFilterRuleFilterEmptyState;
        private set => SetProperty(ref _hasFilterRuleFilterEmptyState, value);
    }

    public string FileTypeFilterSummary
    {
        get => _fileTypeFilterSummary;
        private set => SetProperty(ref _fileTypeFilterSummary, value);
    }

    public string FilterRuleFilterSummary
    {
        get => _filterRuleFilterSummary;
        private set => SetProperty(ref _filterRuleFilterSummary, value);
    }

    public ProfileFilterRuleItemViewModel? SelectedFilterRule
    {
        get => _selectedFilterRule;
        set
        {
            if (SetProperty(ref _selectedFilterRule, value))
            {
                OnPropertyChanged(nameof(HasSelectedFilterRule));
                OnPropertyChanged(nameof(CanDuplicateFilterRule));
                OnPropertyChanged(nameof(CanRemoveFilterRule));
                OnPropertyChanged(nameof(CanMoveFilterRuleUp));
                OnPropertyChanged(nameof(CanMoveFilterRuleDown));
                RaiseFilterRuleCommandStates();
            }
        }
    }

    public IReadOnlyCollection<EnumOptionViewModel<FilterMode>> FilterModeOptions { get; }

    public IReadOnlyCollection<EnumOptionViewModel<FilterTarget>> FilterTargetOptions { get; }

    public IReadOnlyCollection<EnumOptionViewModel<RulePatternType>> RulePatternTypeOptions { get; }

    public ProfileEditorSection SelectedSection
    {
        get => _selectedSection;
        private set
        {
            if (!SetProperty(ref _selectedSection, value))
                return;

            RaiseSectionSelectionProperties();
            SelectSectionCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsGeneralSectionSelected => SelectedSection == ProfileEditorSection.General;

    public bool IsFormattingSectionSelected => SelectedSection == ProfileEditorSection.Formatting;

    public bool IsOutputMetadataSectionSelected => SelectedSection == ProfileEditorSection.OutputMetadata;

    public bool IsInputEncodingSectionSelected => SelectedSection == ProfileEditorSection.InputEncoding;

    public bool IsUnsupportedTextFallbackSectionSelected =>
        SelectedSection == ProfileEditorSection.UnsupportedTextFallback;

    public bool IsFileTypesSectionSelected => SelectedSection == ProfileEditorSection.FileTypes;

    public bool IsCSharpTransformationsSectionSelected =>
        SelectedSection == ProfileEditorSection.CSharpTransformations;

    public bool IsFilterRulesSectionSelected => SelectedSection == ProfileEditorSection.FilterRules;

    public bool ShowCSharpOptions => FileTypes.Any(x => x.Kind == FileKind.CSharp && x.IsEnabled);

    public bool HasFilterRules => FilterRules.Count > 0;

    public bool HasSelectedFilterRule => SelectedFilterRule is not null;

    public bool HasInvalidFilterRules => FilterRules.Any(x => x.HasValidationError);

    public bool CanUseProfile => !HasInvalidFilterRules;

    public bool CanDuplicateFilterRule => SelectedFilterRule is not null;

    public bool CanRemoveFilterRule => SelectedFilterRule is not null && SelectedFilterRule.IsUserEditable;

    public bool CanMoveFilterRuleUp =>
        SelectedFilterRule is not null &&
        FilterRules.IndexOf(SelectedFilterRule) > 0;

    public bool CanMoveFilterRuleDown =>
        SelectedFilterRule is not null &&
        FilterRules.IndexOf(SelectedFilterRule) >= 0 &&
        FilterRules.IndexOf(SelectedFilterRule) < FilterRules.Count - 1;

    public bool CanClearFilterRules => FilterRules.Any(x => x.IsUserEditable);

    public LineEndingMode LineEndingMode
    {
        get => _lineEndingMode;
        set => SetProperty(ref _lineEndingMode, value);
    }

    public SortMode SortMode
    {
        get => _sortMode;
        set => SetProperty(ref _sortMode, value);
    }

    public InputEncodingMode InputEncodingMode
    {
        get => _inputEncodingMode;
        set => SetProperty(ref _inputEncodingMode, value);
    }

    public string? PreferredInputEncodingName
    {
        get => _preferredInputEncodingName;
        set => SetProperty(ref _preferredInputEncodingName, value);
    }

    public string? FallbackInputEncodingName
    {
        get => _fallbackInputEncodingName;
        set => SetProperty(ref _fallbackInputEncodingName, value);
    }

    public bool IncludeUnsupportedTextFiles
    {
        get => _includeUnsupportedTextFiles;
        set => SetProperty(ref _includeUnsupportedTextFiles, value);
    }

    public long UnsupportedTextMaxFileSizeBytes
    {
        get => _unsupportedTextMaxFileSizeBytes;
        set => SetProperty(ref _unsupportedTextMaxFileSizeBytes, Math.Max(0, value));
    }

    public int UnsupportedTextProbeSizeBytes
    {
        get => _unsupportedTextProbeSizeBytes;
        set => SetProperty(ref _unsupportedTextProbeSizeBytes, Math.Max(1, value));
    }

    public double UnsupportedTextMaxControlCharacterRatio
    {
        get => _unsupportedTextMaxControlCharacterRatio;
        set => SetProperty(ref _unsupportedTextMaxControlCharacterRatio, Math.Clamp(value, 0, 1));
    }

    public IReadOnlyCollection<SkippedFilesMetadataMode> SkippedFilesMetadataModes { get; } =
    [
        SkippedFilesMetadataMode.None,
        SkippedFilesMetadataMode.Simple,
        SkippedFilesMetadataMode.Detailed
    ];

    public bool IncludeBuildTimestampMetadata
    {
        get => _includeBuildTimestampMetadata;
        set => SetProperty(ref _includeBuildTimestampMetadata, value);
    }

    public bool IncludeSessionNameMetadata
    {
        get => _includeSessionNameMetadata;
        set => SetProperty(ref _includeSessionNameMetadata, value);
    }

    public bool IncludeOutputPathMetadata
    {
        get => _includeOutputPathMetadata;
        set => SetProperty(ref _includeOutputPathMetadata, value);
    }

    public bool IncludeFileSummaryMetadata
    {
        get => _includeFileSummaryMetadata;
        set => SetProperty(ref _includeFileSummaryMetadata, value);
    }

    public bool IncludeSourceExcludedFiles
    {
        get => _includeSourceExcludedFiles;
        set => SetProperty(ref _includeSourceExcludedFiles, value);
    }

    public SkippedFilesMetadataMode SkippedFilesMetadataMode
    {
        get => _skippedFilesMetadataMode;
        set => SetProperty(ref _skippedFilesMetadataMode, value);
    }

    public RelayCommand<ProfileEditorSection> SelectSectionCommand { get; }

    public RelayCommand ClearFileTypeFiltersCommand { get; }

    public RelayCommand ClearFilterRuleFiltersCommand { get; }

    public RelayCommand AddFilterRuleCommand { get; }

    public RelayCommand DuplicateFilterRuleCommand { get; }

    public RelayCommand RemoveFilterRuleCommand { get; }

    public RelayCommand MoveFilterRuleUpCommand { get; }

    public RelayCommand MoveFilterRuleDownCommand { get; }

    public RelayCommand ClearFilterRulesCommand { get; }

    public void LoadDefaults()
    {
        MergeProfile defaultProfile = DefaultMergeProfiles.CreateDefault();

        WorkingProfileName = NormalizeProfileName(defaultProfile.Name);
        ApplyProfile(ToWorkspaceProfileDto(defaultProfile));
    }

    public WorkspaceProfileDto CaptureProfile()
    {
        return new WorkspaceProfileDto(
            IncludeHeaderComment,
            IncludeFileSeparators,
            IncludeRelativePathInSeparator,
            TrimTrailingEmptyLines,
            RemoveUsingDirectives,
            [
                .. FileTypes.Select(x => new WorkspaceFileTypeDto(
                    x.Extension,
                    x.DisplayName,
                    x.Kind,
                    x.IsEnabled,
                    x.SupportsLanguageSpecificProcessing))
            ],
            LineEndingMode,
            SortMode,
            InputEncodingMode,
            PreferredInputEncodingName,
            FallbackInputEncodingName,
            FilterRules:
            [
                .. FilterRules.Select(x => x.ToDto())
            ],
            IncludeUnsupportedTextFiles: IncludeUnsupportedTextFiles,
            UnsupportedTextMaxFileSizeBytes: UnsupportedTextMaxFileSizeBytes,
            UnsupportedTextProbeSizeBytes: UnsupportedTextProbeSizeBytes,
            UnsupportedTextMaxControlCharacterRatio: UnsupportedTextMaxControlCharacterRatio,
            IncludeBuildTimestampMetadata: IncludeBuildTimestampMetadata,
            IncludeSessionNameMetadata: IncludeSessionNameMetadata,
            IncludeOutputPathMetadata: IncludeOutputPathMetadata,
            IncludeFileSummaryMetadata: IncludeFileSummaryMetadata,
            SkippedFilesMetadataMode: SkippedFilesMetadataMode,
            IncludeSourceExcludedFiles: IncludeSourceExcludedFiles);
    }

    public void ApplyProfile(WorkspaceProfileDto profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ResetEditorFilters();

        IncludeHeaderComment = profile.IncludeHeaderComment;
        IncludeFileSeparators = profile.IncludeFileSeparators;
        IncludeRelativePathInSeparator = profile.IncludeRelativePathInSeparator;
        TrimTrailingEmptyLines = profile.TrimTrailingEmptyLines;

        RemoveUsingDirectives = profile.RemoveUsingDirectives;

        IReadOnlyCollection<FileTypeDefinition> fileTypes =
            BuildAvailableFileTypes(profile.FileTypes);

        ResetFileTypes(fileTypes);
        ResetFilterRules(profile.FilterRules);

        LineEndingMode = profile.LineEndingMode;
        SortMode = profile.SortMode;

        InputEncodingMode = profile.InputEncodingMode;
        PreferredInputEncodingName = profile.PreferredInputEncodingName;
        FallbackInputEncodingName = profile.FallbackInputEncodingName;

        IncludeUnsupportedTextFiles = profile.IncludeUnsupportedTextFiles;
        UnsupportedTextMaxFileSizeBytes = profile.UnsupportedTextMaxFileSizeBytes;
        UnsupportedTextProbeSizeBytes = profile.UnsupportedTextProbeSizeBytes;
        UnsupportedTextMaxControlCharacterRatio = profile.UnsupportedTextMaxControlCharacterRatio;

        IncludeBuildTimestampMetadata = profile.IncludeBuildTimestampMetadata;
        IncludeSessionNameMetadata = profile.IncludeSessionNameMetadata;
        IncludeOutputPathMetadata = profile.IncludeOutputPathMetadata;
        IncludeFileSummaryMetadata = profile.IncludeFileSummaryMetadata;
        IncludeSourceExcludedFiles = profile.IncludeSourceExcludedFiles;
        SkippedFilesMetadataMode = profile.SkippedFilesMetadataMode;
    }

    public MergeProfile BuildProfile()
    {
        return new MergeProfile(
            name: NormalizeProfileName(WorkingProfileName),
            generalOptions: new GeneralMergeOptions(
                includeHeaderComment: IncludeHeaderComment,
                includeFileSeparators: IncludeFileSeparators,
                includeRelativePathInSeparator: IncludeRelativePathInSeparator,
                trimTrailingEmptyLines: TrimTrailingEmptyLines,
                lineEndingMode: LineEndingMode,
                sortMode: SortMode,
                inputEncodingMode: InputEncodingMode,
                preferredInputEncodingName: PreferredInputEncodingName,
                fallbackInputEncodingName: FallbackInputEncodingName,
                unsupportedTextFallbackOptions: new UnsupportedTextFallbackOptions(
                    isEnabled: IncludeUnsupportedTextFiles,
                    maxFileSizeBytes: UnsupportedTextMaxFileSizeBytes,
                    probeSizeBytes: UnsupportedTextProbeSizeBytes,
                    maxControlCharacterRatio: UnsupportedTextMaxControlCharacterRatio),
                outputMetadataOptions: new OutputMetadataOptions(
                    IncludeBuildTimestamp: IncludeBuildTimestampMetadata,
                    IncludeSessionName: IncludeSessionNameMetadata,
                    IncludeOutputPath: IncludeOutputPathMetadata,
                    IncludeFileSummary: IncludeFileSummaryMetadata,
                    SkippedFilesMetadataMode: SkippedFilesMetadataMode,
                    IncludeSourceExcludedFiles: IncludeSourceExcludedFiles)),
            csOptions: new CsMergeOptions(
                RemoveUsingDirectives: RemoveUsingDirectives),
            fileTypes: BuildFileTypes(),
            filterRules: BuildFilterRules(),
            transformations: BuildTransformations());
    }

    public PreviewProfileStateSnapshot BuildPreviewProfileSnapshot()
    {
        return new PreviewProfileStateSnapshot(
            IncludeHeaderComment,
            IncludeFileSeparators,
            IncludeRelativePathInSeparator,
            TrimTrailingEmptyLines,
            RemoveUsingDirectives,
            LineEndingMode,
            SortMode,
            InputEncodingMode,
            PreferredInputEncodingName,
            FallbackInputEncodingName,
            [
                .. FileTypes
                    .Select(x => new PreviewFileTypeStateSnapshot(x.Extension, x.IsEnabled))
                    .OrderBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
            ],
            [
                .. FilterRules
                    .Select(ToPreviewFileFilterRuleStateSnapshot)
                    .OrderBy(x => x.Mode)
                    .ThenBy(x => x.Target)
                    .ThenBy(x => x.PatternType)
                    .ThenBy(x => x.Pattern, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Description, StringComparer.Ordinal)
                    .ThenBy(x => x.IsEnabled)
            ],
            includeUnsupportedTextFiles: IncludeUnsupportedTextFiles,
            unsupportedTextMaxFileSizeBytes: UnsupportedTextMaxFileSizeBytes,
            unsupportedTextProbeSizeBytes: UnsupportedTextProbeSizeBytes,
            unsupportedTextMaxControlCharacterRatio: UnsupportedTextMaxControlCharacterRatio,
            includeBuildTimestampMetadata: IncludeBuildTimestampMetadata,
            includeSessionNameMetadata: IncludeSessionNameMetadata,
            includeOutputPathMetadata: IncludeOutputPathMetadata,
            includeFileSummaryMetadata: IncludeFileSummaryMetadata,
            skippedFilesMetadataMode: SkippedFilesMetadataMode,
            includeSourceExcludedFiles: IncludeSourceExcludedFiles);
    }

    private void SelectSection(ProfileEditorSection section)
    {
        if (!CanSelectSection(section))
            return;

        SelectedSection = section;
    }

    private bool CanSelectSection(ProfileEditorSection section)
    {
        return section != ProfileEditorSection.CSharpTransformations || ShowCSharpOptions;
    }

    private void RefreshCSharpSectionAvailability()
    {
        OnPropertyChanged(nameof(ShowCSharpOptions));
        SelectSectionCommand.RaiseCanExecuteChanged();

        if (!ShowCSharpOptions && SelectedSection == ProfileEditorSection.CSharpTransformations)
            SelectedSection = ProfileEditorSection.FileTypes;
    }

    private void RaiseSectionSelectionProperties()
    {
        OnPropertyChanged(nameof(IsGeneralSectionSelected));
        OnPropertyChanged(nameof(IsFormattingSectionSelected));
        OnPropertyChanged(nameof(IsOutputMetadataSectionSelected));
        OnPropertyChanged(nameof(IsInputEncodingSectionSelected));
        OnPropertyChanged(nameof(IsUnsupportedTextFallbackSectionSelected));
        OnPropertyChanged(nameof(IsFileTypesSectionSelected));
        OnPropertyChanged(nameof(IsCSharpTransformationsSectionSelected));
        OnPropertyChanged(nameof(IsFilterRulesSectionSelected));
    }

    private IReadOnlyCollection<FileTypeDefinition> BuildFileTypes()
    {
        return
        [
            .. FileTypes
                .Select(ToFileTypeDefinition)
                .OrderBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private List<FileFilterRule> BuildFilterRules()
    {
        List<FileFilterRule> rules = [];

        foreach (ProfileFilterRuleItemViewModel rule in FilterRules.Where(x => !x.HasValidationError))
            AddFilterRuleIfMissing(rules, ToFileFilterRule(rule));

        return rules;
    }

    private List<ContentTransformationRule> BuildTransformations()
    {
        return
        [
            new ContentTransformationRule(
                kind: TransformationKind.RemoveUsingDirectives,
                order: 0,
                isEnabled: RemoveUsingDirectives,
                appliesTo: [FileKind.CSharp]),

            new ContentTransformationRule(
                kind: TransformationKind.TrimTrailingEmptyLines,
                order: 1,
                isEnabled: TrimTrailingEmptyLines),

            new ContentTransformationRule(
                kind: TransformationKind.NormalizeLineEndings,
                order: 2,
                isEnabled: LineEndingMode != LineEndingMode.Preserve)
        ];
    }

    private void AddFilterRule()
    {
        ProfileFilterRuleItemViewModel item = new(new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: "Library",
            IsEnabled: true,
            Description: "Exclude directory",
            IsUserEditable: true));

        FilterRules.Add(item);
        SelectFilterRuleAfterMutation(item);
    }

    private void DuplicateFilterRule()
    {
        if (SelectedFilterRule is null)
            return;

        ProfileFilterRuleItemViewModel duplicate = SelectedFilterRule.Clone();
        int selectedIndex = FilterRules.IndexOf(SelectedFilterRule);
        int insertIndex = selectedIndex < 0
            ? FilterRules.Count
            : selectedIndex + 1;

        FilterRules.Insert(insertIndex, duplicate);
        SelectFilterRuleAfterMutation(duplicate);
    }

    private void RemoveSelectedFilterRule()
    {
        if (SelectedFilterRule is null || !SelectedFilterRule.IsUserEditable)
            return;

        int oldIndex = FilterRules.IndexOf(SelectedFilterRule);
        if (oldIndex < 0)
            return;

        FilterRules.RemoveAt(oldIndex);

        if (FilterRules.Count == 0)
        {
            SelectedFilterRule = null;
            return;
        }

        int newIndex = Math.Min(oldIndex, FilterRules.Count - 1);
        SelectFilterRuleAfterMutation(FilterRules[newIndex]);
    }

    private void ClearFilterRules()
    {
        ProfileFilterRuleItemViewModel[] editableRules =
        [
            .. FilterRules.Where(x => x.IsUserEditable)
        ];

        foreach (ProfileFilterRuleItemViewModel rule in editableRules)
            FilterRules.Remove(rule);

        if (SelectedFilterRule is not null && !FilterRules.Contains(SelectedFilterRule))
            SelectFilterRuleAfterMutation(VisibleFilterRules.FirstOrDefault());
    }

    private void SelectFilterRuleAfterMutation(ProfileFilterRuleItemViewModel? preferredRule)
    {
        if (preferredRule is not null && VisibleFilterRules.Contains(preferredRule))
        {
            SelectedFilterRule = preferredRule;
            return;
        }

        SelectedFilterRule = VisibleFilterRules.FirstOrDefault();
    }

    private void MoveSelectedFilterRuleUp()
    {
        if (SelectedFilterRule is null)
            return;

        int index = FilterRules.IndexOf(SelectedFilterRule);
        if (index <= 0)
            return;

        FilterRules.Move(index, index - 1);
    }

    private void MoveSelectedFilterRuleDown()
    {
        if (SelectedFilterRule is null)
            return;

        int index = FilterRules.IndexOf(SelectedFilterRule);
        if (index < 0 || index >= FilterRules.Count - 1)
            return;

        FilterRules.Move(index, index + 1);
    }

    private IReadOnlyCollection<FileTypeDefinition> BuildAvailableFileTypes(
        IReadOnlyCollection<WorkspaceFileTypeDto>? profileFileTypes)
    {
        if (profileFileTypes is not { Count: > 0 })
            return _fileTypeCatalog.GetDefault();

        var profileFileTypeByExtension =
            profileFileTypes
                .Where(x => !string.IsNullOrWhiteSpace(x.Extension))
                .GroupBy(x => x.Extension.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => x.First(),
                    StringComparer.OrdinalIgnoreCase);

        List<FileTypeDefinition> mergedFileTypes = [];
        HashSet<string> catalogExtensions = new(StringComparer.OrdinalIgnoreCase);

        foreach (FileTypeDefinition catalogFileType in _fileTypeCatalog.GetAll())
        {
            catalogExtensions.Add(catalogFileType.Extension);

            bool isEnabled =
                profileFileTypeByExtension.TryGetValue(catalogFileType.Extension, out WorkspaceFileTypeDto? profileFileType) &&
                profileFileType.IsEnabled;

            mergedFileTypes.Add(new FileTypeDefinition(
                extension: catalogFileType.Extension,
                displayName: catalogFileType.DisplayName,
                kind: catalogFileType.Kind,
                isEnabled: isEnabled,
                supportsLanguageSpecificProcessing: catalogFileType.SupportsLanguageSpecificProcessing));
        }

        foreach (WorkspaceFileTypeDto profileFileType in profileFileTypes)
        {
            if (string.IsNullOrWhiteSpace(profileFileType.Extension))
                continue;

            if (catalogExtensions.Contains(profileFileType.Extension))
                continue;

            mergedFileTypes.Add(ToFileTypeDefinition(profileFileType));
        }

        return mergedFileTypes;
    }

    private void ResetFileTypes(IReadOnlyCollection<FileTypeDefinition> fileTypes)
    {
        ArgumentNullException.ThrowIfNull(fileTypes);

        _isResettingFileTypes = true;

        try
        {
            foreach (FileTypeOptionViewModel item in _fileTypes)
                item.PropertyChanged -= FileType_PropertyChanged;

            _fileTypes.Clear();

            foreach (FileTypeDefinition fileType in fileTypes
                         .OrderBy(x => x.Extension, StringComparer.OrdinalIgnoreCase))
            {
                FileTypeOptionViewModel item = new(
                    extension: fileType.Extension,
                    displayName: fileType.DisplayName,
                    kind: fileType.Kind,
                    isEnabled: fileType.IsEnabled,
                    supportsLanguageSpecificProcessing: fileType.SupportsLanguageSpecificProcessing);

                _fileTypes.Add(item);
            }
        }
        finally
        {
            _isResettingFileTypes = false;
        }

        RebuildFileTypeGroups();
        RefreshCSharpSectionAvailability();
    }

    private void RebuildFileTypeGroups()
    {
        ClearFileTypeGroups();

        var fileTypesByGroup =
            FileTypes
                .GroupBy(GetFileTypeGroupKey)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList(),
                    StringComparer.OrdinalIgnoreCase);

        foreach (FileTypeGroupDefinition definition in FileTypeGroupDefinitions)
        {
            if (!fileTypesByGroup.TryGetValue(definition.Key, out List<FileTypeOptionViewModel>? groupFileTypes))
                continue;

            if (groupFileTypes.Count == 0)
                continue;

            bool isExpanded = groupFileTypes.Any(x => x.IsEnabled);

            _fileTypeGroups.Add(new ProfileFileTypeGroupViewModel(
                title: definition.Title,
                description: definition.Description,
                fileTypes: groupFileTypes,
                isExpanded: isExpanded));
        }

        RefreshVisibleFileTypes();
        OnPropertyChanged(nameof(FileTypeGroups));
    }

    private void ClearFileTypeGroups()
    {
        foreach (ProfileFileTypeGroupViewModel group in _fileTypeGroups)
            group.Detach();

        _fileTypeGroups.Clear();
    }

    private void ClearFileTypeFilters()
    {
        FileTypeFilters.Reset();
        ClearFileTypeFiltersCommand.RaiseCanExecuteChanged();
    }

    private void ResetEditorFilters()
    {
        FileTypeFilters.Reset();
        FilterRuleFilters.Reset();

        ClearFileTypeFiltersCommand.RaiseCanExecuteChanged();
        ClearFilterRuleFiltersCommand.RaiseCanExecuteChanged();
    }

    private void RefreshVisibleFileTypes()
    {
        int visibleCount = 0;

        foreach (ProfileFileTypeGroupViewModel group in FileTypeGroups)
        {
            group.ApplyVisibleFileTypes(fileType => FileTypeFilters.Matches(fileType, group));

            if (FileTypeFilters.HasActiveFilters && group.HasVisibleFileTypes)
                group.IsExpanded = true;

            visibleCount += group.VisibleCount;
        }

        int totalCount = FileTypes.Count;

        HasVisibleFileTypeGroups = FileTypeGroups.Any(x => x.HasVisibleFileTypes);
        HasFileTypeFilterEmptyState = totalCount > 0 && visibleCount == 0;
        FileTypeFilterSummary = $"{visibleCount} of {totalCount} file types shown";

        ClearFileTypeFiltersCommand.RaiseCanExecuteChanged();
    }

    private static string GetFileTypeGroupKey(FileTypeOptionViewModel fileType)
    {
        string extension = fileType.Extension.Trim();

        if (SourceCodeExtensions.Contains(extension))
            return SourceCodeGroupKey;

        if (WebAndStylesExtensions.Contains(extension))
            return WebAndStylesGroupKey;

        if (DataAndConfigurationExtensions.Contains(extension))
            return DataAndConfigurationGroupKey;

        if (DotNetExtensions.Contains(extension))
            return DotNetGroupKey;

        if (UnityExtensions.Contains(extension))
            return UnityGroupKey;

        if (DocumentExtensions.Contains(extension))
            return DocumentsGroupKey;

        return OtherGroupKey;
    }

    private void ResetFilterRules(IEnumerable<WorkspaceFileFilterRuleDto>? filterRules)
    {
        foreach (ProfileFilterRuleItemViewModel item in _filterRules)
            item.PropertyChanged -= FilterRule_PropertyChanged;

        _filterRules.Clear();

        foreach (WorkspaceFileFilterRuleDto rule in filterRules ?? [])
            _filterRules.Add(ProfileFilterRuleItemViewModel.FromDto(rule));

        RefreshVisibleFilterRules();
        SelectedFilterRule = VisibleFilterRules.FirstOrDefault();

        RaiseFilterRuleStateProperties();
        RaiseFilterRuleCommandStates();
    }

    private void ClearFilterRuleFilters()
    {
        FilterRuleFilters.Reset();
        ClearFilterRuleFiltersCommand.RaiseCanExecuteChanged();
    }

    private void RefreshVisibleFilterRules()
    {
        _visibleFilterRules.Clear();

        foreach (ProfileFilterRuleItemViewModel rule in FilterRules.Where(FilterRuleFilters.Matches))
            _visibleFilterRules.Add(rule);

        HasVisibleFilterRules = VisibleFilterRules.Count > 0;
        HasFilterRuleFilterEmptyState = HasFilterRules && !HasVisibleFilterRules;
        FilterRuleFilterSummary = $"{VisibleFilterRules.Count} of {FilterRules.Count} rules shown";

        if (SelectedFilterRule is not null && !VisibleFilterRules.Contains(SelectedFilterRule))
            SelectedFilterRule = VisibleFilterRules.FirstOrDefault();

        OnPropertyChanged(nameof(VisibleFilterRules));
        ClearFilterRuleFiltersCommand.RaiseCanExecuteChanged();
    }

    private static FileTypeDefinition ToFileTypeDefinition(WorkspaceFileTypeDto dto)
    {
        return new FileTypeDefinition(
            extension: dto.Extension,
            displayName: dto.DisplayName,
            kind: dto.Kind,
            isEnabled: dto.IsEnabled,
            supportsLanguageSpecificProcessing: dto.SupportsLanguageSpecificProcessing);
    }

    private static FileTypeDefinition ToFileTypeDefinition(FileTypeOptionViewModel vm)
    {
        return new FileTypeDefinition(
            extension: vm.Extension,
            displayName: vm.DisplayName,
            kind: vm.Kind,
            isEnabled: vm.IsEnabled,
            supportsLanguageSpecificProcessing: vm.SupportsLanguageSpecificProcessing);
    }

    private static FileFilterRule ToFileFilterRule(ProfileFilterRuleItemViewModel item)
    {
        return new FileFilterRule(
            mode: item.Mode,
            target: item.Target,
            patternType: item.PatternType,
            pattern: item.Pattern.Trim(),
            isEnabled: item.IsEnabled,
            description: NormalizeDescription(item.Description));
    }

    private static PreviewFileFilterRuleStateSnapshot ToPreviewFileFilterRuleStateSnapshot(
        ProfileFilterRuleItemViewModel item)
    {
        return new PreviewFileFilterRuleStateSnapshot(
            Mode: item.Mode,
            Target: item.Target,
            PatternType: item.PatternType,
            Pattern: item.Pattern.Trim(),
            IsEnabled: item.IsEnabled,
            Description: NormalizeDescription(item.Description));
    }

    private static WorkspaceProfileDto ToWorkspaceProfileDto(MergeProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new WorkspaceProfileDto(
            profile.GeneralOptions.IncludeHeaderComment,
            profile.GeneralOptions.IncludeFileSeparators,
            profile.GeneralOptions.IncludeRelativePathInSeparator,
            profile.GeneralOptions.TrimTrailingEmptyLines,
            profile.CsOptions.RemoveUsingDirectives,
            [
                .. profile.FileTypes.Select(x => new WorkspaceFileTypeDto(
                    x.Extension,
                    x.DisplayName,
                    x.Kind,
                    x.IsEnabled,
                    x.SupportsLanguageSpecificProcessing))
            ],
            profile.GeneralOptions.LineEndingMode,
            profile.GeneralOptions.SortMode,
            profile.GeneralOptions.InputEncodingMode,
            profile.GeneralOptions.PreferredInputEncodingName,
            profile.GeneralOptions.FallbackInputEncodingName,
            FilterRules:
            [
                .. profile.FilterRules.Select(x => new WorkspaceFileFilterRuleDto(
                    Mode: x.Mode,
                    Target: x.Target,
                    PatternType: x.PatternType,
                    Pattern: x.Pattern,
                    IsEnabled: x.IsEnabled,
                    Description: x.Description,
                    IsUserEditable: true))
            ],
            IncludeUnsupportedTextFiles: profile.GeneralOptions.UnsupportedTextFallbackOptions.IsEnabled,
            UnsupportedTextMaxFileSizeBytes: profile.GeneralOptions.UnsupportedTextFallbackOptions.MaxFileSizeBytes,
            UnsupportedTextProbeSizeBytes: profile.GeneralOptions.UnsupportedTextFallbackOptions.ProbeSizeBytes,
            UnsupportedTextMaxControlCharacterRatio: profile.GeneralOptions.UnsupportedTextFallbackOptions.MaxControlCharacterRatio,
            IncludeBuildTimestampMetadata: profile.GeneralOptions.OutputMetadataOptions.IncludeBuildTimestamp,
            IncludeSessionNameMetadata: profile.GeneralOptions.OutputMetadataOptions.IncludeSessionName,
            IncludeOutputPathMetadata: profile.GeneralOptions.OutputMetadataOptions.IncludeOutputPath,
            IncludeFileSummaryMetadata: profile.GeneralOptions.OutputMetadataOptions.IncludeFileSummary,
            SkippedFilesMetadataMode: profile.GeneralOptions.OutputMetadataOptions.SkippedFilesMetadataMode,
            IncludeSourceExcludedFiles: profile.GeneralOptions.OutputMetadataOptions.IncludeSourceExcludedFiles);
    }

    private static void AddFilterRuleIfMissing(
        List<FileFilterRule> rules,
        FileFilterRule rule)
    {
        if (rules.Any(x => HasSameFilterRuleIdentity(x, rule)))
            return;

        rules.Add(rule);
    }

    private static bool HasSameFilterRuleIdentity(FileFilterRule left, FileFilterRule right)
    {
        return left.Mode == right.Mode &&
               left.Target == right.Target &&
               left.PatternType == right.PatternType &&
               left.IsEnabled == right.IsEnabled &&
               string.Equals(left.Pattern, right.Pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProfileName(string? profileName)
    {
        return string.IsNullOrWhiteSpace(profileName)
            ? "Default"
            : profileName.Trim();
    }

    private static string? NormalizeDescription(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private void FileTypes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (FileTypeOptionViewModel item in e.OldItems)
                item.PropertyChanged -= FileType_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (FileTypeOptionViewModel item in e.NewItems)
                item.PropertyChanged += FileType_PropertyChanged;
        }

        if (!_isResettingFileTypes)
        {
            RebuildFileTypeGroups();
            RefreshCSharpSectionAvailability();
        }
    }

    private void FileType_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileTypeOptionViewModel.IsEnabled))
        {
            RefreshVisibleFileTypes();
            RefreshCSharpSectionAvailability();
        }
    }

    private void FilterRules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ProfileFilterRuleItemViewModel item in e.OldItems)
                item.PropertyChanged -= FilterRule_PropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (ProfileFilterRuleItemViewModel item in e.NewItems)
                item.PropertyChanged += FilterRule_PropertyChanged;
        }

        RefreshVisibleFilterRules();
        RaiseFilterRuleStateProperties();
        RaiseFilterRuleCommandStates();
    }

    private void FilterRule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProfileFilterRuleItemViewModel.HasValidationError) or
            nameof(ProfileFilterRuleItemViewModel.ValidationMessage) or
            nameof(ProfileFilterRuleItemViewModel.Pattern) or
            nameof(ProfileFilterRuleItemViewModel.Mode) or
            nameof(ProfileFilterRuleItemViewModel.Target) or
            nameof(ProfileFilterRuleItemViewModel.PatternType) or
            nameof(ProfileFilterRuleItemViewModel.IsEnabled) or
            nameof(ProfileFilterRuleItemViewModel.Description) or
            nameof(ProfileFilterRuleItemViewModel.IsUserEditable))
        {
            RefreshVisibleFilterRules();
            RaiseFilterRuleStateProperties();
            RaiseFilterRuleCommandStates();
        }
    }

    private void FileTypeFilters_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshVisibleFileTypes();
    }

    private void FilterRuleFilters_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshVisibleFilterRules();
        RaiseFilterRuleCommandStates();
    }

    private void RaiseFilterRuleStateProperties()
    {
        OnPropertyChanged(nameof(FilterRules));
        OnPropertyChanged(nameof(VisibleFilterRules));
        OnPropertyChanged(nameof(HasFilterRules));
        OnPropertyChanged(nameof(HasVisibleFilterRules));
        OnPropertyChanged(nameof(HasFilterRuleFilterEmptyState));
        OnPropertyChanged(nameof(HasInvalidFilterRules));
        OnPropertyChanged(nameof(CanUseProfile));
        OnPropertyChanged(nameof(CanClearFilterRules));
        OnPropertyChanged(nameof(CanDuplicateFilterRule));
        OnPropertyChanged(nameof(CanRemoveFilterRule));
        OnPropertyChanged(nameof(CanMoveFilterRuleUp));
        OnPropertyChanged(nameof(CanMoveFilterRuleDown));
    }

    private void RaiseFilterRuleCommandStates()
    {
        DuplicateFilterRuleCommand.RaiseCanExecuteChanged();
        RemoveFilterRuleCommand.RaiseCanExecuteChanged();
        MoveFilterRuleUpCommand.RaiseCanExecuteChanged();
        MoveFilterRuleDownCommand.RaiseCanExecuteChanged();
        ClearFilterRulesCommand.RaiseCanExecuteChanged();
    }

    private sealed record FileTypeGroupDefinition(
        string Key,
        string Title,
        string Description);
}