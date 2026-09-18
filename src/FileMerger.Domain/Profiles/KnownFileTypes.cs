using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Domain.Profiles;

public static class KnownFileTypes
{
    public static readonly FileTypeDefinition CSharp = new(
        ".cs",
        "C# source",
        FileKind.CSharp,
        isEnabled: true,
        supportsLanguageSpecificProcessing: true);

    public static readonly FileTypeDefinition JavaScript = new(".js", "JavaScript source", FileKind.JavaScript);

    public static readonly FileTypeDefinition JavaScriptReact = new(
        ".jsx",
        "JavaScript JSX source",
        FileKind.JavaScript);

    public static readonly FileTypeDefinition JavaScriptModule = new(
        ".mjs",
        "JavaScript ES module",
        FileKind.JavaScript);

    public static readonly FileTypeDefinition JavaScriptCommonJs = new(
        ".cjs",
        "JavaScript CommonJS module",
        FileKind.JavaScript);

    public static readonly FileTypeDefinition TypeScript = new(".ts", "TypeScript source", FileKind.TypeScript);

    public static readonly FileTypeDefinition TypeScriptReact = new(
        ".tsx",
        "TypeScript TSX source",
        FileKind.TypeScript);

    public static readonly FileTypeDefinition TypeScriptModule = new(
        ".mts",
        "TypeScript ES module",
        FileKind.TypeScript);

    public static readonly FileTypeDefinition TypeScriptCommonJs = new(
        ".cts",
        "TypeScript CommonJS module",
        FileKind.TypeScript);

    public static readonly FileTypeDefinition Xml = new(".xml", "XML", FileKind.Xml);

    public static readonly FileTypeDefinition Xaml = new(".xaml", "XAML", FileKind.Xaml);

    public static readonly FileTypeDefinition Json = new(".json", "JSON", FileKind.Json);

    public static readonly FileTypeDefinition Jsonc = new(".jsonc", "JSON with comments", FileKind.Json);

    public static readonly FileTypeDefinition Text = new(".txt", "Text", FileKind.Text);

    public static readonly FileTypeDefinition Markdown = new(".md", "Markdown", FileKind.Text);

    public static readonly FileTypeDefinition Html = new(".html", "HTML", FileKind.Html);

    public static readonly FileTypeDefinition HtmlAlt = new(".htm", "HTML", FileKind.Html);

    public static readonly FileTypeDefinition Css = new(".css", "CSS", FileKind.Stylesheet);

    public static readonly FileTypeDefinition Scss = new(".scss", "SCSS", FileKind.Stylesheet);

    public static readonly FileTypeDefinition Sass = new(".sass", "Sass", FileKind.Stylesheet);

    public static readonly FileTypeDefinition Less = new(".less", "Less", FileKind.Stylesheet);

    public static readonly FileTypeDefinition GraphQl = new(".graphql", "GraphQL", FileKind.GraphQl);

    public static readonly FileTypeDefinition GraphQlAlt = new(".gql", "GraphQL", FileKind.GraphQl);

    public static readonly FileTypeDefinition Lock = new(".lock", "Lock file", FileKind.Text);

    public static readonly FileTypeDefinition Yaml = new(".yml", "YAML", FileKind.Text);

    public static readonly FileTypeDefinition YamlAlt = new(".yaml", "YAML", FileKind.Text);

    public static readonly FileTypeDefinition Toml = new(".toml", "TOML", FileKind.Text);

    public static readonly FileTypeDefinition Ini = new(".ini", "INI", FileKind.Text);

    public static readonly FileTypeDefinition Cfg = new(".cfg", "CFG", FileKind.Text);

    public static readonly FileTypeDefinition Conf = new(".conf", "CONF", FileKind.Text);

    public static readonly FileTypeDefinition Config = new(".config", "Config (XML)", FileKind.Xml);

    public static readonly FileTypeDefinition EditorConfig = new(".editorconfig", "EditorConfig", FileKind.Text);

    public static readonly FileTypeDefinition Props = new(".props", "MSBuild props", FileKind.Xml);

    public static readonly FileTypeDefinition Targets = new(".targets", "MSBuild targets", FileKind.Xml);

    public static readonly FileTypeDefinition Csproj = new(".csproj", "C# project", FileKind.Xml);

    public static readonly FileTypeDefinition Solution = new(".sln", "Visual Studio solution", FileKind.Text);

    public static readonly FileTypeDefinition SolutionXml = new(".slnx", "Visual Studio XML solution", FileKind.Xml);

    public static readonly FileTypeDefinition Resx = new(".resx", "Resources", FileKind.Xml);

    public static readonly FileTypeDefinition Settings = new(".settings", "Settings", FileKind.Xml);

    public static readonly FileTypeDefinition Manifest = new(".manifest", "Manifest", FileKind.Xml);

    public static readonly FileTypeDefinition Pubxml = new(".pubxml", "Publish profile", FileKind.Xml);

    public static readonly FileTypeDefinition Ruleset = new(".ruleset", "Code analysis ruleset", FileKind.Xml);

    public static readonly FileTypeDefinition Asmdef = new(".asmdef", "Unity assembly definition", FileKind.Json);

    public static readonly FileTypeDefinition Asmref = new(".asmref", "Unity assembly reference", FileKind.Json);

    public static readonly FileTypeDefinition UnityScene = new(".unity", "Unity scene", FileKind.Text);

    public static readonly FileTypeDefinition Prefab = new(".prefab", "Unity prefab", FileKind.Text);

    public static readonly FileTypeDefinition Asset = new(".asset", "Unity asset", FileKind.Text);

    public static readonly FileTypeDefinition Meta = new(".meta", "Unity meta", FileKind.Text);

    public static readonly FileTypeDefinition Mat = new(".mat", "Unity material", FileKind.Text);

    public static readonly FileTypeDefinition Controller = new(
        ".controller",
        "Unity animator controller",
        FileKind.Text);

    public static readonly FileTypeDefinition Anim = new(".anim", "Unity animation", FileKind.Text);

    public static readonly FileTypeDefinition OverrideController = new(
        ".overrideController",
        "Unity override controller",
        FileKind.Text);

    public static readonly FileTypeDefinition Playable = new(".playable", "Unity playable asset", FileKind.Text);

    public static readonly FileTypeDefinition PhysicMaterial = new(
        ".physicMaterial",
        "Unity physics material",
        FileKind.Text);

    public static readonly FileTypeDefinition PhysicsMaterial2D = new(
        ".physicsMaterial2D",
        "Unity 2D physics material",
        FileKind.Text);

    public static readonly FileTypeDefinition SpriteAtlas = new(".spriteatlas", "Unity sprite atlas", FileKind.Text);

    public static readonly FileTypeDefinition InputActions = new(".inputactions", "Unity input actions", FileKind.Json);

    public static readonly FileTypeDefinition Shader = new(".shader", "Unity shader", FileKind.Text);

    public static readonly FileTypeDefinition Compute = new(".compute", "Unity compute shader", FileKind.Text);

    public static readonly FileTypeDefinition Hlsl = new(".hlsl", "HLSL", FileKind.Text);

    public static readonly FileTypeDefinition Cginc = new(".cginc", "CG include", FileKind.Text);

    public static readonly FileTypeDefinition Uxml = new(".uxml", "Unity UI XML", FileKind.Xml);

    public static readonly FileTypeDefinition Uss = new(".uss", "Unity style sheet", FileKind.Text);

    public static readonly FileTypeDefinition ShaderGraph = new(".shadergraph", "Unity shader graph", FileKind.Text);

    public static readonly FileTypeDefinition Vfx = new(".vfx", "Unity visual effect graph", FileKind.Text);

    public static IReadOnlyCollection<FileTypeDefinition> All { get; } =
    [
        CSharp,
        JavaScript,
        JavaScriptReact,
        JavaScriptModule,
        JavaScriptCommonJs,
        TypeScript,
        TypeScriptReact,
        TypeScriptModule,
        TypeScriptCommonJs,
        Xml,
        Xaml,
        Json,
        Jsonc,
        Text,
        Markdown,
        Html,
        HtmlAlt,
        Css,
        Scss,
        Sass,
        Less,
        GraphQl,
        GraphQlAlt,
        Lock,
        Yaml,
        YamlAlt,
        Toml,
        Ini,
        Cfg,
        Conf,
        Config,
        EditorConfig,
        Props,
        Targets,
        Csproj,
        Solution,
        SolutionXml,
        Resx,
        Settings,
        Manifest,
        Pubxml,
        Ruleset,
        Asmdef,
        Asmref,
        UnityScene,
        Prefab,
        Asset,
        Meta,
        Mat,
        Controller,
        Anim,
        OverrideController,
        Playable,
        PhysicMaterial,
        PhysicsMaterial2D,
        SpriteAtlas,
        InputActions,
        Shader,
        Compute,
        Hlsl,
        Cginc,
        Uxml,
        Uss,
        ShaderGraph,
        Vfx
    ];

    public static IReadOnlyCollection<FileTypeDefinition> Default { get; } =
    [
        CSharp,
        Xml,
        Xaml,
        Json,
        Text,
        Markdown,
        Yaml,
        YamlAlt,
        Toml,
        Ini,
        Cfg,
        Conf
    ];

    public static IReadOnlyCollection<FileTypeDefinition> WpfApplication { get; } =
    [
        CSharp,
        Xaml,
        Csproj,
        Solution,
        SolutionXml,
        Props,
        Targets,
        Config,
        Json,
        Xml,
        Resx,
        Settings,
        Manifest,
        Pubxml,
        Ruleset
    ];

    public static IReadOnlyCollection<FileTypeDefinition> UnityProject { get; } =
    [
        CSharp,
        Json,
        Xml,
        Text,
        Markdown,
        Yaml,
        YamlAlt,
        Asmdef,
        Asmref,
        UnityScene,
        Prefab,
        Asset,
        Mat,
        Controller,
        Anim,
        OverrideController,
        Playable,
        PhysicMaterial,
        PhysicsMaterial2D,
        SpriteAtlas,
        InputActions,
        Shader,
        Compute,
        Hlsl,
        Cginc,
        Uxml,
        Uss,
        ShaderGraph,
        Vfx
    ];
}