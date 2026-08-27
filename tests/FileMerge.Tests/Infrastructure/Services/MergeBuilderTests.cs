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
                skippedFilesMetadataMode: SkippedFilesMetadataMode.None,
                skippedFileCategories: CreateSkippedFileCategorySelection(
                    SkippedFileCategory.ManualExclusion))
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

    private static SkippedFileCategorySelection CreateSkippedFileCategorySelection(
        params SkippedFileCategory[] includedCategories)
    {
        return new SkippedFileCategorySelection(
            IncludeDisabledFileTypes: includedCategories.Contains(SkippedFileCategory.DisabledFileType),
            IncludeUnsupportedFiles: includedCategories.Contains(SkippedFileCategory.UnsupportedFile),
            IncludeProfileExclusions: includedCategories.Contains(SkippedFileCategory.ProfileExclusion),
            IncludeManualExclusions: includedCategories.Contains(SkippedFileCategory.ManualExclusion),
            IncludeSourceExclusions: includedCategories.Contains(SkippedFileCategory.SourceExclusion),
            IncludeProcessingFailures: includedCategories.Contains(SkippedFileCategory.ProcessingFailure),
            IncludeOther: includedCategories.Contains(SkippedFileCategory.Other));
    }

    [Theory]
    [InlineData("Disabled.json", "discovery.file-type-disabled", SkippedFileCategory.DisabledFileType)]
    [InlineData("Unsupported.bin", "discovery.unsupported-file-type", SkippedFileCategory.UnsupportedFile)]
    [InlineData("ProfileExcluded.cs", "filter.rule.exclude", SkippedFileCategory.ProfileExclusion)]
    [InlineData("ManualExcluded.cs", "manual.exclude", SkippedFileCategory.ManualExclusion)]
    [InlineData("Broken.cs", "file.read.failed", SkippedFileCategory.ProcessingFailure)]
    [InlineData("CustomSkipped.cs", "custom.exclude", SkippedFileCategory.Other)]
    public void Build_Should_Filter_Normal_Skipped_File_By_Selected_Category(
        string relativePath,
        string reasonCode,
        SkippedFileCategory category)
    {
        MergeBuilder builder = new();

        InputFile skippedFile = CreateFile(relativePath)
            .Exclude(new SkipReason(reasonCode, "Skipped for test."));

        MergeSession selectedSession = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                skippedFileCategories: CreateSkippedFileCategorySelection(category))
            .WithFiles([skippedFile]);

        MergeOutput selectedResult = builder.Build(
            selectedSession,
            [],
            TimeSpan.Zero);

        Assert.Contains($"// - {relativePath}", selectedResult.Content);
        Assert.Contains($"//   Reason: {reasonCode} — Skipped for test.", selectedResult.Content);
        Assert.Equal(1, selectedResult.Statistics.FilesSkipped);

        MergeSession excludedSession = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                skippedFileCategories: CreateSkippedFileCategorySelection())
            .WithFiles([skippedFile]);

        MergeOutput excludedResult = builder.Build(
            excludedSession,
            [],
            TimeSpan.Zero);

        Assert.Contains("// Skipped files: none", excludedResult.Content);
        Assert.DoesNotContain($"// - {relativePath}", excludedResult.Content);
        Assert.Equal(1, excludedResult.Statistics.FilesSkipped);
    }

    [Fact]
    public void Build_Should_Filter_Mixed_Skipped_Metadata_Without_Changing_Statistics()
    {
        MergeBuilder builder = new();

        InputFile disabledFile = CreateFile("Disabled.json")
            .Exclude(new SkipReason(
                "discovery.file-type-disabled",
                "File type is disabled by the current profile."));
        InputFile unsupportedFile = CreateFile("Unsupported.bin")
            .Exclude(new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."));
        InputFile profileExcludedFile = CreateFile("ProfileExcluded.cs")
            .Exclude(new SkipReason(
                "filter.rule.exclude",
                "Excluded by profile filter."));
        InputFile manualExcludedFile = CreateFile("ManualExcluded.cs")
            .Exclude(new SkipReason(
                "manual.exclude",
                "Excluded manually by user."));
        InputFile processingFailedFile = CreateFile("Broken.cs")
            .Exclude(new SkipReason(
                "file.read.failed",
                "Failed to read 'Broken.cs': Access denied."));
        InputFile otherFile = CreateFile("CustomSkipped.cs")
            .Exclude(new SkipReason(
                "custom.exclude",
                "Excluded by a custom reason."));
        InputFile sourceExcluded = CreateSourceExcludedFile("SourceExcluded.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                skippedFileCategories: CreateSkippedFileCategorySelection(
                    SkippedFileCategory.DisabledFileType,
                    SkippedFileCategory.ProfileExclusion,
                    SkippedFileCategory.SourceExclusion,
                    SkippedFileCategory.ProcessingFailure))
            .WithFiles(
            [
                disabledFile,
                unsupportedFile,
                profileExcludedFile,
                manualExcludedFile,
                processingFailedFile,
                otherFile
            ]);

        MergeOutput result = builder.Build(
            session,
            [],
            TimeSpan.Zero,
            [sourceExcluded]);

        Assert.Equal(6, result.Statistics.FilesScanned);
        Assert.Equal(0, result.Statistics.FilesIncluded);
        Assert.Equal(6, result.Statistics.FilesSkipped);
        Assert.Contains("// Files skipped: 6", result.Content);

        Assert.Contains("// - Disabled.json", result.Content);
        Assert.Contains("// - ProfileExcluded.cs", result.Content);
        Assert.Contains("// - Broken.cs", result.Content);
        Assert.Contains("// - SourceExcluded.cs", result.Content);

        Assert.DoesNotContain("// - Unsupported.bin", result.Content);
        Assert.DoesNotContain("// - ManualExcluded.cs", result.Content);
        Assert.DoesNotContain("// - CustomSkipped.cs", result.Content);

        Assert.Equal(4, CountOccurrences(result.Content, "// - "));
    }

    [Fact]
    public void Build_Should_Use_Same_Category_Membership_For_Simple_And_Detailed_Modes()
    {
        MergeBuilder builder = new();

        InputFile manualExcludedFile = CreateFile("ManualExcluded.cs")
            .Exclude(new SkipReason(
                "manual.exclude",
                "Excluded manually by user."));
        InputFile unsupportedFile = CreateFile("Unsupported.bin")
            .Exclude(new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."));

        SkippedFileCategorySelection selection = CreateSkippedFileCategorySelection(
            SkippedFileCategory.ManualExclusion);

        MergeSession simpleSession = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
                skippedFileCategories: selection)
            .WithFiles([manualExcludedFile, unsupportedFile]);

        MergeSession detailedSession = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                skippedFileCategories: selection)
            .WithFiles([manualExcludedFile, unsupportedFile]);

        MergeOutput simpleResult = builder.Build(simpleSession, [], TimeSpan.Zero);
        MergeOutput detailedResult = builder.Build(detailedSession, [], TimeSpan.Zero);

        Assert.Contains("// - ManualExcluded.cs", simpleResult.Content);
        Assert.Contains("// - ManualExcluded.cs", detailedResult.Content);
        Assert.DoesNotContain("// - Unsupported.bin", simpleResult.Content);
        Assert.DoesNotContain("// - Unsupported.bin", detailedResult.Content);

        Assert.DoesNotContain("manual.exclude", simpleResult.Content);
        Assert.Contains(
            "//   Reason: manual.exclude — Excluded manually by user.",
            detailedResult.Content);
    }

    [Fact]
    public void Build_Should_Treat_Skipped_File_Without_Reason_As_Other_Category()
    {
        MergeBuilder builder = new();

        var skippedWithoutReason = new InputFile(
            fullPath: @"D:\Project\Unknown.cs",
            relativePath: "Unknown.cs",
            extension: ".cs",
            kind: FileKind.CSharp,
            isIncluded: false);

        MergeSession selectedSession = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                skippedFileCategories: CreateSkippedFileCategorySelection(
                    SkippedFileCategory.Other))
            .WithFiles([skippedWithoutReason]);

        MergeOutput selectedResult = builder.Build(selectedSession, [], TimeSpan.Zero);

        Assert.Contains("// - Unknown.cs", selectedResult.Content);
        Assert.Contains("//   Reason: unknown — No skip reason available.", selectedResult.Content);

        MergeSession excludedSession = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                skippedFileCategories: CreateSkippedFileCategorySelection())
            .WithFiles([skippedWithoutReason]);

        MergeOutput excludedResult = builder.Build(excludedSession, [], TimeSpan.Zero);

        Assert.Contains("// Skipped files: none", excludedResult.Content);
        Assert.DoesNotContain("// - Unknown.cs", excludedResult.Content);
        Assert.Equal(1, excludedResult.Statistics.FilesSkipped);
    }

    [Fact]
    public void Build_Should_Render_Source_Excluded_Files_When_Explicit_Category_Selection_Includes_Them()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile sourceExcluded = CreateSourceExcludedFile("Generated.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
                includeSourceExcludedFiles: false,
                skippedFileCategories: CreateSkippedFileCategorySelection(
                    SkippedFileCategory.SourceExclusion))
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
    }

    [Fact]
    public void Build_Should_Not_Render_Source_Excluded_Files_When_Explicit_Category_Selection_Excludes_Them()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        InputFile sourceExcluded = CreateSourceExcludedFile("Generated.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Simple,
                includeSourceExcludedFiles: true,
                skippedFileCategories: CreateSkippedFileCategorySelection())
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
    public void Build_Should_Filter_Categories_Before_Deduplicating_Normal_And_Source_Excluded_Files()
    {
        MergeBuilder builder = new();

        InputFile regularSkipped = CreateFile("Shared.cs")
            .Exclude(new SkipReason(
                "manual.exclude",
                "Excluded manually by user."));
        InputFile sourceExcluded = CreateSourceExcludedFile("Shared.cs");

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
                includeSourceExcludedFiles: false,
                skippedFileCategories: CreateSkippedFileCategorySelection(
                    SkippedFileCategory.SourceExclusion))
            .WithFiles([regularSkipped]);

        MergeOutput result = builder.Build(
            session,
            [],
            TimeSpan.Zero,
            [sourceExcluded]);

        Assert.Equal(1, CountOccurrences(result.Content, "// - Shared.cs"));
        Assert.Contains(
            "//   Reason: source.exclude — Excluded by a source-specific exclusion.",
            result.Content);
        Assert.DoesNotContain("manual.exclude", result.Content);
        Assert.Equal(1, result.Statistics.FilesSkipped);
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

    [Fact]
    public void Build_Should_Account_For_Complete_Normal_File_Inventory()
    {
        MergeBuilder builder = new();

        InputFile includedFile = CreateFile("Included.cs");
        var fallbackFile = new InputFile(
            fullPath: @"D:\Project\Notes.custom",
            relativePath: "Notes.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);
        var disabledFile = new InputFile(
            fullPath: @"D:\Project\Disabled.json",
            relativePath: "Disabled.json",
            extension: ".json",
            kind: FileKind.Json,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.file-type-disabled",
                "File type is disabled by the current profile."),
            isMergeCandidate: false);
        var unsupportedFile = new InputFile(
            fullPath: @"D:\Project\Unsupported.bin",
            relativePath: "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);
        InputFile profileExcludedFile = CreateFile("ProfileExcluded.cs")
            .Exclude(new SkipReason(
                "filter.rule.exclude",
                "Excluded by profile filter."));
        InputFile manualExcludedFile = CreateFile("ManualExcluded.cs")
            .Exclude(new SkipReason(
                "manual.exclude",
                "Excluded manually by user."));
        InputFile processingFailedFile = CreateFile("Broken.cs")
            .Exclude(new SkipReason(
                "file.read.failed",
                "Failed to read 'Broken.cs': Access denied."));

        MergeSession session = CreateSession(
                includeHeaderComment: true,
                skippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed)
            .WithFiles(
            [
                includedFile,
                fallbackFile,
                disabledFile,
                unsupportedFile,
                profileExcludedFile,
                manualExcludedFile,
                processingFailedFile
            ]);

        MergeSection[] sections =
        [
            new MergeSection(
                sourceFile: includedFile,
                content: "class Included {}",
                order: 0),
            new MergeSection(
                sourceFile: fallbackFile,
                content: "fallback text",
                order: 1)
        ];

        MergeOutput result = builder.Build(session, sections, TimeSpan.Zero);

        Assert.Equal(7, result.Statistics.FilesScanned);
        Assert.Equal(2, result.Statistics.FilesIncluded);
        Assert.Equal(5, result.Statistics.FilesSkipped);

        Assert.Equal(2, result.Sections.Count);
        Assert.Equal(
            new[] { "Included.cs", "Notes.custom" },
            result.Sections.Select(x => x.SourceFile.RelativePath).ToArray());

        Assert.Contains("// Files included: 2", result.Content);
        Assert.Contains("// Files skipped: 5", result.Content);
        Assert.Contains("// Skipped files:", result.Content);

        string[] skippedPaths =
        [
            "Broken.cs",
            "Disabled.json",
            "ManualExcluded.cs",
            "ProfileExcluded.cs",
            "Unsupported.bin"
        ];

        foreach (string skippedPath in skippedPaths)
        {
            Assert.Contains($"// - {skippedPath}", result.Content);
        }

        Assert.Contains(
            "//   Reason: discovery.file-type-disabled — File type is disabled by the current profile.",
            result.Content);
        Assert.Contains(
            "//   Reason: discovery.unsupported-file-type — File type is not supported by the current profile.",
            result.Content);
        Assert.Contains(
            "//   Reason: filter.rule.exclude — Excluded by profile filter.",
            result.Content);
        Assert.Contains(
            "//   Reason: manual.exclude — Excluded manually by user.",
            result.Content);
        Assert.Contains(
            "//   Reason: file.read.failed — Failed to read 'Broken.cs': Access denied.",
            result.Content);

        Assert.Equal(5, CountOccurrences(result.Content, "// - "));
        Assert.Equal(5, CountOccurrences(result.Content, "//   Reason: "));
        Assert.DoesNotContain("// - Included.cs", result.Content);
        Assert.DoesNotContain("// - Notes.custom", result.Content);
    }

    private static MergeSession CreateSession(
        bool includeHeaderComment,
        SkippedFilesMetadataMode skippedFilesMetadataMode = SkippedFilesMetadataMode.None,
        bool includeBuildTimestampMetadata = true,
        bool includeSessionNameMetadata = true,
        bool includeOutputPathMetadata = true,
        bool includeFileSummaryMetadata = true,
        bool includeSourceExcludedFiles = false,
        SkippedFileCategorySelection? skippedFileCategories = null)
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
                    IncludeSourceExcludedFiles: includeSourceExcludedFiles,
                    SkippedFileCategories: skippedFileCategories)),
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