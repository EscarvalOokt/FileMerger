using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Services.Merge;

namespace FileMerger.Tests.Infrastructure.Services;

public sealed class MergeBuilderTests
{
    [Fact]
    public void Build_Should_Throw_When_Session_Is_Null()
    {
        var builder = new MergeBuilder();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            builder.Build(null!, [], TimeSpan.Zero));

        Assert.Equal("session", ex.ParamName);
    }

    [Fact]
    public void Build_Should_Throw_When_Sections_Are_Null()
    {
        var builder = new MergeBuilder();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            builder.Build(CreateSession(true), null!, TimeSpan.Zero));

        Assert.Equal("sections", ex.ParamName);
    }

    [Fact]
    public void Build_Should_Order_Sections_By_Order()
    {
        var builder = new MergeBuilder();
        MergeSession session = CreateSession(includeHeaderComment: false);

        var sectionA = new MergeSection(
            sourceFile: CreateFile("A.cs"),
            content: "A",
            order: 1);

        var sectionB = new MergeSection(
            sourceFile: CreateFile("B.cs"),
            content: "B",
            order: 0);

        MergeOutput result = builder.Build(session, [sectionA, sectionB], TimeSpan.Zero);

        Assert.Equal("B\r\n\r\nA".Replace("\r\n", Environment.NewLine), result.Content);
        Assert.Equal("B.cs", result.Sections.ElementAt(0).SourceFile.RelativePath);
        Assert.Equal("A.cs", result.Sections.ElementAt(1).SourceFile.RelativePath);
    }

    [Fact]
    public void Build_Should_Include_Header_Comment_When_Enabled()
    {
        var builder = new MergeBuilder();
        MergeSession session = CreateSession(includeHeaderComment: true);

        var section = new MergeSection(
            sourceFile: CreateFile("Test.cs"),
            content: "class Test {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.Contains("// Auto-generated merged source file", result.Content);
        Assert.Contains("// Session: Session 1", result.Content);
        Assert.Contains("// Output: D:\\Output\\merged.txt", result.Content);
        Assert.Contains("class Test {}", result.Content);
    }

    [Fact]
    public void Build_Should_Render_Section_Header_When_HeaderText_Is_Present()
    {
        var builder = new MergeBuilder();
        MergeSession session = CreateSession(includeHeaderComment: false);

        var section = new MergeSection(
            sourceFile: CreateFile("Test.cs"),
            content: "class Test {}",
            order: 0,
            headerText: "// FILE: Test.cs");

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.Contains("// -----------------------------------------------", result.Content);
        Assert.Contains("// FILE: Test.cs", result.Content);
        Assert.Contains("class Test {}", result.Content);
    }

    [Fact]
    public void Build_Should_Set_Statistics_From_Session_Files()
    {
        var builder = new MergeBuilder();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile skippedFile = CreateFile("Skipped.cs").Exclude(new SkipReason("skip", "Skipped"));

        MergeSession session = CreateSession(includeHeaderComment: false)
            .WithFiles([includedFile, skippedFile]);

        var section = new MergeSection(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.Equal(2, result.Statistics.FilesScanned);
        Assert.Equal(1, result.Statistics.FilesIncluded);
        Assert.Equal(1, result.Statistics.FilesSkipped);
        Assert.True(result.Statistics.TotalCharacters > 0);
        Assert.Equal(session.OutputTarget, result.OutputTarget);
    }

    [Fact]
    public void Build_Should_Set_Duration_From_TotalDuration()
    {
        var builder = new MergeBuilder();
        var duration = TimeSpan.FromMilliseconds(123);
        MergeSession session = CreateSession(includeHeaderComment: false);

        var section = new MergeSection(
            sourceFile: CreateFile("Test.cs"),
            content: "class Test {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], duration);

        Assert.Equal(duration, result.Statistics.Duration);
    }

    [Fact]
    public void Build_Should_Not_Render_Excluded_Files_When_Metadata_Mode_Is_None()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile excludedFile = CreateFile("Excluded.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.None)
            .WithFiles([includedFile, excludedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.DoesNotContain("// Skipped files:", result.Content);
        Assert.DoesNotContain("Excluded.cs", result.Content);
    }

    [Fact]
    public void Build_Should_Render_Excluded_File_Paths_When_Metadata_Mode_Is_Simple()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile excludedFile = CreateFile("Excluded.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple)
            .WithFiles([includedFile, excludedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.Contains("// Skipped files:", result.Content);
        Assert.Contains("// - Excluded.cs", result.Content);
        Assert.DoesNotContain("manual.exclude", result.Content);
        Assert.DoesNotContain("//   Rule:", result.Content);
    }

    [Fact]
    public void Build_Should_Render_Excluded_File_Reasons_When_Metadata_Mode_Is_Detailed()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile excludedFile = CreateFile("Excluded.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed)
            .WithFiles([includedFile, excludedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.Contains("// Skipped files:", result.Content);
        Assert.Contains("// - Excluded.cs", result.Content);
        Assert.Contains("//   Reason: manual.exclude — Excluded manually by user.", result.Content);
        Assert.DoesNotContain("//   Rule:", result.Content);
    }

    [Fact]
    public void Build_Should_Render_Filter_Rule_Details_When_Metadata_Mode_Is_Detailed()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile excludedFile = CreateFile("Generated/View.g.cs")
            .Exclude(new SkipReason(
                code: "filter.rule.exclude",
                description: "Exclude generated files",
                ruleDetails: new SkipRuleDetails(
                    mode: FilterMode.Exclude,
                    target: FilterTarget.FileName,
                    patternType: RulePatternType.Wildcard,
                    pattern: "*.g.cs",
                    description: "Exclude generated files")));

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed)
            .WithFiles([includedFile, excludedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.Contains("// - Generated/View.g.cs", result.Content);
        Assert.Contains("//   Reason: filter.rule.exclude — Exclude generated files", result.Content);
        Assert.Contains("//   Rule: exclude FileName Wildcard \"*.g.cs\"", result.Content);
    }

    [Fact]
    public void Build_Should_Not_List_Included_Files_In_Excluded_Metadata()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile excludedFile = CreateFile("Excluded.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple)
            .WithFiles([includedFile, excludedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.DoesNotContain("// - Included.cs", result.Content);
        Assert.Contains("// - Excluded.cs", result.Content);
    }

    [Fact]
    public void Build_Should_Render_Excluded_Files_None_When_Metadata_Mode_Enabled_And_No_Excluded_Files()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.Contains("// Skipped files: none", result.Content);
    }

    [Fact]
    public void Build_Should_Order_Excluded_Files_By_RelativePath()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile excludedB = CreateFile("B.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));
        InputFile excludedA = CreateFile("A.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple)
            .WithFiles([includedFile, excludedB, excludedA]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        int indexA = result.Content.IndexOf("// - A.cs", StringComparison.Ordinal);
        int indexB = result.Content.IndexOf("// - B.cs", StringComparison.Ordinal);

        Assert.True(indexA >= 0);
        Assert.True(indexB >= 0);
        Assert.True(indexA < indexB);
    }

    [Fact]
    public void Build_Should_Not_Render_Source_Excluded_Files_When_Option_Is_Disabled()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile sourceExcluded = CreateSourceExcludedFile("Generated.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
                includeSourceExcludedFiles: false)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(
            session,
            [section],
            TimeSpan.Zero,
            [sourceExcluded]);

        Assert.Contains("// Skipped files: none", result.Content);
        Assert.DoesNotContain("Generated.cs", result.Content);
    }

    [Fact]
    public void Build_Should_Not_Render_Source_Excluded_Files_When_Metadata_Mode_Is_None()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile sourceExcluded = CreateSourceExcludedFile("Generated.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.None,
                includeSourceExcludedFiles: true)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(
            session,
            [section],
            TimeSpan.Zero,
            [sourceExcluded]);

        Assert.DoesNotContain("// Skipped files:", result.Content);
        Assert.DoesNotContain("Generated.cs", result.Content);
    }

    [Fact]
    public void Build_Should_Render_Source_Excluded_File_Path_When_Metadata_Mode_Is_Simple()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile sourceExcluded = CreateSourceExcludedFile("Generated.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
                includeSourceExcludedFiles: true)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(
            session,
            [section],
            TimeSpan.Zero,
            [sourceExcluded]);

        Assert.Contains("// - Generated.cs", result.Content);
        Assert.DoesNotContain("source.exclude", result.Content);
    }

    [Fact]
    public void Build_Should_Render_Source_Excluded_File_Reason_When_Metadata_Mode_Is_Detailed()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile sourceExcluded = CreateSourceExcludedFile("Generated.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                includeSourceExcludedFiles: true)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(
            session,
            [section],
            TimeSpan.Zero,
            [sourceExcluded]);

        Assert.Contains("// - Generated.cs", result.Content);
        Assert.Contains(
            "//   Reason: source.exclude — Excluded by a source-specific exclusion.",
            result.Content);
    }

    [Fact]
    public void Build_Should_Order_Regular_And_Source_Excluded_Files_Together()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile regularSkipped = CreateFile("B.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));
        InputFile sourceExcluded = CreateSourceExcludedFile("A.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
                includeSourceExcludedFiles: true)
            .WithFiles([includedFile, regularSkipped]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(
            session,
            [section],
            TimeSpan.Zero,
            [sourceExcluded]);

        int indexA = result.Content.IndexOf("// - A.cs", StringComparison.Ordinal);
        int indexB = result.Content.IndexOf("// - B.cs", StringComparison.Ordinal);

        Assert.True(indexA >= 0);
        Assert.True(indexB >= 0);
        Assert.True(indexA < indexB);
    }

    [Fact]
    public void Build_Should_Deduplicate_Source_Excluded_File_Against_Session_File()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile regularSkipped = CreateFile("Shared.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));
        InputFile sourceExcluded = CreateSourceExcludedFile("Shared.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                includeSourceExcludedFiles: true)
            .WithFiles([includedFile, regularSkipped]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(
            session,
            [section],
            TimeSpan.Zero,
            [sourceExcluded]);

        Assert.Equal(1, CountOccurrences(result.Content, "// - Shared.cs"));
        Assert.Contains("manual.exclude", result.Content);
        Assert.DoesNotContain("source.exclude", result.Content);
    }

    [Fact]
    public void Build_Should_Not_Render_Build_Timestamp_When_Metadata_Toggle_Is_Disabled()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                includeBuildTimestampMetadata: false)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.DoesNotContain("// Date (UTC):", result.Content);
    }

    [Fact]
    public void Build_Should_Not_Render_Session_Name_When_Metadata_Toggle_Is_Disabled()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                includeSessionNameMetadata: false)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.DoesNotContain("// Session:", result.Content);
    }

    [Fact]
    public void Build_Should_Not_Render_Output_Path_When_Metadata_Toggle_Is_Disabled()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                includeOutputPathMetadata: false)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.DoesNotContain("// Output:", result.Content);
    }

    [Fact]
    public void Build_Should_Not_Render_File_Summary_When_Metadata_Toggle_Is_Disabled()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile skippedFile = CreateFile("Skipped.cs")
            .Exclude(new SkipReason("manual.exclude", "Excluded manually by user."));

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                includeFileSummaryMetadata: false)
            .WithFiles([includedFile, skippedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.DoesNotContain("// Files included:", result.Content);
        Assert.DoesNotContain("// Files skipped:", result.Content);
    }

    [Fact]
    public void Build_Should_Render_Header_Frame_When_All_Metadata_Toggles_Are_Disabled()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                includeBuildTimestampMetadata: false,
                includeSessionNameMetadata: false,
                includeOutputPathMetadata: false,
                includeFileSummaryMetadata: false)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.Contains("// ===============================================", result.Content);
        Assert.Contains("// Auto-generated merged source file", result.Content);
        Assert.DoesNotContain("// Date (UTC):", result.Content);
        Assert.DoesNotContain("// Session:", result.Content);
        Assert.DoesNotContain("// Output:", result.Content);
        Assert.DoesNotContain("// Files included:", result.Content);
        Assert.DoesNotContain("// Files skipped:", result.Content);
    }

    [Fact]
    public void Build_Should_Set_GeneratedAtUtc_And_Render_Date_When_Metadata_Toggle_Is_Enabled()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                includeBuildTimestampMetadata: true)
            .WithFiles([includedFile]);

        MergeSection section = new(
            sourceFile: includedFile,
            content: "class Included {}",
            order: 0);

        MergeOutput result = builder.Build(session, [section], TimeSpan.Zero);

        Assert.NotEqual(default, result.GeneratedAtUtc);
        Assert.Contains("// Date (UTC):", result.Content);
    }

    private static MergeSession CreateSession(
        bool includeHeaderComment,
        SkippedFilesMetadataMode skippedFilesMetadataMode = SkippedFilesMetadataMode.None,
        bool includeBuildTimestampMetadata = true,
        bool includeSessionNameMetadata = true,
        bool includeOutputPathMetadata = true,
        bool includeFileSummaryMetadata = true,
        bool includeSourceExcludedFiles = false)
    {
        MergeProfile profile = new(
            name: "Default",
            generalOptions: new GeneralMergeOptions(
                includeHeaderComment: includeHeaderComment,
                outputMetadataOptions: new OutputMetadataOptions(
                    IncludeBuildTimestamp: includeBuildTimestampMetadata,
                    IncludeSessionName: includeSessionNameMetadata,
                    IncludeOutputPath: includeOutputPathMetadata,
                    IncludeFileSummary: includeFileSummaryMetadata,
                    SkippedFilesMetadataMode: skippedFilesMetadataMode,
                    IncludeSourceExcludedFiles: includeSourceExcludedFiles)),
            csOptions: new CsMergeOptions(),
            fileTypes:
            [
                new FileTypeDefinition(".cs", "C# source", FileKind.CSharp)
            ]);

        return new MergeSession(
            id: Guid.NewGuid(),
            name: "Session 1",
            sources:
            [
                new MergeSource(@"D:\Project", MergeSourceType.Directory)
            ],
            profile: profile,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));
    }

    private static InputFile CreateSourceExcludedFile(string relativePath)
    {
        return new InputFile(
            fullPath: $@"D:\Project\{relativePath}",
            relativePath: relativePath,
            extension: Path.GetExtension(relativePath),
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "source.exclude",
                "Excluded by a source-specific exclusion."),
            isMergeCandidate: false);
    }

    private static int CountOccurrences(string value, string search)
    {
        int count = 0;
        int index = 0;

        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }

    private static InputFile CreateFile(string relativePath)
    {
        return new InputFile(
            fullPath: $@"D:\Project\{relativePath}",
            relativePath: relativePath,
            extension: ".cs",
            kind: FileKind.CSharp);
    }
}