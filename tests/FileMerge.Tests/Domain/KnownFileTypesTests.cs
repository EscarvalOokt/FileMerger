using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain;

public sealed class KnownFileTypesTests
{
    [Fact]
    public void Default_Should_Contain_Current_Default_File_Types()
    {
        IReadOnlyCollection<FileTypeDefinition> result = KnownFileTypes.Default;

        Assert.Contains(result, x => x.Extension == ".cs" && x.Kind == FileKind.CSharp);
        Assert.Contains(result, x => x.Extension == ".xml" && x.Kind == FileKind.Xml);
        Assert.Contains(result, x => x.Extension == ".xaml" && x.Kind == FileKind.Xaml);
        Assert.Contains(result, x => x.Extension == ".json" && x.Kind == FileKind.Json);
        Assert.Contains(result, x => x.Extension == ".txt" && x.Kind == FileKind.Text);
        Assert.Contains(result, x => x.Extension == ".md" && x.Kind == FileKind.Text);
        Assert.Contains(result, x => x.Extension == ".yml" && x.Kind == FileKind.Text);
        Assert.Contains(result, x => x.Extension == ".yaml" && x.Kind == FileKind.Text);
        Assert.Contains(result, x => x.Extension == ".toml" && x.Kind == FileKind.Text);
        Assert.Contains(result, x => x.Extension == ".ini" && x.Kind == FileKind.Text);
        Assert.Contains(result, x => x.Extension == ".cfg" && x.Kind == FileKind.Text);
        Assert.Contains(result, x => x.Extension == ".conf" && x.Kind == FileKind.Text);
    }

    [Fact]
    public void All_Should_Contain_All_Default_File_Types()
    {
        var allExtensions = KnownFileTypes.All
            .Select(x => x.Extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (FileTypeDefinition defaultType in KnownFileTypes.Default)
        {
            Assert.Contains(defaultType.Extension, allExtensions);
        }
    }

    [Fact]
    public void All_Should_Not_Contain_Duplicate_Extensions()
    {
        string[] extensions = KnownFileTypes.All.Select(x => x.Extension).ToArray();
        string[] distinctExtensions = extensions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(distinctExtensions.Length, extensions.Length);
    }

    [Fact]
    public void Default_Should_Not_Contain_Duplicate_Extensions()
    {
        string[] extensions = KnownFileTypes.Default.Select(x => x.Extension).ToArray();
        string[] distinctExtensions = extensions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(distinctExtensions.Length, extensions.Length);
    }

    [Fact]
    public void All_Should_Contain_Wpf_File_Types()
    {
        var extensions = KnownFileTypes.All
            .Select(x => x.Extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(".sln", extensions);
        Assert.Contains(".slnx", extensions);
        Assert.Contains(".editorconfig", extensions);
        Assert.Contains(".resx", extensions);
        Assert.Contains(".settings", extensions);
        Assert.Contains(".manifest", extensions);
        Assert.Contains(".pubxml", extensions);
        Assert.Contains(".ruleset", extensions);
    }

    [Fact]
    public void All_Should_Contain_Unity_File_Types()
    {
        var extensions = KnownFileTypes.All
            .Select(x => x.Extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(".asmdef", extensions);
        Assert.Contains(".asmref", extensions);
        Assert.Contains(".unity", extensions);
        Assert.Contains(".prefab", extensions);
        Assert.Contains(".asset", extensions);
        Assert.Contains(".meta", extensions);
        Assert.Contains(".mat", extensions);
        Assert.Contains(".controller", extensions);
        Assert.Contains(".anim", extensions);
        Assert.Contains(".overrideController", extensions);
        Assert.Contains(".playable", extensions);
        Assert.Contains(".physicMaterial", extensions);
        Assert.Contains(".physicsMaterial2D", extensions);
        Assert.Contains(".spriteatlas", extensions);
        Assert.Contains(".inputactions", extensions);
        Assert.Contains(".shader", extensions);
        Assert.Contains(".compute", extensions);
        Assert.Contains(".hlsl", extensions);
        Assert.Contains(".cginc", extensions);
        Assert.Contains(".uxml", extensions);
        Assert.Contains(".uss", extensions);
        Assert.Contains(".shadergraph", extensions);
        Assert.Contains(".vfx", extensions);
    }

    [Fact]
    public void WpfApplication_Should_Enable_Wpf_Application_File_Types()
    {
        var extensions = KnownFileTypes.WpfApplication
            .Select(x => x.Extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(".cs", extensions);
        Assert.Contains(".xaml", extensions);
        Assert.Contains(".csproj", extensions);
        Assert.Contains(".sln", extensions);
        Assert.Contains(".slnx", extensions);
        Assert.Contains(".resx", extensions);
        Assert.Contains(".settings", extensions);
        Assert.Contains(".manifest", extensions);
        Assert.Contains(".props", extensions);
        Assert.Contains(".targets", extensions);
    }

    [Fact]
    public void UnityProject_Should_Enable_Unity_Project_File_Types()
    {
        var extensions = KnownFileTypes.UnityProject
            .Select(x => x.Extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(".cs", extensions);
        Assert.Contains(".asmdef", extensions);
        Assert.Contains(".asmref", extensions);
        Assert.Contains(".unity", extensions);
        Assert.Contains(".prefab", extensions);
        Assert.Contains(".asset", extensions);
        Assert.Contains(".shader", extensions);
        Assert.Contains(".compute", extensions);
        Assert.Contains(".hlsl", extensions);
        Assert.Contains(".cginc", extensions);
        Assert.Contains(".uxml", extensions);
        Assert.Contains(".uss", extensions);
        Assert.Contains(".inputactions", extensions);
    }

    [Fact]
    public void UnityProject_Should_Not_Enable_Meta_By_Default()
    {
        Assert.DoesNotContain(KnownFileTypes.UnityProject, x =>
            string.Equals(x.Extension, ".meta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WpfApplication_Should_Not_Contain_Duplicate_Extensions()
    {
        string[] extensions = KnownFileTypes.WpfApplication.Select(x => x.Extension).ToArray();
        string[] distinctExtensions = extensions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(distinctExtensions.Length, extensions.Length);
    }

    [Fact]
    public void UnityProject_Should_Not_Contain_Duplicate_Extensions()
    {
        string[] extensions = KnownFileTypes.UnityProject.Select(x => x.Extension).ToArray();
        string[] distinctExtensions = extensions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(distinctExtensions.Length, extensions.Length);
    }

    [Fact]
    public void CSharp_Should_Support_Language_Specific_Processing()
    {
        Assert.True(KnownFileTypes.CSharp.SupportsLanguageSpecificProcessing);
    }

    [Fact]
    public void SolutionXml_Should_Define_Slnx_File_Type()
    {
        Assert.Equal(".slnx", KnownFileTypes.SolutionXml.Extension);
        Assert.Equal("Visual Studio XML solution", KnownFileTypes.SolutionXml.DisplayName);
        Assert.Equal(FileKind.Xml, KnownFileTypes.SolutionXml.Kind);
    }

    [Fact]
    public void EditorConfig_Should_Define_EditorConfig_File_Type()
    {
        Assert.Equal(".editorconfig", KnownFileTypes.EditorConfig.Extension);
        Assert.Equal("EditorConfig", KnownFileTypes.EditorConfig.DisplayName);
        Assert.Equal(FileKind.Text, KnownFileTypes.EditorConfig.Kind);
    }

    [Fact]
    public void Props_Should_Define_MsBuild_Props_File_Type()
    {
        Assert.Equal(".props", KnownFileTypes.Props.Extension);
        Assert.Equal("MSBuild props", KnownFileTypes.Props.DisplayName);
        Assert.Equal(FileKind.Xml, KnownFileTypes.Props.Kind);
    }

    [Fact]
    public void Targets_Should_Define_MsBuild_Targets_File_Type()
    {
        Assert.Equal(".targets", KnownFileTypes.Targets.Extension);
        Assert.Equal("MSBuild targets", KnownFileTypes.Targets.DisplayName);
        Assert.Equal(FileKind.Xml, KnownFileTypes.Targets.Kind);
    }

    [Fact]
    public void All_Should_Contain_MsBuild_Props_And_Targets_File_Types()
    {
        var extensions = KnownFileTypes.All
            .Select(x => x.Extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(KnownFileTypes.Props.Extension, extensions);
        Assert.Contains(KnownFileTypes.Targets.Extension, extensions);
    }
}