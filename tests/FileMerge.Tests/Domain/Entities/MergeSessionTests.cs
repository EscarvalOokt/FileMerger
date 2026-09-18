using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.Entities;

public sealed class MergeSessionTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        MergeProfile profile = CreateProfile();
        var outputTarget = new OutputTarget(@"D:\C# Repository\Tests\FileMerger\out\merged.txt");
        var source = new MergeSource(@"D:\C# Repository\Tests\FileMerger", MergeSourceType.Directory);

        var file = new InputFile(@"D:\C# Repository\Tests\FileMerger\Test.cs", "Test.cs", ".cs", FileKind.CSharp);
        var issue = new ValidationIssue(ValidationSeverity.Warning, "warn", "warning");
        var statistics = new MergeStatistics(1, 1, 0, 10, TimeSpan.Zero);
        var output = new MergeOutput("content", [], statistics, DateTime.UtcNow);

        var result = new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources: [source],
            profile: profile,
            outputTarget: outputTarget,
            files: [file],
            validationIssues: [issue],
            lastOutput: output);

        Assert.Equal("Session 1", result.Name);
        Assert.Single(result.Sources);
        Assert.Equal(source, result.Sources.Single());
        Assert.Equal(profile, result.Profile);
        Assert.Equal(outputTarget, result.OutputTarget);
        Assert.Single(result.Files);
        Assert.Equal(file, result.Files.Single());
        Assert.Single(result.ValidationIssues);
        Assert.Equal(issue, result.ValidationIssues.Single());
        Assert.Equal(output, result.LastOutput);
    }

    [Fact]
    public void Constructor_Should_Initialize_Empty_Collections_When_Null_Is_Passed()
    {
        var result = new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources: [new MergeSource(@"D:\C# Repository\Tests\FileMerger", MergeSourceType.Directory)],
            profile: CreateProfile(),
            outputTarget: new OutputTarget(@"D:\C# Repository\Tests\FileMerger\out\merged.txt"),
            files: null,
            validationIssues: null,
            lastOutput: null);

        Assert.NotNull(result.Files);
        Assert.Empty(result.Files);
        Assert.NotNull(result.ValidationIssues);
        Assert.Empty(result.ValidationIssues);
        Assert.Null(result.LastOutput);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Id_Is_Empty()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new MergeSession(
            id: Guid.Empty,
            name: "Session 1",
            sources: [new MergeSource(@"D:\C# Repository\Tests\FileMerger", MergeSourceType.Directory)],
            profile: CreateProfile(),
            outputTarget: new OutputTarget(@"D:\C# Repository\Tests\FileMerger\out\merged.txt")));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Should_Throw_When_Name_Is_Invalid(string? name)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new MergeSession(
            id: Guid.NewGuid(),
            name: name!,
            sources: [new MergeSource(@"D:\C# Repository\Tests\FileMerger", MergeSourceType.Directory)],
            profile: CreateProfile(),
            outputTarget: new OutputTarget(@"D:\C# Repository\Tests\FileMerger\out\merged.txt")));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Sources_Are_Null()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources: null!,
            profile: CreateProfile(),
            outputTarget: new OutputTarget(@"D:\C# Repository\Tests\FileMerger\out\merged.txt")));

        Assert.Equal("sources", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Profile_Is_Null()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources: [new MergeSource(@"D:\C# Repository\Tests\FileMerger", MergeSourceType.Directory)],
            profile: null!,
            outputTarget: new OutputTarget(@"D:\C# Repository\Tests\FileMerger\out\merged.txt")));

        Assert.Equal("profile", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_OutputTarget_Is_Null()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources: [new MergeSource(@"D:\C# Repository\Tests\FileMerger", MergeSourceType.Directory)],
            profile: CreateProfile(),
            outputTarget: null!));

        Assert.Equal("outputTarget", ex.ParamName);
    }

    [Fact]
    public void WithFiles_Should_Return_New_Instance_With_New_Files()
    {
        MergeSession session = CreateSession();
        InputFile[] newFiles =
        [
            new InputFile(@"D:\C# Repository\Tests\FileMerger\New.cs", "New.cs", ".cs", FileKind.CSharp)
        ];

        MergeSession result = session.WithFiles(newFiles);

        Assert.NotSame(session, result);
        Assert.Empty(session.Files);
        Assert.Single(result.Files);
        Assert.Equal("New.cs", result.Files.Single().RelativePath);
    }

    [Fact]
    public void WithFiles_Should_Throw_When_Files_Are_Null()
    {
        MergeSession session = CreateSession();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => session.WithFiles(null!));

        Assert.Equal("files", ex.ParamName);
    }

    [Fact]
    public void WithValidationIssues_Should_Return_New_Instance_With_New_Issues()
    {
        MergeSession session = CreateSession();
        ValidationIssue[] issues =
        [
            new ValidationIssue(ValidationSeverity.Error, "err", "error")
        ];

        MergeSession result = session.WithValidationIssues(issues);

        Assert.NotSame(session, result);
        Assert.Empty(session.ValidationIssues);
        Assert.Single(result.ValidationIssues);
        Assert.Equal("err", result.ValidationIssues.Single().Code);
    }

    [Fact]
    public void WithValidationIssues_Should_Throw_When_Issues_Are_Null()
    {
        MergeSession session = CreateSession();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => session.WithValidationIssues(null!));

        Assert.Equal("issues", ex.ParamName);
    }

    [Fact]
    public void WithLastOutput_Should_Return_New_Instance_With_New_Output()
    {
        MergeSession session = CreateSession();
        var output = new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(1, 1, 0, 6, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow);

        MergeSession result = session.WithLastOutput(output);

        Assert.NotSame(session, result);
        Assert.Null(session.LastOutput);
        Assert.Equal(output, result.LastOutput);
    }

    [Fact]
    public void WithLastOutput_Should_Throw_When_Output_Is_Null()
    {
        MergeSession session = CreateSession();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => session.WithLastOutput(null!));

        Assert.Equal("output", ex.ParamName);
    }

    private static MergeSession CreateSession()
    {
        return new MergeSession(
            id: Guid.NewGuid(),
            name: "Session",
            sources: [new MergeSource(@"D:\C# Repository\Tests\FileMerger", MergeSourceType.Directory)],
            profile: CreateProfile(),
            outputTarget: new OutputTarget(@"D:\C# Repository\Tests\FileMerger\out\merged.txt"));
    }

    private static MergeProfile CreateProfile()
    {
        return new MergeProfile(name: "Default", generalOptions: new GeneralMergeOptions());
    }
}