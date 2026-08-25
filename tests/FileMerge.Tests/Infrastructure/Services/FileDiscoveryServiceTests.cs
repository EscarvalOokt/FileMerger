using System.IO;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Discovery;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class FileDiscoveryServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public FileDiscoveryServiceTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(FileDiscoveryServiceTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void DiscoverFiles_Should_Throw_When_Sources_Are_Null()
    {
        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.DiscoverFiles(null!, profile));

        Assert.Equal("sources", ex.ParamName);
    }

    [Fact]
    public void DiscoverFiles_Should_Throw_When_Profile_Is_Null()
    {
        var service = new FileDiscoveryService();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.DiscoverFiles([], null!));

        Assert.Equal("profile", ex.ParamName);
    }

    [Fact]
    public void DiscoverFiles_Should_Retain_Disabled_Known_Types_Outside_MergeCandidates()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "A.cs", "class A {}");
        CreateFile(root, "B.json", "{}");
        CreateFile(root, "C.txt", "hello");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp),
            new FileTypeDefinition(".json", "JSON", FileKind.Json, isEnabled: false),
            new FileTypeDefinition(".txt", "Text", FileKind.Text));

        var source = new MergeSource(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Equal(3, result.InventoryFiles.Count);
        Assert.Equal(2, result.MergeCandidates.Count);
        Assert.Contains(result.MergeCandidates, x => x is { Extension: ".cs", Kind: FileKind.CSharp });
        Assert.Contains(result.MergeCandidates, x => x is { Extension: ".txt", Kind: FileKind.Text });

        InputFile disabledFile = Assert.Single(result.InventoryFiles, x => x.Extension == ".json");
        Assert.Equal(FileKind.Json, disabledFile.Kind);
        Assert.False(disabledFile.IsMergeCandidate);
        Assert.False(disabledFile.IsIncluded);
        Assert.Equal("discovery.file-type-disabled", disabledFile.SkipReason?.Code);
    }

    [Fact]
    public void DiscoverFiles_Should_Search_Recursively_When_Source_Is_Recursive()
    {
        string root = CreateDirectory("project");
        string sub = Directory.CreateDirectory(Path.Combine(root, "Sub")).FullName;

        CreateFile(root, "Root.cs", "class Root {}");
        CreateFile(sub, "Nested.cs", "class Nested {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));
        var source = new MergeSource(root, MergeSourceType.Directory, isRecursive: true);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Equal(2, result.Length);
        Assert.Contains(result, x => x.RelativePath == "Root.cs");
        Assert.Contains(result, x => x.RelativePath == Path.Combine("Sub", "Nested.cs"));
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Search_Recursively_When_Source_Is_Not_Recursive()
    {
        string root = CreateDirectory("project");
        string sub = Directory.CreateDirectory(Path.Combine(root, "Sub")).FullName;

        CreateFile(root, "Root.cs", "class Root {}");
        CreateFile(sub, "Nested.cs", "class Nested {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));
        var source = new MergeSource(root, MergeSourceType.Directory, isRecursive: false);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles.ToArray()];

        Assert.Single(result);
        Assert.Equal("Root.cs", result[0].RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_File_Source_When_Extension_Is_Enabled()
    {
        string filePath = CreateFile(_tempRoot, "Single.cs", "class Single {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));
        var source = new MergeSource(filePath, MergeSourceType.File);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(filePath, result[0].FullPath);
        Assert.Equal("Single.cs", result[0].RelativePath);
        Assert.Equal(".cs", result[0].Extension);
        Assert.Equal(FileKind.CSharp, result[0].Kind);
    }

    [Fact]
    public void DiscoverFiles_Should_Retain_Unsupported_File_Source_Outside_MergeCandidates()
    {
        string filePath = CreateFile(_tempRoot, "Single.json", "{}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));
        var source = new MergeSource(filePath, MergeSourceType.File);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.Equal(filePath, file.FullPath);
        Assert.Equal("Single.json", file.RelativePath);
        Assert.Equal(".json", file.Extension);
        Assert.Equal(FileKind.Unknown, file.Kind);
        Assert.False(file.IsMergeCandidate);
        Assert.False(file.IsIncluded);
        Assert.Equal("discovery.unsupported-file-type", file.SkipReason?.Code);
        Assert.Empty(result.MergeCandidates);
    }

    [Fact]
    public void DiscoverFiles_Should_Ignore_Disabled_Sources()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "A.cs", "class A {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));
        var source = new MergeSource(root, MergeSourceType.Directory, isEnabled: false);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_Empty_When_Directory_Does_Not_Exist()
    {
        string missing = Path.Combine(_tempRoot, "missing");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));
        var source = new MergeSource(missing, MergeSourceType.Directory);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Empty(result);
    }

    [Fact]
    public void DiscoverFiles_Should_Deduplicate_By_FullPath()
    {
        string root = CreateDirectory("project");
        string filePath = CreateFile(root, "A.cs", "class A {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource[] sources =
        [
            new MergeSource(root, MergeSourceType.Directory),
            new MergeSource(filePath, MergeSourceType.File)
        ];

        InputFile[] result = [.. service.DiscoverFiles(sources, profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(filePath, result[0].FullPath);
    }

    [Fact]
    public void DiscoverFiles_Should_Sort_By_RelativePath_Ascending()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "B.cs", "class B {}");
        CreateFile(root, "A.cs", "class A {}");
        CreateFile(root, "C.cs", "class C {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));
        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Equal("A.cs", result[0].RelativePath);
        Assert.Equal("B.cs", result[1].RelativePath);
        Assert.Equal("C.cs", result[2].RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Set_SizeInBytes_When_File_Is_Readable()
    {
        string root = CreateDirectory("project");
        string content = "1234567890";
        CreateFile(root, "A.cs", content);

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));
        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile result = service.DiscoverFiles([source], profile).InventoryFiles.Single();

        Assert.Equal(content.Length, result.SizeInBytes);
    }

    [Fact]
    public void DiscoverFiles_Should_Match_Extensions_Case_Insensitively()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "View.XAML", "<Grid />");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".xaml", "XAML", FileKind.Xaml));
        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile result = service.DiscoverFiles([source], profile).InventoryFiles.Single();

        Assert.Equal(".XAML", result.Extension);
        Assert.Equal(FileKind.Xaml, result.Kind);
        Assert.Equal("View.XAML", result.RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Use_RelativePath_From_Each_Directory_Source()
    {
        string firstRoot = CreateDirectory("project1");
        string secondRoot = CreateDirectory("project2");

        string firstSub = Directory.CreateDirectory(Path.Combine(firstRoot, "Sub")).FullName;
        string secondSub = Directory.CreateDirectory(Path.Combine(secondRoot, "Sub")).FullName;

        string firstFile = CreateFile(firstSub, "File.cs", "class First {}");
        string secondFile = CreateFile(secondSub, "File.cs", "class Second {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        InputFile[] result =
        [
            .. service.DiscoverFiles(
                [
                    new MergeSource(firstRoot, MergeSourceType.Directory),
                    new MergeSource(secondRoot, MergeSourceType.Directory)
                ],
                profile).InventoryFiles
        ];

        Assert.Equal(2, result.Length);

        Assert.Contains(result, x =>
            x.FullPath == firstFile &&
            x.RelativePath == Path.Combine("Sub", "File.cs"));

        Assert.Contains(result, x =>
            x.FullPath == secondFile &&
            x.RelativePath == Path.Combine("Sub", "File.cs"));
    }

    [Fact]
    public void DiscoverFiles_Should_Ignore_Profile_FilterRules_During_Discovery()
    {
        string root = CreateDirectory("project");
        string bin = Directory.CreateDirectory(Path.Combine(root, "bin")).FullName;
        CreateFile(bin, "Test.cs", "class Test {}");

        var service = new FileDiscoveryService();

        MergeProfile profile = CreateProfileWithRules(
            fileTypes:
            [
                new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)
            ],
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.DirectorySegment,
                    patternType: RulePatternType.Exact,
                    pattern: "bin")
            ]);

        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile result = service.DiscoverFiles([source], profile).InventoryFiles.Single();

        Assert.True(result.IsIncluded);
        Assert.Null(result.SkipReason);
        Assert.Equal(Path.Combine("bin", "Test.cs"), result.RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Exclude_Files_Inside_Excluded_Directory()
    {
        string root = CreateDirectory("project");
        string binDebug = Directory.CreateDirectory(Path.Combine(root, "bin", "Debug")).FullName;

        CreateFile(root, "Keep.cs", "class Keep {}");
        CreateFile(binDebug, "Generated.cs", "class Generated {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        var source = new MergeSource(
            path: root,
            type: MergeSourceType.Directory,
            isRecursive: true,
            exclusions:
            [
                new MergeSourceExclusion("bin", MergeSourceExclusionType.Directory)
            ]);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal("Keep.cs", result[0].RelativePath);
        Assert.DoesNotContain(result, x => x.RelativePath == Path.Combine("bin", "Debug", "Generated.cs"));
    }

    [Fact]
    public void DiscoverFiles_Should_Exclude_Nested_Directory_By_RelativePath()
    {
        string root = CreateDirectory("project");
        string src = Directory.CreateDirectory(Path.Combine(root, "Src")).FullName;
        string generated = Directory.CreateDirectory(Path.Combine(src, "Generated")).FullName;

        CreateFile(src, "Keep.cs", "class Keep {}");
        CreateFile(generated, "Auto.cs", "class Auto {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        var source = new MergeSource(
            path: root,
            type: MergeSourceType.Directory,
            isRecursive: true,
            exclusions:
            [
                new MergeSourceExclusion(
                    Path.Combine("Src", "Generated"),
                    MergeSourceExclusionType.Directory)
            ]);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(Path.Combine("Src", "Keep.cs"), result[0].RelativePath);
        Assert.DoesNotContain(result, x => x.RelativePath == Path.Combine("Src", "Generated", "Auto.cs"));
    }

    [Fact]
    public void DiscoverFiles_Should_Exclude_Exact_File_By_RelativePath()
    {
        string root = CreateDirectory("project");

        CreateFile(root, "Public.cs", "class Public {}");
        CreateFile(root, "Secrets.cs", "class Secrets {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        var source = new MergeSource(
            path: root,
            type: MergeSourceType.Directory,
            exclusions:
            [
                new MergeSourceExclusion("Secrets.cs", MergeSourceExclusionType.File)
            ]);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal("Public.cs", result[0].RelativePath);
        Assert.DoesNotContain(result, x => x.RelativePath == "Secrets.cs");
    }

    [Fact]
    public void DiscoverFiles_Should_Ignore_Disabled_Exclusions()
    {
        string root = CreateDirectory("project");

        CreateFile(root, "Public.cs", "class Public {}");
        CreateFile(root, "Secrets.cs", "class Secrets {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        var source = new MergeSource(
            path: root,
            type: MergeSourceType.Directory,
            exclusions:
            [
                new MergeSourceExclusion(
                    relativePath: "Secrets.cs",
                    type: MergeSourceExclusionType.File,
                    isEnabled: false)
            ]);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Equal(2, result.Length);
        Assert.Contains(result, x => x.RelativePath == "Public.cs");
        Assert.Contains(result, x => x.RelativePath == "Secrets.cs");
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Treat_Directory_Prefix_As_Excluded_Directory()
    {
        string root = CreateDirectory("project");
        string bin = Directory.CreateDirectory(Path.Combine(root, "bin")).FullName;
        string binary = Directory.CreateDirectory(Path.Combine(root, "binary")).FullName;

        CreateFile(bin, "Skip.cs", "class Skip {}");
        CreateFile(binary, "Keep.cs", "class Keep {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        var source = new MergeSource(
            path: root,
            type: MergeSourceType.Directory,
            isRecursive: true,
            exclusions:
            [
                new MergeSourceExclusion("bin", MergeSourceExclusionType.Directory)
            ]);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(Path.Combine("binary", "Keep.cs"), result[0].RelativePath);
        Assert.DoesNotContain(result, x => x.RelativePath == Path.Combine("bin", "Skip.cs"));
    }

    [Fact]
    public void DiscoverFiles_Should_Apply_File_Exclusions_When_Source_Is_Not_Recursive()
    {
        string root = CreateDirectory("project");
        string nested = Directory.CreateDirectory(Path.Combine(root, "Nested")).FullName;

        CreateFile(root, "A.cs", "class A {}");
        CreateFile(root, "B.cs", "class B {}");
        CreateFile(nested, "C.cs", "class C {}");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        var source = new MergeSource(
            path: root,
            type: MergeSourceType.Directory,
            isRecursive: false,
            exclusions:
            [
                new MergeSourceExclusion("B.cs", MergeSourceExclusionType.File)
            ]);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal("A.cs", result[0].RelativePath);
        Assert.DoesNotContain(result, x => x.RelativePath == "B.cs");
        Assert.DoesNotContain(result, x => x.RelativePath == Path.Combine("Nested", "C.cs"));
    }

    [Fact]
    public void DiscoverFiles_Should_Return_Wpf_Resource_File_When_Resx_Is_Enabled()
    {
        string root = CreateDirectory("wpf");
        CreateFile(root, "Resources.resx", "<root />");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(KnownFileTypes.Resx);
        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(".resx", result[0].Extension);
        Assert.Equal(FileKind.Xml, result[0].Kind);
        Assert.Equal("Resources.resx", result[0].RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_Unity_File_When_Unity_Extension_Is_Enabled()
    {
        string root = CreateDirectory("unity");
        CreateFile(root, "Player.prefab", "%YAML 1.1");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(KnownFileTypes.Prefab);
        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(".prefab", result[0].Extension);
        Assert.Equal(FileKind.Text, result[0].Kind);
        Assert.Equal("Player.prefab", result[0].RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_Unity_OverrideController_File_Case_Insensitively()
    {
        string root = CreateDirectory("unity");
        CreateFile(root, "Player.overrideController", "%YAML 1.1");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(KnownFileTypes.OverrideController);
        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(".overrideController", result[0].Extension);
        Assert.Equal(FileKind.Text, result[0].Kind);
        Assert.Equal("Player.overrideController", result[0].RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_Slnx_File_When_Slnx_Is_Enabled()
    {
        string root = CreateDirectory("solution");
        CreateFile(root, "FileMerger.slnx", "<Solution />");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(KnownFileTypes.SolutionXml);
        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(".slnx", result[0].Extension);
        Assert.Equal(FileKind.Xml, result[0].Kind);
        Assert.Equal("FileMerger.slnx", result[0].RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_EditorConfig_File_When_EditorConfig_Is_Enabled()
    {
        string root = CreateDirectory("editorconfig");
        CreateFile(root, ".editorconfig", "root = true");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(KnownFileTypes.EditorConfig);
        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(".editorconfig", result[0].Extension);
        Assert.Equal(FileKind.Text, result[0].Kind);
        Assert.Equal(".editorconfig", result[0].RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_DirectoryBuild_And_Packages_Files_When_MsBuild_FileTypes_Are_Enabled()
    {
        string root = CreateDirectory("msbuild-config");
        CreateFile(root, "Directory.Build.props", "<Project />");
        CreateFile(root, "Directory.Packages.props", "<Project />");
        CreateFile(root, "Directory.Build.targets", "<Project />");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(
            KnownFileTypes.Props,
            KnownFileTypes.Targets);

        var source = new MergeSource(root, MergeSourceType.Directory);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Equal(3, result.Length);

        Assert.Contains(result, x =>
            x.RelativePath == "Directory.Build.props" &&
            x.Extension == KnownFileTypes.Props.Extension &&
            x.Kind == FileKind.Xml);

        Assert.Contains(result, x =>
            x.RelativePath == "Directory.Packages.props" &&
            x.Extension == KnownFileTypes.Props.Extension &&
            x.Kind == FileKind.Xml);

        Assert.Contains(result, x =>
            x.RelativePath == "Directory.Build.targets" &&
            x.Extension == KnownFileTypes.Targets.Extension &&
            x.Kind == FileKind.Xml);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_DirectoryBuildProps_File_Source_When_Props_Is_Enabled()
    {
        string root = CreateDirectory("msbuild-file-source");
        string filePath = CreateFile(root, "Directory.Build.props", "<Project />");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(KnownFileTypes.Props);
        var source = new MergeSource(filePath, MergeSourceType.File);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal("Directory.Build.props", result[0].RelativePath);
        Assert.Equal(KnownFileTypes.Props.Extension, result[0].Extension);
        Assert.Equal(FileKind.Xml, result[0].Kind);
    }

    [Fact]
    public void DiscoverFiles_Should_Return_EditorConfig_File_Source_When_EditorConfig_Is_Enabled()
    {
        string root = CreateDirectory("editorconfig-file-source");
        string filePath = CreateFile(root, ".editorconfig", "root = true");

        var service = new FileDiscoveryService();
        MergeProfile profile = CreateProfile(KnownFileTypes.EditorConfig);
        var source = new MergeSource(filePath, MergeSourceType.File);

        InputFile[] result = [.. service.DiscoverFiles([source], profile).InventoryFiles];

        Assert.Single(result);
        Assert.Equal(".editorconfig", result[0].Extension);
        Assert.Equal(FileKind.Text, result[0].Kind);
        Assert.Equal(".editorconfig", result[0].RelativePath);
    }

    [Fact]
    public void DiscoverFiles_Should_Retain_Unsupported_Text_File_When_Fallback_Is_Disabled()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "notes.foo", "hello");

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.Equal(".foo", file.Extension);
        Assert.Equal(FileKind.Unknown, file.Kind);
        Assert.False(file.IsMergeCandidate);
        Assert.False(file.IsIncluded);
        Assert.False(file.IsFallbackText);
        Assert.Equal("discovery.unsupported-file-type", file.SkipReason?.Code);
        Assert.Empty(result.MergeCandidates);
    }

    [Fact]
    public void DiscoverFiles_Should_Include_Unsupported_Text_File_When_Fallback_Is_Enabled()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "notes.foo", "hello");

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);
        InputFile file = Assert.Single(result.InventoryFiles);

        Assert.Equal(".foo", file.Extension);
        Assert.Equal(FileKind.Text, file.Kind);
        Assert.True(file.IsFallbackText);
        Assert.True(file.IsMergeCandidate);
        Assert.True(file.IsIncluded);
        Assert.Equal("notes.foo", file.RelativePath);
        Assert.Same(file, Assert.Single(result.MergeCandidates));
    }

    [Fact]
    public void DiscoverFiles_Should_Include_Unsupported_Text_File_From_File_Source_When_Fallback_Is_Enabled()
    {
        string root = CreateDirectory("project");
        string path = CreateFile(root, "notes.foo", "hello");

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource source = new(path, MergeSourceType.File);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);
        InputFile file = Assert.Single(result.InventoryFiles);

        Assert.Equal(".foo", file.Extension);
        Assert.Equal(FileKind.Text, file.Kind);
        Assert.True(file.IsFallbackText);
        Assert.True(file.IsMergeCandidate);
        Assert.Equal("notes.foo", file.RelativePath);
        Assert.Same(file, Assert.Single(result.MergeCandidates));
    }

    [Fact]
    public void DiscoverFiles_Should_Retain_Unsupported_Binary_File_Outside_MergeCandidates()
    {
        string root = CreateDirectory("project");
        string path = Path.Combine(root, "binary.foo");
        File.WriteAllBytes(path, "H\0e"u8);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.Equal(FileKind.Unknown, file.Kind);
        Assert.False(file.IsMergeCandidate);
        Assert.False(file.IsIncluded);
        Assert.False(file.IsFallbackText);
        Assert.Equal("discovery.unsupported-file-type", file.SkipReason?.Code);
        Assert.Empty(result.MergeCandidates);
    }

    [Fact]
    public void DiscoverFiles_Should_Retain_Unsupported_Large_File_Outside_MergeCandidates()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "large.foo", "1234567890");

        FileDiscoveryService service = new();

        MergeProfile profile = CreateProfileWithFallback(
            maxFileSizeBytes: 5,
            fileTypes:
            [
                new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)
            ]);

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.False(file.IsMergeCandidate);
        Assert.False(file.IsIncluded);
        Assert.False(file.IsFallbackText);
        Assert.Equal("discovery.unsupported-file-type", file.SkipReason?.Code);
        Assert.Empty(result.MergeCandidates);
    }

    [Fact]
    public void DiscoverFiles_Should_Retain_Disabled_Known_File_Type_Without_Fallback()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "config.json", "{ \"name\": \"test\" }");

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".json", "JSON", FileKind.Json, isEnabled: false));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.Equal(".json", file.Extension);
        Assert.Equal(FileKind.Json, file.Kind);
        Assert.False(file.IsMergeCandidate);
        Assert.False(file.IsIncluded);
        Assert.False(file.IsFallbackText);
        Assert.Equal("discovery.file-type-disabled", file.SkipReason?.Code);
        Assert.Empty(result.MergeCandidates);
    }

    [Fact]
    public void DiscoverFiles_Should_Match_Disabled_Known_File_Type_Case_Insensitively()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "CONFIG.JSON", "{ \"name\": \"test\" }");

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".json", "JSON", FileKind.Json, isEnabled: false));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.Equal(".JSON", file.Extension);
        Assert.Equal(FileKind.Json, file.Kind);
        Assert.False(file.IsMergeCandidate);
        Assert.False(file.IsFallbackText);
        Assert.Equal("discovery.file-type-disabled", file.SkipReason?.Code);
        Assert.Empty(result.MergeCandidates);
    }

    [Fact]
    public void DiscoverFiles_Should_Preserve_Original_Unsupported_Extension()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "notes.CUSTOM", "hello");

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);
        InputFile file = Assert.Single(result.InventoryFiles);

        Assert.Equal(".CUSTOM", file.Extension);
        Assert.True(file.IsFallbackText);
        Assert.True(file.IsMergeCandidate);
        Assert.Same(file, Assert.Single(result.MergeCandidates));
    }


    [Fact]
    public void DiscoverFiles_Should_Retain_Extensionless_File_Without_Changing_Fallback_Eligibility()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "LICENSE", "license text");

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.Equal("LICENSE", file.RelativePath);
        Assert.Equal(string.Empty, file.Extension);
        Assert.Equal(FileKind.Unknown, file.Kind);
        Assert.False(file.IsFallbackText);
        Assert.False(file.IsMergeCandidate);
        Assert.False(file.IsIncluded);
        Assert.Equal("discovery.unsupported-file-type", file.SkipReason?.Code);
        Assert.Empty(result.MergeCandidates);
    }

    [Fact]
    public void DiscoverFiles_Should_Classify_Mixed_Inventory_Without_Changing_MergeEligibility()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Enabled.cs", "class Enabled {}");
        CreateFile(root, "Disabled.json", "{}");
        CreateFile(root, "Notes.foo", "notes");
        File.WriteAllBytes(Path.Combine(root, "Binary.bin"), "A\0B"u8);
        CreateFile(root, "LICENSE", "license text");

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp),
            new FileTypeDefinition(".json", "JSON", FileKind.Json, isEnabled: false));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Equal(5, result.InventoryFiles.Count);
        Assert.Equal(2, result.MergeCandidates.Count);
        Assert.Contains(result.MergeCandidates, x => x.RelativePath == "Enabled.cs");
        Assert.Contains(result.MergeCandidates, x => x is { RelativePath: "Notes.foo", IsFallbackText: true });

        InputFile disabled = Assert.Single(result.InventoryFiles, x => x.RelativePath == "Disabled.json");
        Assert.False(disabled.IsMergeCandidate);
        Assert.Equal("discovery.file-type-disabled", disabled.SkipReason?.Code);

        InputFile binary = Assert.Single(result.InventoryFiles, x => x.RelativePath == "Binary.bin");
        Assert.False(binary.IsMergeCandidate);
        Assert.Equal("discovery.unsupported-file-type", binary.SkipReason?.Code);

        InputFile extensionless = Assert.Single(result.InventoryFiles, x => x.RelativePath == "LICENSE");
        Assert.False(extensionless.IsMergeCandidate);
        Assert.Equal(string.Empty, extensionless.Extension);
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Probe_Disabled_Known_File_Type_As_Fallback()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "config.json", "{}");

        var detector = new ThrowingUnsupportedTextFileDetector();
        FileDiscoveryService service = new(detector);
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".json", "JSON", FileKind.Json, isEnabled: false));

        MergeSource source = new(root, MergeSourceType.Directory);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.False(file.IsMergeCandidate);
        Assert.Equal("discovery.file-type-disabled", file.SkipReason?.Code);
        Assert.Empty(result.MergeCandidates);
    }

    [Fact]
    public void DiscoverFiles_Should_Report_Progress_When_Unsupported_Files_Are_Probed()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Notes.foo", "notes");
        CreateFile(root, "Config.custom", "config");

        var progressEvents = new List<FileDiscoveryProgress>();
        int detectorCalls = 0;

        var detector = new DelegatingUnsupportedTextFileDetector((_, _) =>
        {
            detectorCalls++;
            Assert.Equal(detectorCalls, progressEvents.Count);
            return true;
        });

        FileDiscoveryService service = new(detector);
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource source = new(root, MergeSourceType.Directory);
        var progress = new CapturingProgress<FileDiscoveryProgress>(progressEvents.Add);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile, progress);

        Assert.Equal(2, detectorCalls);
        Assert.Equal(2, progressEvents.Count);
        Assert.Equal(new[] { 1, 2 }, progressEvents.Select(x => x.ProbedFiles).ToArray());
        Assert.Contains(progressEvents, x => x.RelativePath == "Notes.foo");
        Assert.Contains(progressEvents, x => x.RelativePath == "Config.custom");

        Assert.Equal(2, result.MergeCandidates.Count);
        Assert.All(result.MergeCandidates, x => Assert.True(x.IsFallbackText));
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Report_Probe_Progress_When_Fallback_Is_Disabled()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Notes.foo", "notes");

        FileDiscoveryService service = new(new ThrowingUnsupportedTextFileDetector());
        MergeProfile profile = CreateProfile(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        MergeSource source = new(root, MergeSourceType.Directory);
        var progressEvents = new List<FileDiscoveryProgress>();
        var progress = new CapturingProgress<FileDiscoveryProgress>(progressEvents.Add);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile, progress);

        Assert.Empty(progressEvents);

        InputFile file = Assert.Single(result.InventoryFiles);
        Assert.False(file.IsMergeCandidate);
        Assert.Equal("discovery.unsupported-file-type", file.SkipReason?.Code);
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Report_Probe_Progress_For_Known_Or_Extensionless_Files()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Enabled.cs", "class Enabled {}");
        CreateFile(root, "Disabled.json", "{}");
        CreateFile(root, "LICENSE", "license text");

        FileDiscoveryService service = new(new ThrowingUnsupportedTextFileDetector());
        MergeProfile profile = CreateProfileWithFallback(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp),
            new FileTypeDefinition(".json", "JSON", FileKind.Json, isEnabled: false));

        MergeSource source = new(root, MergeSourceType.Directory);
        var progressEvents = new List<FileDiscoveryProgress>();
        var progress = new CapturingProgress<FileDiscoveryProgress>(progressEvents.Add);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile, progress);

        Assert.Empty(progressEvents);
        Assert.Equal(3, result.InventoryFiles.Count);
        Assert.Single(result.MergeCandidates);
        Assert.Contains(
            result.InventoryFiles,
            x => x is { RelativePath: "Disabled.json", IsMergeCandidate: false });

        Assert.Contains(
            result.InventoryFiles,
            x => x is { RelativePath: "LICENSE", IsMergeCandidate: false });
    }

    private string CreateDirectory(string name)
    {
        string path = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateFile(string directory, string fileName, string content)
    {
        string path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private static MergeProfile CreateProfile(
        IReadOnlyCollection<FileTypeDefinition> fileTypes,
        bool includeSourceExcludedFiles,
        SkippedFilesMetadataMode skippedFilesMetadataMode)
    {
        return new MergeProfile(
            name: "Test",
            generalOptions: new GeneralMergeOptions(
                outputMetadataOptions: new OutputMetadataOptions(
                    SkippedFilesMetadataMode: skippedFilesMetadataMode,
                    IncludeSourceExcludedFiles: includeSourceExcludedFiles)),
            csOptions: new CsMergeOptions(),
            fileTypes: fileTypes);
    }

    private static MergeProfile CreateProfileWithFallbackAndOutputMetadata(
        bool includeSourceExcludedFiles,
        SkippedFilesMetadataMode skippedFilesMetadataMode,
        params FileTypeDefinition[] fileTypes)
    {
        return new MergeProfile(
            name: "Test",
            generalOptions: new GeneralMergeOptions(
                unsupportedTextFallbackOptions: UnsupportedTextFallbackOptions.Enabled,
                outputMetadataOptions: new OutputMetadataOptions(
                    SkippedFilesMetadataMode: skippedFilesMetadataMode,
                    IncludeSourceExcludedFiles: includeSourceExcludedFiles)),
            csOptions: new CsMergeOptions(),
            fileTypes: fileTypes);
    }

    private static MergeProfile CreateProfileWithFallback(params FileTypeDefinition[] fileTypes)
    {
        return CreateProfileWithFallback(
            maxFileSizeBytes: UnsupportedTextFallbackOptions.DefaultMaxFileSizeBytes,
            fileTypes: fileTypes);
    }

    private static MergeProfile CreateProfileWithFallback(
        long maxFileSizeBytes,
        params FileTypeDefinition[] fileTypes)
    {
        return new MergeProfile(
            name: "Test profile",
            generalOptions: new GeneralMergeOptions(
                unsupportedTextFallbackOptions: new UnsupportedTextFallbackOptions(
                    isEnabled: true,
                    maxFileSizeBytes: maxFileSizeBytes)),
            csOptions: new CsMergeOptions(),
            fileTypes: fileTypes.Length > 0
                ? fileTypes
                : KnownFileTypes.Default,
            filterRules: [],
            transformations: []);
    }

    private static MergeProfile CreateProfile(params FileTypeDefinition[] fileTypes)
    {
        return new MergeProfile(
            name: "Test profile",
            generalOptions: new GeneralMergeOptions(),
            csOptions: new CsMergeOptions(),
            fileTypes: fileTypes);
    }

    private static MergeProfile CreateProfileWithRules(
        IReadOnlyCollection<FileTypeDefinition> fileTypes,
        IReadOnlyCollection<FileFilterRule> filterRules)
    {
        return new MergeProfile(
            name: "Test profile",
            generalOptions: new GeneralMergeOptions(),
            csOptions: new CsMergeOptions(),
            fileTypes: fileTypes,
            filterRules: filterRules);
    }

    private sealed class CapturingProgress<T>(Action<T> handler) : IProgress<T>
    {
        private readonly Action<T> _handler =
            handler ?? throw new ArgumentNullException(nameof(handler));

        public void Report(T value)
        {
            _handler(value);
        }
    }

    private sealed class DelegatingUnsupportedTextFileDetector(
        Func<string, UnsupportedTextFallbackOptions, bool> isTextCandidate) : IUnsupportedTextFileDetector
    {
        private readonly Func<string, UnsupportedTextFallbackOptions, bool> _isTextCandidate =
            isTextCandidate ?? throw new ArgumentNullException(nameof(isTextCandidate));

        public bool IsTextCandidate(string filePath, UnsupportedTextFallbackOptions options)
        {
            return _isTextCandidate(filePath, options);
        }
    }

    private sealed class ThrowingUnsupportedTextFileDetector : IUnsupportedTextFileDetector
    {
        public bool IsTextCandidate(string filePath, UnsupportedTextFallbackOptions options)
        {
            throw new InvalidOperationException("Disabled known file types must not be probed as fallback text.");
        }
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Report_Source_Excluded_Files_By_Default()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Included.cs", "class Included {}");
        CreateFile(root, "Excluded.cs", "class Excluded {}");

        MergeSource source = new(
            root,
            MergeSourceType.Directory,
            exclusions:
            [
                new MergeSourceExclusion("Excluded.cs", MergeSourceExclusionType.File)
            ]);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            new FileTypeDefinition(".cs", "C# source", FileKind.CSharp));

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Single(result.InventoryFiles);
        Assert.Empty(result.SourceExcludedFiles);
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Report_Source_Excluded_Files_When_Skipped_Metadata_Mode_Is_None()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Excluded.cs", "class Excluded {}");

        MergeSource source = new(
            root,
            MergeSourceType.Directory,
            exclusions:
            [
                new MergeSourceExclusion("Excluded.cs", MergeSourceExclusionType.File)
            ]);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            [new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)],
            includeSourceExcludedFiles: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.None);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Empty(result.InventoryFiles);
        Assert.Empty(result.SourceExcludedFiles);
    }

    [Fact]
    public void DiscoverFiles_Should_Report_Explicit_Source_Excluded_File_When_Enabled()
    {
        string root = CreateDirectory("project");
        string excludedPath = CreateFile(root, "Excluded.cs", "class Excluded {}");

        MergeSource source = new(
            root,
            MergeSourceType.Directory,
            exclusions:
            [
                new MergeSourceExclusion("Excluded.cs", MergeSourceExclusionType.File)
            ]);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            [new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)],
            includeSourceExcludedFiles: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Empty(result.InventoryFiles);
        Assert.Empty(result.MergeCandidates);

        InputFile excluded = Assert.Single(result.SourceExcludedFiles);
        Assert.Equal(excludedPath, excluded.FullPath);
        Assert.Equal("Excluded.cs", excluded.RelativePath);
        Assert.Equal(".cs", excluded.Extension);
        Assert.Equal(FileKind.Unknown, excluded.Kind);
        Assert.False(excluded.IsIncluded);
        Assert.False(excluded.IsMergeCandidate);
        Assert.Equal("source.exclude", excluded.SkipReason?.Code);
    }

    [Fact]
    public void DiscoverFiles_Should_Report_Files_Inside_Source_Excluded_Directory_When_Enabled()
    {
        string root = CreateDirectory("project");
        string generated = Directory.CreateDirectory(Path.Combine(root, "Generated")).FullName;
        string nested = Directory.CreateDirectory(Path.Combine(generated, "Nested")).FullName;
        CreateFile(generated, "A.cs", "class A {}");
        CreateFile(nested, "B.txt", "text");
        CreateFile(root, "Included.cs", "class Included {}");

        MergeSource source = new(
            root,
            MergeSourceType.Directory,
            isRecursive: true,
            exclusions:
            [
                new MergeSourceExclusion("Generated", MergeSourceExclusionType.Directory)
            ]);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            [
                new FileTypeDefinition(".cs", "C# source", FileKind.CSharp),
                new FileTypeDefinition(".txt", "Text", FileKind.Text)
            ],
            includeSourceExcludedFiles: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Single(result.InventoryFiles);
        Assert.Equal(2, result.SourceExcludedFiles.Count);
        Assert.Contains(result.SourceExcludedFiles, x => x.RelativePath == Path.Combine("Generated", "A.cs"));
        Assert.Contains(result.SourceExcludedFiles, x => x.RelativePath == Path.Combine("Generated", "Nested", "B.txt"));
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Report_Disabled_Source_Exclusion()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Excluded.cs", "class Excluded {}");

        MergeSource source = new(
            root,
            MergeSourceType.Directory,
            exclusions:
            [
                new MergeSourceExclusion(
                    "Excluded.cs",
                    MergeSourceExclusionType.File,
                    isEnabled: false)
            ]);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            [new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)],
            includeSourceExcludedFiles: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Single(result.InventoryFiles);
        Assert.Empty(result.SourceExcludedFiles);
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Traverse_Source_Excluded_Directory_For_NonRecursive_Source()
    {
        string root = CreateDirectory("project");
        string generated = Directory.CreateDirectory(Path.Combine(root, "Generated")).FullName;
        CreateFile(generated, "Nested.cs", "class Nested {}");

        MergeSource source = new(
            root,
            MergeSourceType.Directory,
            isRecursive: false,
            exclusions:
            [
                new MergeSourceExclusion("Generated", MergeSourceExclusionType.Directory)
            ]);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            [new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)],
            includeSourceExcludedFiles: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Empty(result.InventoryFiles);
        Assert.Empty(result.SourceExcludedFiles);
    }

    [Fact]
    public void DiscoverFiles_Should_Not_Probe_Source_Excluded_File_As_Unsupported_Text()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Excluded.foo", "text");

        var detector = new ThrowingUnsupportedTextFileDetector();
        FileDiscoveryService service = new(detector);

        MergeSource source = new(
            root,
            MergeSourceType.Directory,
            exclusions:
            [
                new MergeSourceExclusion("Excluded.foo", MergeSourceExclusionType.File)
            ]);

        MergeProfile profile = CreateProfileWithFallbackAndOutputMetadata(
            includeSourceExcludedFiles: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
            fileTypes:
            [
                new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)
            ]);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        InputFile excluded = Assert.Single(result.SourceExcludedFiles);
        Assert.Equal("source.exclude", excluded.SkipReason?.Code);
    }

    [Fact]
    public void DiscoverFiles_Should_Prefer_Normal_Inventory_When_Another_Source_Excludes_Same_File()
    {
        string root = CreateDirectory("project");
        string includedRoot = Directory.CreateDirectory(Path.Combine(root, "Sub")).FullName;
        string path = CreateFile(includedRoot, "Shared.cs", "class Shared {}");

        MergeSource excludingSource = new(
            root,
            MergeSourceType.Directory,
            isRecursive: true,
            exclusions:
            [
                new MergeSourceExclusion(Path.Combine("Sub", "Shared.cs"), MergeSourceExclusionType.File)
            ]);

        MergeSource includingSource = new(path, MergeSourceType.File);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            [new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)],
            includeSourceExcludedFiles: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple);

        FileDiscoveryResult result = service.DiscoverFiles(
            [excludingSource, includingSource],
            profile);

        Assert.Single(result.InventoryFiles);
        Assert.Single(result.MergeCandidates);
        Assert.Empty(result.SourceExcludedFiles);
    }

    [Fact]
    public void DiscoverFiles_Should_Deduplicate_Source_Excluded_Files()
    {
        string root = CreateDirectory("project");
        CreateFile(root, "Excluded.cs", "class Excluded {}");

        MergeSource source = new(
            root,
            MergeSourceType.Directory,
            exclusions:
            [
                new MergeSourceExclusion("Excluded.cs", MergeSourceExclusionType.File),
                new MergeSourceExclusion("excluded.cs", MergeSourceExclusionType.File)
            ]);

        FileDiscoveryService service = new();
        MergeProfile profile = CreateProfile(
            [new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)],
            includeSourceExcludedFiles: true,
            skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple);

        FileDiscoveryResult result = service.DiscoverFiles([source], profile);

        Assert.Single(result.SourceExcludedFiles);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}