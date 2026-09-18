using System.IO;
using System.Text;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Validation;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class MergeSessionValidatorTests : IDisposable
{
    private readonly string _outputRoot;
    private readonly string _tempRoot;

    public MergeSessionValidatorTests()
    {
        string testRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(MergeSessionValidatorTests),
            Guid.NewGuid().ToString("N"));

        _tempRoot = Path.Combine(testRoot, "Source");
        _outputRoot = Path.Combine(testRoot, "Output");

        Directory.CreateDirectory(_tempRoot);
        Directory.CreateDirectory(_outputRoot);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for tests.
        }
    }

    [Fact]
    public void Validate_Should_Return_Empty_When_Session_Is_Valid()
    {
        var validator = new MergeSessionValidator();
        MergeSession session = CreateValidSession();

        IReadOnlyCollection<ValidationIssue> result = validator.Validate(session);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_Should_Throw_When_Session_Is_Null()
    {
        var validator = new MergeSessionValidator();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));

        Assert.Equal("session", ex.ParamName);
    }

    [Fact]
    public void Validate_Should_Return_Error_When_Sources_Are_Empty()
    {
        var validator = new MergeSessionValidator();
        var session = new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources: [],
            profile: DefaultMergeProfiles.CreateDefault(),
            outputTarget: new OutputTarget(Path.Combine(_outputRoot, "merged.txt")));

        IReadOnlyCollection<ValidationIssue> result = validator.Validate(session);

        Assert.Contains(result, x => x is { Severity: ValidationSeverity.Error, Code: "session.sources.empty" });
    }

    [Fact]
    public void Validate_Should_Return_Error_When_No_Enabled_File_Types_Exist()
    {
        var validator = new MergeSessionValidator();

        FileTypeDefinition[] disabledFileTypes = KnownFileTypes.Default
            .Select(x => x with { IsEnabled = false })
            .ToArray();

        var profile = new MergeProfile(
            name: "No types",
            generalOptions: new GeneralMergeOptions(),
            fileTypes: disabledFileTypes);

        var session = new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources: [new MergeSource(_tempRoot, MergeSourceType.Directory)],
            profile: profile,
            outputTarget: new OutputTarget(Path.Combine(_outputRoot, "merged.txt")));

        IReadOnlyCollection<ValidationIssue> result = validator.Validate(session);

        Assert.Contains(result, x => x is { Severity: ValidationSeverity.Error, Code: "profile.fileTypes.empty" });
    }

    [Fact]
    public void Validate_Should_Return_Error_When_Source_Directory_Does_Not_Exist()
    {
        var validator = new MergeSessionValidator();

        string missingDirectory = Path.Combine(_tempRoot, "missing");

        var session = new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources:
            [
                new MergeSource(missingDirectory, MergeSourceType.Directory)
            ],
            profile: DefaultMergeProfiles.CreateDefault(),
            outputTarget: new OutputTarget(Path.Combine(_outputRoot, "merged.txt")));

        IReadOnlyCollection<ValidationIssue> result = validator.Validate(session);

        Assert.Contains(result, x => x is { Severity: ValidationSeverity.Error, Code: "source.directory.notFound" });
    }

    [Fact]
    public void Validate_Should_Return_Error_When_Source_File_Does_Not_Exist()
    {
        var validator = new MergeSessionValidator();

        string missingFile = Path.Combine(_tempRoot, "missing.cs");

        var session = new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources:
            [
                new MergeSource(missingFile, MergeSourceType.File)
            ],
            profile: DefaultMergeProfiles.CreateDefault(),
            outputTarget: new OutputTarget(Path.Combine(_outputRoot, "merged.txt")));

        IReadOnlyCollection<ValidationIssue> result = validator.Validate(session);

        Assert.Contains(result, x => x is { Severity: ValidationSeverity.Error, Code: "source.file.notFound" });
    }

    [Fact]
    public void Validate_Should_Ignore_Disabled_Sources()
    {
        var validator = new MergeSessionValidator();

        var session = new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources:
            [
                new MergeSource(Path.Combine(_tempRoot, "DisabledSource"), MergeSourceType.Directory, isEnabled: false),

                new MergeSource(_tempRoot, MergeSourceType.Directory)
            ],
            profile: DefaultMergeProfiles.CreateDefault(),
            outputTarget: new OutputTarget(Path.Combine(_outputRoot, "merged.txt")));

        IReadOnlyCollection<ValidationIssue> result = validator.Validate(session);

        Assert.DoesNotContain(
            result,
            x => x.Code == "source.directory.notFound" &&
                 x.Message.Contains("DisabledSource", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_Return_Warning_When_Output_Is_Inside_Source_Directory()
    {
        var validator = new MergeSessionValidator();

        var session = new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources:
            [
                new MergeSource(_tempRoot, MergeSourceType.Directory)
            ],
            profile: DefaultMergeProfiles.CreateDefault(),
            outputTarget: new OutputTarget(Path.Combine(_tempRoot, "merged.txt")));

        IReadOnlyCollection<ValidationIssue> result = validator.Validate(session);

        Assert.Contains(
            result,
            x => x is { Severity: ValidationSeverity.Warning, Code: "output.insideSourceDirectory" });
    }

    private MergeSession CreateValidSession()
    {
        return new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources:
            [
                new MergeSource(_tempRoot, MergeSourceType.Directory)
            ],
            profile: DefaultMergeProfiles.CreateDefault(),
            outputTarget: new OutputTarget(Path.Combine(_outputRoot, "merged.txt")));
    }
}