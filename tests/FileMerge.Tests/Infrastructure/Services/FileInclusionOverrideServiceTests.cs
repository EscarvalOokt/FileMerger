using System.IO;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Infrastructure.Services.Filtering;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class FileInclusionOverrideServiceTests
{
    [Fact]
    public void ApplyOverrides_Should_Throw_When_Files_Are_Null()
    {
        var service = new FileInclusionOverrideService();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => service.ApplyOverrides(null!, []));

        Assert.Equal("files", ex.ParamName);
    }

    [Fact]
    public void ApplyOverrides_Should_Throw_When_Overrides_Are_Null()
    {
        var service = new FileInclusionOverrideService();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => service.ApplyOverrides([], null!));

        Assert.Equal("overrides", ex.ParamName);
    }

    [Fact]
    public void ApplyOverrides_Should_Return_Original_Files_When_No_Overrides_Exist()
    {
        var service = new FileInclusionOverrideService();

        InputFile file = CreateFile(@"D:\Project\Test.cs", isIncluded: true);

        InputFile[] result = service.ApplyOverrides([file], []).ToArray();

        Assert.Single(result);
        Assert.Equal(file, result[0]);
        Assert.True(result[0].IsIncluded);
    }

    [Fact]
    public void ApplyOverrides_Should_Exclude_File_When_Override_Is_False()
    {
        var service = new FileInclusionOverrideService();

        InputFile file = CreateFile(@"D:\Project\Test.cs", isIncluded: true);

        InputFile result = service.ApplyOverrides([file], [new FileInclusionOverride(file.FullPath, false)]).Single();

        Assert.False(result.IsIncluded);
        Assert.NotNull(result.SkipReason);
        Assert.Equal("manual.exclude", result.SkipReason!.Code);
    }

    [Fact]
    public void ApplyOverrides_Should_Include_File_When_Override_Is_True()
    {
        var service = new FileInclusionOverrideService();

        InputFile file = CreateFile(@"D:\Project\Test.cs", isIncluded: false);

        InputFile result = service.ApplyOverrides([file], [new FileInclusionOverride(file.FullPath, true)]).Single();

        Assert.True(result.IsIncluded);
        Assert.Null(result.SkipReason);
    }

    [Fact]
    public void ApplyOverrides_Should_Use_Last_Override_When_Duplicates_Exist()
    {
        var service = new FileInclusionOverrideService();

        InputFile file = CreateFile(@"D:\Project\Test.cs", isIncluded: true);

        FileInclusionOverride[] overrides = new[]
        {
            new FileInclusionOverride(file.FullPath, false),
            new FileInclusionOverride(file.FullPath, true)
        };

        InputFile result = service.ApplyOverrides([file], overrides).Single();

        Assert.True(result.IsIncluded);
        Assert.Null(result.SkipReason);
    }

    [Fact]
    public void ApplyOverrides_Should_Ignore_Overrides_For_Unknown_Files()
    {
        var service = new FileInclusionOverrideService();

        InputFile file = CreateFile(@"D:\Project\Test.cs", isIncluded: true);

        InputFile result = service.ApplyOverrides([file], [new FileInclusionOverride(@"D:\Project\Unknown.cs", false)])
            .Single();

        Assert.True(result.IsIncluded);
        Assert.Null(result.SkipReason);
    }

    private static InputFile CreateFile(string path, bool isIncluded)
    {
        return new InputFile(
            fullPath: path,
            relativePath: Path.GetFileName(path),
            extension: ".cs",
            kind: FileKind.CSharp,
            isIncluded: isIncluded);
    }
}