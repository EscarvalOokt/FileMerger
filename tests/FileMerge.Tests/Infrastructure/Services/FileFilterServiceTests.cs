using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Filtering;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class FileFilterServiceTests
{
    [Fact]
    public void ApplyFilters_Should_Throw_When_Files_Are_Null()
    {
        var service = new FileFilterService();
        MergeProfile profile = CreateProfile();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => service.ApplyFilters(null!, profile));

        Assert.Equal("files", ex.ParamName);
    }

    [Fact]
    public void ApplyFilters_Should_Throw_When_Profile_Is_Null()
    {
        var service = new FileFilterService();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => service.ApplyFilters([], null!));

        Assert.Equal("profile", ex.ParamName);
    }

    [Fact]
    public void ApplyFilters_Should_Exclude_File_By_DirectorySegment_Rule_For_Bin()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.DirectorySegment,
                    patternType: RulePatternType.Exact,
                    pattern: "bin",
                    description: "Exclude bin directory")
            ]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\bin\Debug\Test.cs",
            relativePath: @"bin\Debug\Test.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("filter.rule.exclude", result.SkipReason!.Code);
        Assert.Equal("Exclude bin directory", result.SkipReason.Description);
    }

    [Fact]
    public void ApplyFilters_Should_Exclude_File_By_DirectorySegment_Rule_For_Obj()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.DirectorySegment,
                    patternType: RulePatternType.Exact,
                    pattern: "obj",
                    description: "Exclude obj directory")
            ]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\obj\Debug\Test.cs",
            relativePath: @"obj\Debug\Test.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("filter.rule.exclude", result.SkipReason!.Code);
        Assert.Equal("Exclude obj directory", result.SkipReason.Description);
    }

    [Fact]
    public void ApplyFilters_Should_Exclude_File_By_Wildcard_Rule_For_Designer_File()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.FileName,
                    patternType: RulePatternType.Wildcard,
                    pattern: "*.Designer.cs",
                    description: "Exclude designer files")
            ]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\Views\MainWindow.Designer.cs",
            relativePath: @"Views\MainWindow.Designer.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("filter.rule.exclude", result.SkipReason!.Code);
        Assert.Equal("Exclude designer files", result.SkipReason.Description);
    }

    [Fact]
    public void ApplyFilters_Should_Exclude_File_By_Exact_FileName_Rule_For_AssemblyInfo()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.FileName,
                    patternType: RulePatternType.Exact,
                    pattern: "AssemblyInfo.cs",
                    description: "Exclude AssemblyInfo")
            ]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\Properties\AssemblyInfo.cs",
            relativePath: @"Properties\AssemblyInfo.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("filter.rule.exclude", result.SkipReason!.Code);
        Assert.Equal("Exclude AssemblyInfo", result.SkipReason.Description);
    }

    [Fact]
    public void ApplyFilters_Should_Not_Exclude_File_When_No_FilterRule_Matches()
    {
        var service = new FileFilterService();
        MergeProfile profile = CreateProfile();

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\Services\UserService.cs",
            relativePath: @"Services\UserService.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.True(result.IsIncluded);
        Assert.Null(result.SkipReason);
    }

    [Fact]
    public void ApplyFilters_Should_Not_Exclude_NonCSharp_File_When_No_FilterRule_Matches()
    {
        var service = new FileFilterService();
        MergeProfile profile = CreateProfile(
            fileTypes:
            [
                new FileTypeDefinition(".cs", "C# source", FileKind.CSharp),
                new FileTypeDefinition(".json", "JSON", FileKind.Json)
            ]);

        var file = new InputFile(
            fullPath: @"D:\Project\bin\settings.json",
            relativePath: @"bin\settings.json",
            extension: ".json",
            kind: FileKind.Json);

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.True(result.IsIncluded);
        Assert.Null(result.SkipReason);
    }

    [Fact]
    public void ApplyFilters_Should_Exclude_File_By_Wildcard_Rule()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.FileName,
                    patternType: RulePatternType.Wildcard,
                    pattern: "*.g.cs",
                    description: "Generated")
            ]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\Generated\View.g.cs",
            relativePath: @"Generated\View.g.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("filter.rule.exclude", result.SkipReason!.Code);
        Assert.Equal("Generated", result.SkipReason.Description);
    }

    [Fact]
    public void ApplyFilters_Should_Exclude_File_By_DirectorySegment_Exact_Rule()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.DirectorySegment,
                    patternType: RulePatternType.Exact,
                    pattern: "Generated")
            ]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\Generated\Test.cs",
            relativePath: @"Generated\Test.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("filter.rule.exclude", result.SkipReason!.Code);
    }

    [Fact]
    public void ApplyFilters_Should_Sort_By_RelativePath_Ascending()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            generalOptions: new GeneralMergeOptions(sortMode: SortMode.ByRelativePathAscending));

        InputFile[] files =
        [
            CreateInputFile(@"D:\Project\B.cs", "B.cs"),
            CreateInputFile(@"D:\Project\A.cs", "A.cs"),
            CreateInputFile(@"D:\Project\C.cs", "C.cs")
        ];

        InputFile[] result = [.. service.ApplyFilters(files, profile)];

        Assert.Equal("A.cs", result[0].RelativePath);
        Assert.Equal("B.cs", result[1].RelativePath);
        Assert.Equal("C.cs", result[2].RelativePath);
    }

    [Fact]
    public void ApplyFilters_Should_Sort_By_RelativePath_Descending()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            generalOptions: new GeneralMergeOptions(sortMode: SortMode.ByRelativePathDescending));

        InputFile[] files =
        [
            CreateInputFile(@"D:\Project\B.cs", "B.cs"),
            CreateInputFile(@"D:\Project\A.cs", "A.cs"),
            CreateInputFile(@"D:\Project\C.cs", "C.cs")
        ];

        InputFile[] result = [.. service.ApplyFilters(files, profile)];

        Assert.Equal("C.cs", result[0].RelativePath);
        Assert.Equal("B.cs", result[1].RelativePath);
        Assert.Equal("A.cs", result[2].RelativePath);
    }


    [Fact]
    public void ApplyFilters_Should_Match_DirectorySegment_Rules_Case_Insensitively()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.DirectorySegment,
                    patternType: RulePatternType.Exact,
                    pattern: "bin",
                    description: "Exclude bin directory")
            ]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\Bin\Debug\Test.cs",
            relativePath: @"Bin\Debug\Test.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("filter.rule.exclude", result.SkipReason!.Code);
        Assert.Equal("Exclude bin directory", result.SkipReason.Description);
    }

    [Fact]
    public void ApplyFilters_Should_Not_Apply_Disabled_FilterRule()
    {
        var service = new FileFilterService();

        MergeProfile profile = CreateProfile(
            filterRules:
            [
                new FileFilterRule(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.FileName,
                    patternType: RulePatternType.Wildcard,
                    pattern: "*.Designer.cs",
                    isEnabled: false,
                    description: "Exclude designer files")
            ]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\Views\MainWindow.Designer.cs",
            relativePath: @"Views\MainWindow.Designer.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.True(result.IsIncluded);
        Assert.Null(result.SkipReason);
    }

    [Fact]
    public void ApplyFilters_Should_Add_Rule_Details_To_Filter_Exclusion()
    {
        FileFilterService service = new();

        FileFilterRule rule = new(
            mode: FilterMode.Exclude,
            target: FilterTarget.FileName,
            patternType: RulePatternType.Wildcard,
            pattern: "*.Designer.cs",
            description: "Exclude designer files");

        MergeProfile profile = CreateProfile(filterRules: [rule]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\MainWindow.Designer.cs",
            relativePath: "MainWindow.Designer.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.NotNull(result.SkipReason!.RuleDetails);

        Assert.Equal("filter.rule.exclude", result.SkipReason.Code);
        Assert.Equal("Exclude designer files", result.SkipReason.Description);
        Assert.Equal(FilterMode.Exclude, result.SkipReason.RuleDetails!.Mode);
        Assert.Equal(FilterTarget.FileName, result.SkipReason.RuleDetails.Target);
        Assert.Equal(RulePatternType.Wildcard, result.SkipReason.RuleDetails.PatternType);
        Assert.Equal("*.Designer.cs", result.SkipReason.RuleDetails.Pattern);
        Assert.Equal("Exclude designer files", result.SkipReason.RuleDetails.Description);
    }

    [Fact]
    public void ApplyFilters_Should_Keep_Rule_Details_When_Description_Is_Missing()
    {
        FileFilterService service = new();

        FileFilterRule rule = new(
            mode: FilterMode.Exclude,
            target: FilterTarget.DirectorySegment,
            patternType: RulePatternType.Exact,
            pattern: "Generated");

        MergeProfile profile = CreateProfile(filterRules: [rule]);

        InputFile file = CreateInputFile(
            fullPath: @"D:\Project\Generated\Test.cs",
            relativePath: @"Generated\Test.cs");

        InputFile result = service.ApplyFilters([file], profile).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.NotNull(result.SkipReason!.RuleDetails);

        Assert.Equal("filter.rule.exclude", result.SkipReason.Code);
        Assert.Equal("Excluded by rule: Generated", result.SkipReason.Description);
        Assert.Equal(FilterMode.Exclude, result.SkipReason.RuleDetails!.Mode);
        Assert.Equal(FilterTarget.DirectorySegment, result.SkipReason.RuleDetails.Target);
        Assert.Equal(RulePatternType.Exact, result.SkipReason.RuleDetails.PatternType);
        Assert.Equal("Generated", result.SkipReason.RuleDetails.Pattern);
        Assert.Null(result.SkipReason.RuleDetails.Description);
    }

    private static InputFile CreateInputFile(string fullPath, string relativePath)
    {
        return new InputFile(
            fullPath: fullPath,
            relativePath: relativePath,
            extension: ".cs",
            kind: FileKind.CSharp);
    }

    private static MergeProfile CreateProfile(
        GeneralMergeOptions? generalOptions = null,
        CsMergeOptions? csOptions = null,
        IReadOnlyCollection<FileFilterRule>? filterRules = null,
        IReadOnlyCollection<FileTypeDefinition>? fileTypes = null)
    {
        return new MergeProfile(
            name: "Test profile",
            generalOptions: generalOptions ?? new GeneralMergeOptions(),
            csOptions: csOptions ?? new CsMergeOptions(),
            fileTypes: fileTypes ??
            [
                new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)
            ],
            filterRules: filterRules,
            transformations: []);
    }
}