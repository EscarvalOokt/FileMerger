using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Wpf.Features.Profile.Models;
using FileMerger.Wpf.Features.Profile.Services;

namespace FileMerger.Tests.Wpf.Features.Profile.Services;

public sealed class BuiltInProfilePresetProviderTests
{
    [Fact]
    public void GetAll_Should_Return_Current_BuiltIn_Profile_Ids()
    {
        ProfileLibraryEntry[] entries = CreateProvider().GetAll().ToArray();

        Assert.Contains(entries, x => x.Id == "builtin.default");
        Assert.Contains(entries, x => x.Id == "builtin.csharp-minimal");
        Assert.Contains(entries, x => x.Id == "builtin.docs-config");
        Assert.Contains(entries, x => x.Id == "builtin.wpf-app");
        Assert.Contains(entries, x => x.Id == "builtin.unity-project");
        Assert.Contains(entries, x => x.Id == "builtin.full-source-dump");

        Assert.Equal(entries.Length, entries.Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void GetAll_Should_Return_ReadOnly_BuiltIn_Entries()
    {
        IReadOnlyCollection<ProfileLibraryEntry> entries = CreateProvider().GetAll();

        Assert.All(
            entries,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            entry =>
            {
                Assert.Equal(ProfileEntryKind.BuiltIn, entry.Kind);
                Assert.True(entry.Metadata.IsBuiltIn);
                Assert.True(entry.Metadata.IsReadOnly);
                Assert.True(entry.IsBuiltIn);
                Assert.True(entry.IsReadOnly);
                Assert.False(entry.IsUserDefined);
                Assert.Null(entry.FilePath);
            });
    }

    [Fact]
    public void Default_Profile_Should_Enable_Default_Catalog_FileTypes()
    {
        var catalog = new BuiltInFileTypeCatalog();
        var provider = new BuiltInProfilePresetProvider(catalog);

        ProfileLibraryEntry entry = provider.GetAll().Single(x => x.Id == "builtin.default");

        AssertSameExtensions(catalog.GetDefault().Select(x => x.Extension), EnabledExtensions(entry));
        AssertCSharpFilterRules(entry);
    }

    [Fact]
    public void CSharpMinimal_Profile_Should_Enable_Only_CSharp()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.csharp-minimal");

        AssertSameExtensions([".cs"], EnabledExtensions(entry));

        AssertCSharpFilterRules(entry);
    }

    [Fact]
    public void DocsAndConfig_Profile_Should_Enable_Docs_And_Config_Extensions()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.docs-config");

        AssertSameExtensions(
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
            EnabledExtensions(entry));

        Assert.DoesNotContain(entry.Profile.FileTypes, x => x is { Extension: ".cs", IsEnabled: true });

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "bin", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "obj", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "*.Designer.cs", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DocsAndConfig_Profile_Should_Enable_Slnx()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.docs-config");

        Assert.Contains(
            entry.Profile.FileTypes,
            x => x.Extension == KnownFileTypes.SolutionXml.Extension && x is { Kind: FileKind.Xml, IsEnabled: true });
    }

    [Fact]
    public void DocsAndConfig_Profile_Should_Enable_EditorConfig()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.docs-config");

        Assert.Contains(
            entry.Profile.FileTypes,
            x => x.Extension == KnownFileTypes.EditorConfig.Extension && x is { Kind: FileKind.Text, IsEnabled: true });
    }

    [Fact]
    public void DocsAndConfig_Profile_Should_Enable_MsBuild_Props_And_Targets()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.docs-config");

        AssertMsBuildPropsAndTargetsEnabled(entry);
    }

    [Fact]
    public void WpfApplication_Profile_Should_Enable_Wpf_Application_Extensions()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.wpf-app");

        AssertSameExtensions(KnownFileTypes.WpfApplication.Select(x => x.Extension), EnabledExtensions(entry));

        AssertCSharpFilterRules(entry);
    }

    [Fact]
    public void WpfApplication_Profile_Should_Enable_Slnx()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.wpf-app");

        Assert.Contains(
            entry.Profile.FileTypes,
            x => x.Extension == KnownFileTypes.SolutionXml.Extension && x is { Kind: FileKind.Xml, IsEnabled: true });
    }

    [Fact]
    public void WpfApplication_Profile_Should_Enable_MsBuild_Props_And_Targets()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.wpf-app");

        AssertMsBuildPropsAndTargetsEnabled(entry);
    }

    [Fact]
    public void UnityProject_Profile_Should_Enable_Unity_Project_Extensions()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.unity-project");

        AssertSameExtensions(KnownFileTypes.UnityProject.Select(x => x.Extension), EnabledExtensions(entry));

        AssertCSharpFilterRules(entry);
    }

    [Fact]
    public void UnityProject_Profile_Should_Not_Enable_Unity_Meta_By_Default()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.unity-project");

        Assert.DoesNotContain(
            entry.Profile.FileTypes,
            x => string.Equals(x.Extension, ".meta", StringComparison.OrdinalIgnoreCase) && x.IsEnabled);
        AssertCSharpFilterRules(entry);
    }

    [Fact]
    public void FullSourceDump_Profile_Should_Enable_All_Known_FileTypes()
    {
        var catalog = new BuiltInFileTypeCatalog();
        var provider = new BuiltInProfilePresetProvider(catalog);

        ProfileLibraryEntry entry = provider.GetAll().Single(x => x.Id == "builtin.full-source-dump");

        AssertSameExtensions(catalog.GetAll().Select(x => x.Extension), EnabledExtensions(entry));


        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "bin", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "obj", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "*.Designer.cs", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FullSourceDump_Profile_Should_Not_Include_FilterRules()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.full-source-dump");

        Assert.Empty(entry.Profile.FilterRules ?? []);

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "bin", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "obj", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "*.Designer.cs", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            entry.Profile.FilterRules ?? [],
            x => string.Equals(x.Pattern, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FullSourceDump_Profile_Should_Enable_Slnx()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.full-source-dump");

        Assert.Contains(
            entry.Profile.FileTypes,
            x => x.Extension == KnownFileTypes.SolutionXml.Extension && x is { Kind: FileKind.Xml, IsEnabled: true });
    }

    [Fact]
    public void FullSourceDump_Profile_Should_Enable_EditorConfig()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.full-source-dump");

        Assert.Contains(
            entry.Profile.FileTypes,
            x => x.Extension == KnownFileTypes.EditorConfig.Extension && x is { Kind: FileKind.Text, IsEnabled: true });
    }

    [Fact]
    public void FullSourceDump_Profile_Should_Enable_MsBuild_Props_And_Targets()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.full-source-dump");

        AssertMsBuildPropsAndTargetsEnabled(entry);
    }

    [Fact]
    public void WpfApplication_Profile_Should_Include_Wpf_Specific_FilterRules()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.wpf-app");

        Assert.NotNull(entry.Profile.FilterRules);

        Assert.Contains(
            entry.Profile.FilterRules!,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Wildcard,
                Pattern: "*.g.cs", IsEnabled: true, IsUserEditable: true
            });

        Assert.Contains(
            entry.Profile.FilterRules!,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Wildcard,
                Pattern: "*.g.i.cs", IsEnabled: true, IsUserEditable: true
            });

        Assert.Contains(
            entry.Profile.FilterRules!,
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Wildcard,
                Pattern: "*.AssemblyAttributes.cs", IsEnabled: true, IsUserEditable: true
            });

        AssertCSharpFilterRules(entry);
    }

    [Fact]
    public void UnityProject_Profile_Should_Include_Unity_Specific_FilterRules()
    {
        ProfileLibraryEntry entry = GetEntry("builtin.unity-project");

        Assert.NotNull(entry.Profile.FilterRules);

        AssertCSharpFilterRules(entry);
        AssertUnityDirectoryRule(entry, "Library");
        AssertUnityDirectoryRule(entry, "Temp");
        AssertUnityDirectoryRule(entry, "Logs");
        AssertUnityDirectoryRule(entry, "Build");
        AssertUnityDirectoryRule(entry, "Builds");
        AssertUnityDirectoryRule(entry, "UserSettings");
        AssertUnityDirectoryRule(entry, "MemoryCaptures");
        AssertUnityDirectoryRule(entry, "Recordings");
        AssertUnityDirectoryRule(entry, ".vs");
        AssertUnityDirectoryRule(entry, ".idea");
        AssertUnityDirectoryRule(entry, ".git");
    }

    private static void AssertCSharpFilterRules(ProfileLibraryEntry entry)
    {
        AssertDirectoryRule(entry, "bin");
        AssertDirectoryRule(entry, "obj");

        Assert.Contains(
            entry.Profile.FilterRules ?? [],
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Wildcard,
                Pattern: "*.Designer.cs", IsEnabled: true, IsUserEditable: true
            });

        Assert.Contains(
            entry.Profile.FilterRules ?? [],
            x => x is
            {
                Mode: FilterMode.Exclude, Target: FilterTarget.FileName, PatternType: RulePatternType.Exact,
                Pattern: "AssemblyInfo.cs", IsEnabled: true, IsUserEditable: true
            });
    }

    private static void AssertDirectoryRule(ProfileLibraryEntry entry, string pattern)
    {
        Assert.Contains(
            entry.Profile.FilterRules ?? [],
            x => x is
                 {
                     Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact
                 } &&
                 string.Equals(x.Pattern, pattern, StringComparison.OrdinalIgnoreCase) &&
                 x is { IsEnabled: true, IsUserEditable: true });
    }

    private static void AssertMsBuildPropsAndTargetsEnabled(ProfileLibraryEntry entry)
    {
        Assert.Contains(
            entry.Profile.FileTypes,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            x => x.Extension == KnownFileTypes.Props.Extension && x is { Kind: FileKind.Xml, IsEnabled: true });

        Assert.Contains(
            entry.Profile.FileTypes,
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            x => x.Extension == KnownFileTypes.Targets.Extension && x is { Kind: FileKind.Xml, IsEnabled: true });
    }

    private static BuiltInProfilePresetProvider CreateProvider()
    {
        return new BuiltInProfilePresetProvider(new BuiltInFileTypeCatalog());
    }

    private static ProfileLibraryEntry GetEntry(string id)
    {
        return CreateProvider().GetAll().Single(x => x.Id == id);
    }

    private static string[] EnabledExtensions(ProfileLibraryEntry entry)
    {
        return
        [
            .. entry.Profile.FileTypes.Where(x => x.IsEnabled)
                .Select(x => x.Extension)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static void AssertUnityDirectoryRule(ProfileLibraryEntry entry, string pattern)
    {
        Assert.Contains(
            entry.Profile.FilterRules ?? [],
            x => x is
                 {
                     Mode: FilterMode.Exclude, Target: FilterTarget.DirectorySegment, PatternType: RulePatternType.Exact
                 } &&
                 string.Equals(x.Pattern, pattern, StringComparison.OrdinalIgnoreCase) &&
                 x is { IsEnabled: true, IsUserEditable: true });
    }

    private static void AssertSameExtensions(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        string[] expectedArray =
        [
            .. expected.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        ];

        string[] actualArray =
        [
            .. actual.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        ];

        Assert.Equal(expectedArray, actualArray);
    }
}