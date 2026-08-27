using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.UseCases.BuildPreview;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Application.UseCases;

public sealed class BuildMergePreviewUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_Request_Is_Null()
    {
        BuildMergePreviewUseCase useCase = CreateUseCase();

        ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(null!));

        Assert.Equal("request", ex.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Failed_Result_When_Validation_Contains_Error()
    {
        ValidationIssue[] validationIssues =
        [
            new ValidationIssue(ValidationSeverity.Error, "validation.error", "Validation failed.")
        ];

        var validator = new FakeMergeSessionValidator(validationIssues);
        var discovery = new FakeFileDiscoveryService();
        var filter = new FakeFileFilterService();
        var overrides = new FakeFileInclusionOverrideService();
        var reader = new FakeContentReader();
        var transformation = new FakeContentTransformationService();
        var builder = new FakeMergeBuilder();

        var useCase = new BuildMergePreviewUseCase(
            validator,
            discovery,
            filter,
            overrides,
            reader,
            transformation,
            builder);

        MergeSession session = CreateSession();
        var request = new BuildMergePreviewRequest(session);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(request);

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Output);
        Assert.Single(result.ValidationIssues);
        Assert.Equal("validation.error", result.ValidationIssues.Single().Code);

        Assert.Empty(discovery.DiscoverCalls);
        Assert.Empty(filter.ApplyCalls);
        Assert.Empty(overrides.ApplyCalls);
        Assert.Empty(reader.ReadCalls);
        Assert.Empty(transformation.TransformCalls);
        Assert.Empty(builder.BuildCalls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Discover_Filter_ApplyOverrides_Read_Transform_And_Build_Output_When_Validation_Passes()
    {
        var sourceFile1 = new InputFile(
            fullPath: @"D:\Project\Test1.cs",
            relativePath: "Test1.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var sourceFile2 = new InputFile(
            fullPath: @"D:\Project\Test2.cs",
            relativePath: "Test2.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        InputFile[] filteredFiles =
        [
            sourceFile1,
            sourceFile2.Exclude(new SkipReason("skip", "Skipped"))
        ];

        InputFile[] overriddenFiles =
        [
            sourceFile1.Include(),
            sourceFile2.Include()
        ];

        var output = new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(2, 2, 0, 6, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));

        var validator = new FakeMergeSessionValidator([]);
        var discovery = new FakeFileDiscoveryService([sourceFile1, sourceFile2]);
        var filter = new FakeFileFilterService(filteredFiles);
        var overrides = new FakeFileInclusionOverrideService(overriddenFiles);
        var reader = new FakeContentReader(ContentReadResult.Success("raw content", "utf-8"));
        var transformation = new FakeContentTransformationService("transformed content");
        var builder = new FakeMergeBuilder(output);

        var useCase = new BuildMergePreviewUseCase(
            validator,
            discovery,
            filter,
            overrides,
            reader,
            transformation,
            builder);

        FileInclusionOverride[] inclusionOverrides =
        [
            new FileInclusionOverride(sourceFile1.FullPath, true)
        ];

        MergeSession session = CreateSession(includeFileSeparators: true);
        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(session, inclusionOverrides));

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Output);
        Assert.Equal(output, result.Output);

        Assert.Single(discovery.DiscoverCalls);
        Assert.Single(filter.ApplyCalls);
        Assert.Single(overrides.ApplyCalls);
        Assert.Equal(inclusionOverrides, overrides.ApplyCalls.Single().Overrides);

        Assert.Equal(2, reader.ReadCalls.Count);
        Assert.Equal(2, transformation.TransformCalls.Count);
        Assert.Single(builder.BuildCalls);

        MergeSection[] builtSections = builder.BuildCalls.Single().Sections.ToArray();
        Assert.Equal(2, builtSections.Length);

        Assert.All(builtSections, section => Assert.Equal("transformed content", section.Content));
        Assert.Contains(builtSections, x => x.HeaderText == "Test1.cs [utf-8]");
        Assert.Contains(builtSections, x => x.HeaderText == "Test2.cs [utf-8]");

        Assert.Equal(output, result.Session.LastOutput);
        Assert.Equal(2, result.Session.Files.Count);
        Assert.All(result.Session.Files, x => Assert.True(x.IsIncluded));
    }


    [Fact]
    public async Task ExecuteAsync_Should_Process_Only_MergeCandidates_And_Retain_Complete_Inventories()
    {
        var candidate = new InputFile(
            fullPath: @"D:\Project\Included.cs",
            relativePath: "Included.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var disabledNonCandidate = new InputFile(
            fullPath: @"D:\Project\Disabled.cs",
            relativePath: "Disabled.cs",
            extension: ".cs",
            kind: FileKind.CSharp,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.file-type-disabled",
                "File type is disabled by the current profile."),
            isMergeCandidate: false);

        var unsupportedNonCandidate = new InputFile(
            fullPath: @"D:\Project\Unsupported.bin",
            relativePath: "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        var filter = new FakeFileFilterService([candidate]);
        var overrides = new FakeFileInclusionOverrideService([candidate]);
        var reader = new FakeContentReader(ContentReadResult.Success("raw", "utf-8"));
        var builder = new FakeMergeBuilder();

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([candidate, disabledNonCandidate, unsupportedNonCandidate]),
            filter,
            overrides,
            reader,
            new FakeContentTransformationService("transformed"),
            builder);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(CreateSession()));

        Assert.True(result.IsSuccessful);

        InputFile filteredInput = Assert.Single(filter.ApplyCalls.Single().Files);
        Assert.Equal(candidate.FullPath, filteredInput.FullPath);

        InputFile overrideInput = Assert.Single(overrides.ApplyCalls.Single().Files);
        Assert.Equal(candidate.FullPath, overrideInput.FullPath);

        InputFile readInput = Assert.Single(reader.ReadCalls).File;
        Assert.Equal(candidate.FullPath, readInput.FullPath);

        Assert.Equal(3, result.AutomaticFiles.Count);
        Assert.Equal(3, result.Session.Files.Count);

        Assert.Contains(result.AutomaticFiles, x => x.FullPath == candidate.FullPath);
        Assert.Contains(result.Session.Files, x => x.FullPath == candidate.FullPath);

        InputFile automaticDisabled = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == disabledNonCandidate.FullPath);
        InputFile effectiveDisabled = Assert.Single(
            result.Session.Files,
            x => x.FullPath == disabledNonCandidate.FullPath);

        Assert.False(automaticDisabled.IsMergeCandidate);
        Assert.False(automaticDisabled.IsIncluded);
        Assert.Equal("discovery.file-type-disabled", automaticDisabled.SkipReason?.Code);
        Assert.False(effectiveDisabled.IsMergeCandidate);
        Assert.False(effectiveDisabled.IsIncluded);
        Assert.Equal("discovery.file-type-disabled", effectiveDisabled.SkipReason?.Code);

        InputFile automaticUnsupported = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == unsupportedNonCandidate.FullPath);
        InputFile effectiveUnsupported = Assert.Single(
            result.Session.Files,
            x => x.FullPath == unsupportedNonCandidate.FullPath);

        Assert.False(automaticUnsupported.IsMergeCandidate);
        Assert.False(automaticUnsupported.IsIncluded);
        Assert.Equal("discovery.unsupported-file-type", automaticUnsupported.SkipReason?.Code);
        Assert.False(effectiveUnsupported.IsMergeCandidate);
        Assert.False(effectiveUnsupported.IsIncluded);
        Assert.Equal("discovery.unsupported-file-type", effectiveUnsupported.SkipReason?.Code);

        MergeSession buildSession = Assert.Single(builder.BuildCalls).Session;
        Assert.Equal(3, buildSession.Files.Count);
        Assert.Contains(buildSession.Files, x => x.FullPath == disabledNonCandidate.FullPath);
        Assert.Contains(buildSession.Files, x => x.FullPath == unsupportedNonCandidate.FullPath);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_Filtered_Candidate_State_In_AutomaticInventory()
    {
        var candidate = new InputFile(
            fullPath: @"D:\Project\Filtered.cs",
            relativePath: "Filtered.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var filterReason = new SkipReason("filter.rule.exclude", "Excluded by profile filter.");
        InputFile filteredCandidate = candidate.Exclude(filterReason);

        var nonCandidate = new InputFile(
            fullPath: @"D:\Project\Unsupported.bin",
            relativePath: "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        var reader = new FakeContentReader(ContentReadResult.Success("raw", "utf-8"));
        var builder = new FakeMergeBuilder();

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([candidate, nonCandidate]),
            new FakeFileFilterService([filteredCandidate]),
            new FakeFileInclusionOverrideService([filteredCandidate]),
            reader,
            new FakeContentTransformationService("transformed"),
            builder);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(CreateSession()));

        InputFile automaticCandidate = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == candidate.FullPath);

        Assert.False(automaticCandidate.IsIncluded);
        Assert.Equal(filterReason, automaticCandidate.SkipReason);

        InputFile effectiveCandidate = Assert.Single(
            result.Session.Files,
            x => x.FullPath == candidate.FullPath);

        Assert.False(effectiveCandidate.IsIncluded);
        Assert.Equal(filterReason, effectiveCandidate.SkipReason);

        InputFile effectiveNonCandidate = Assert.Single(
            result.Session.Files,
            x => x.FullPath == nonCandidate.FullPath);

        Assert.False(effectiveNonCandidate.IsMergeCandidate);
        Assert.False(effectiveNonCandidate.IsIncluded);
        Assert.Equal("discovery.unsupported-file-type", effectiveNonCandidate.SkipReason?.Code);

        Assert.Equal(2, result.AutomaticFiles.Count);
        Assert.Equal(2, result.Session.Files.Count);
        Assert.Empty(reader.ReadCalls);
        Assert.Equal(2, Assert.Single(builder.BuildCalls).Session.Files.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_Overridden_Files_In_Result_Session()
    {
        var sourceFile = new InputFile(
            fullPath: @"D:\Project\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var nonCandidate = new InputFile(
            fullPath: @"D:\Project\Unsupported.bin",
            relativePath: "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        InputFile[] filteredFiles =
        [
            sourceFile
        ];

        var manualReason = new SkipReason("manual.exclude", "Excluded manually by user.");
        InputFile[] overriddenFiles =
        [
            sourceFile.Exclude(manualReason)
        ];

        var output = new MergeOutput(
            content: "",
            sections: [],
            statistics: new MergeStatistics(2, 0, 2, 0, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));

        var builder = new FakeMergeBuilder(output);
        var reader = new FakeContentReader(ContentReadResult.Success("raw", "utf-8"));

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([sourceFile, nonCandidate]),
            new FakeFileFilterService(filteredFiles),
            new FakeFileInclusionOverrideService(overriddenFiles),
            reader,
            new FakeContentTransformationService("transformed"),
            builder);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(
            CreateSession(),
            [new FileInclusionOverride(sourceFile.FullPath, false)]));

        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.AutomaticFiles.Count);
        Assert.Equal(2, result.Session.Files.Count);

        InputFile automaticCandidate = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == sourceFile.FullPath);
        Assert.True(automaticCandidate.IsIncluded);
        Assert.Null(automaticCandidate.SkipReason);

        InputFile effectiveCandidate = Assert.Single(
            result.Session.Files,
            x => x.FullPath == sourceFile.FullPath);
        Assert.False(effectiveCandidate.IsIncluded);
        Assert.Equal(manualReason, effectiveCandidate.SkipReason);

        InputFile effectiveNonCandidate = Assert.Single(
            result.Session.Files,
            x => x.FullPath == nonCandidate.FullPath);
        Assert.False(effectiveNonCandidate.IsMergeCandidate);
        Assert.False(effectiveNonCandidate.IsIncluded);
        Assert.Equal("discovery.unsupported-file-type", effectiveNonCandidate.SkipReason?.Code);

        Assert.Empty(reader.ReadCalls);

        (MergeSession Session, IReadOnlyCollection<MergeSection> Sections, TimeSpan TotalDuration, IReadOnlyCollection<InputFile> SourceExcludedFiles) buildCall = Assert.Single(builder.BuildCalls);
        Assert.Empty(buildCall.Sections);
        Assert.Equal(2, buildCall.Session.Files.Count);

        InputFile builtCandidate = Assert.Single(
            buildCall.Session.Files,
            x => x.FullPath == sourceFile.FullPath);
        Assert.False(builtCandidate.IsIncluded);
        Assert.Equal(manualReason, builtCandidate.SkipReason);

        Assert.Contains(buildCall.Session.Files, x => x.FullPath == nonCandidate.FullPath);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Not_Set_HeaderText_When_File_Separators_Are_Disabled()
    {
        var sourceFile = new InputFile(
            fullPath: @"D:\Project\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var output = new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(1, 1, 0, 6, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));

        var builder = new FakeMergeBuilder(output);

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([sourceFile]),
            new FakeFileFilterService([sourceFile]),
            new FakeFileInclusionOverrideService([sourceFile]),
            new FakeContentReader(ContentReadResult.Success("raw content", "utf-8")),
            new FakeContentTransformationService("transformed content"),
            builder);

        MergeSession session = CreateSession(includeFileSeparators: false);
        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(session));

        Assert.True(result.IsSuccessful);

        MergeSection onlySection = builder.BuildCalls.Single().Sections.Single();
        Assert.Null(onlySection.HeaderText);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_FileName_Header_When_RelativePath_In_Separator_Is_Disabled()
    {
        var sourceFile = new InputFile(
            fullPath: @"D:\Project\Subfolder\Test.cs",
            relativePath: @"Subfolder\Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var output = new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(1, 1, 0, 6, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));

        var builder = new FakeMergeBuilder(output);

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([sourceFile]),
            new FakeFileFilterService([sourceFile]),
            new FakeFileInclusionOverrideService([sourceFile]),
            new FakeContentReader(ContentReadResult.Success("raw content", "utf-8")),
            new FakeContentTransformationService("transformed content"),
            builder);

        MergeSession session = CreateSession(
            includeFileSeparators: true,
            includeRelativePathInSeparator: false);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(session));

        Assert.True(result.IsSuccessful);

        MergeSection onlySection = builder.BuildCalls.Single().Sections.Single();
        Assert.Equal("Test.cs [utf-8]", onlySection.HeaderText);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Use_RelativePath_Header_When_RelativePath_In_Separator_Is_Enabled()
    {
        var sourceFile = new InputFile(
            fullPath: @"D:\Project\Subfolder\Test.cs",
            relativePath: @"Subfolder\Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var output = new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(1, 1, 0, 6, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));

        var builder = new FakeMergeBuilder(output);

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([sourceFile]),
            new FakeFileFilterService([sourceFile]),
            new FakeFileInclusionOverrideService([sourceFile]),
            new FakeContentReader(ContentReadResult.Success("raw content", "utf-8")),
            new FakeContentTransformationService("transformed content"),
            builder);

        MergeSession session = CreateSession(
            includeFileSeparators: true,
            includeRelativePathInSeparator: true);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(session));

        Assert.True(result.IsSuccessful);

        MergeSection onlySection = builder.BuildCalls.Single().Sections.Single();
        Assert.Equal(@"Subfolder\Test.cs [utf-8]", onlySection.HeaderText);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Skip_File_When_Read_Fails_And_Continue_Building()
    {
        var readableFile = new InputFile(
            fullPath: @"D:\Project\Ok.cs",
            relativePath: "Ok.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var brokenFile = new InputFile(
            fullPath: @"D:\Project\Broken.cs",
            relativePath: "Broken.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var output = new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(2, 1, 1, 6, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));

        var builder = new FakeMergeBuilder(output);

        var reader = new DelegatingContentReader((file, _) =>
        {
            if (file.FullPath.EndsWith("Broken.cs", StringComparison.OrdinalIgnoreCase))
            {
                return ContentReadResult.Failure(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "file.read.failed",
                    "Failed to read 'Broken.cs': Access denied."));
            }

            return ContentReadResult.Success("raw content", "utf-8");
        });

        var transformation = new FakeContentTransformationService("transformed content");

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([readableFile, brokenFile]),
            new FakeFileFilterService([readableFile, brokenFile]),
            new FakeFileInclusionOverrideService([readableFile, brokenFile]),
            reader,
            transformation,
            builder);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(CreateSession()));

        Assert.True(result.IsSuccessful);

        MergeSection section = Assert.Single(builder.BuildCalls.Single().Sections);
        Assert.Equal("Ok.cs", section.SourceFile.RelativePath);

        InputFile automaticBrokenFile = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == brokenFile.FullPath);
        Assert.True(automaticBrokenFile.IsMergeCandidate);
        Assert.True(automaticBrokenFile.IsIncluded);
        Assert.Null(automaticBrokenFile.SkipReason);

        InputFile effectiveReadableFile = Assert.Single(
            result.Session.Files,
            x => x.FullPath == readableFile.FullPath);
        Assert.True(effectiveReadableFile.IsIncluded);

        InputFile effectiveBrokenFile = Assert.Single(
            result.Session.Files,
            x => x.FullPath == brokenFile.FullPath);
        Assert.True(effectiveBrokenFile.IsMergeCandidate);
        Assert.False(effectiveBrokenFile.IsIncluded);
        Assert.Equal("file.read.failed", effectiveBrokenFile.SkipReason?.Code);
        Assert.Equal(
            "Failed to read 'Broken.cs': Access denied.",
            effectiveBrokenFile.SkipReason?.Description);

        ValidationIssue readIssue = Assert.Single(
            result.ValidationIssues,
            x => x.Code == "file.read.failed");
        Assert.Equal(ValidationSeverity.Warning, readIssue.Severity);
        Assert.Equal(
            "Failed to read 'Broken.cs': Access denied.",
            readIssue.Message);

        (MergeSession Session, IReadOnlyCollection<MergeSection> Sections, TimeSpan TotalDuration, IReadOnlyCollection<InputFile> SourceExcludedFiles) buildCall = Assert.Single(builder.BuildCalls);
        InputFile builtBrokenFile = Assert.Single(
            buildCall.Session.Files,
            x => x.FullPath == brokenFile.FullPath);
        Assert.False(builtBrokenFile.IsIncluded);
        Assert.Equal("file.read.failed", builtBrokenFile.SkipReason?.Code);

        (string Content, InputFile File, MergeProfile Profile) transformCall = Assert.Single(transformation.TransformCalls);
        Assert.Equal(readableFile.FullPath, transformCall.File.FullPath);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Pass_Input_Read_Options_To_ContentReader()
    {
        var sourceFile = new InputFile(
            fullPath: @"D:\Project\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var reader = new FakeContentReader(ContentReadResult.Success("raw content", "windows-1251"));

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([sourceFile]),
            new FakeFileFilterService([sourceFile]),
            new FakeFileInclusionOverrideService([sourceFile]),
            reader,
            new FakeContentTransformationService("transformed content"),
            new FakeMergeBuilder());

        MergeSession session = CreateSession(
            inputEncodingMode: InputEncodingMode.Specific,
            preferredInputEncodingName: "windows-1251",
            fallbackInputEncodingName: "utf-8");

        BuildMergePreviewResult result = await useCase.ExecuteAsync(new BuildMergePreviewRequest(session));

        Assert.True(result.IsSuccessful);
        Assert.Single(reader.ReadCalls);

        (_, InputReadOptions options) = reader.ReadCalls.Single();
        Assert.Equal(InputEncodingMode.Specific, options.EncodingMode);
        Assert.Equal("windows-1251", options.PreferredEncodingName);
        Assert.Equal("utf-8", options.FallbackEncodingName);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Report_Progress_For_Main_Stages()
    {
        var sourceFile = new InputFile(
            fullPath: @"D:\Project\Test.cs",
            relativePath: "Test.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var progressEvents = new List<BuildMergePreviewProgress>();

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([sourceFile]),
            new FakeFileFilterService([sourceFile]),
            new FakeFileInclusionOverrideService([sourceFile]),
            new FakeContentReader(ContentReadResult.Success("raw content", "utf-8")),
            new FakeContentTransformationService("transformed content"),
            new FakeMergeBuilder());

        var progress = new CapturingProgress<BuildMergePreviewProgress>(progressEvents.Add);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(
            new BuildMergePreviewRequest(CreateSession()),
            progress,
            CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.NotEmpty(progressEvents);

        Assert.Contains(progressEvents, x => x.Stage == BuildMergePreviewStage.Validating);
        Assert.Contains(progressEvents, x => x.Stage == BuildMergePreviewStage.DiscoveringFiles);
        Assert.Contains(progressEvents, x => x.Stage == BuildMergePreviewStage.FilteringFiles);
        Assert.Contains(progressEvents, x => x.Stage == BuildMergePreviewStage.ApplyingOverrides);
        Assert.Contains(progressEvents, x => x.Stage == BuildMergePreviewStage.ReadingFiles);
        Assert.Contains(progressEvents, x => x.Stage == BuildMergePreviewStage.BuildingOutput);
        Assert.Contains(progressEvents, x => x.Stage == BuildMergePreviewStage.Completed);

        BuildMergePreviewProgress discoveryStartEvent = progressEvents.First(x => x.Stage == BuildMergePreviewStage.DiscoveringFiles);

        Assert.Equal(0, discoveryStartEvent.Current);
        Assert.Equal(0, discoveryStartEvent.Total);
        Assert.Equal("Discovering files.", discoveryStartEvent.Message);

        BuildMergePreviewProgress readEvent = progressEvents.Last(x => x.Stage == BuildMergePreviewStage.ReadingFiles);
        Assert.Equal(1, readEvent.Current);
        Assert.Equal(1, readEvent.Total);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Forward_Discovery_Probe_Progress()
    {
        FileDiscoveryProgress[] discoveryProgressEvents =
        [
            new FileDiscoveryProgress(1, "Notes.foo"),
            new FileDiscoveryProgress(2, @"Sub\Config.custom")
        ];

        var progressEvents = new List<BuildMergePreviewProgress>();

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService(
                result: [],
                progressEvents: discoveryProgressEvents),
            new FakeFileFilterService(),
            new FakeFileInclusionOverrideService(),
            new FakeContentReader(),
            new FakeContentTransformationService(),
            new FakeMergeBuilder());

        var progress = new CapturingProgress<BuildMergePreviewProgress>(progressEvents.Add);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(
            new BuildMergePreviewRequest(CreateSession()),
            progress,
            CancellationToken.None);

        Assert.True(result.IsSuccessful);

        BuildMergePreviewProgress[] discoveryEvents =
        [
            .. progressEvents.Where(x => x.Stage == BuildMergePreviewStage.DiscoveringFiles)
        ];

        Assert.Equal(3, discoveryEvents.Length);

        Assert.Equal(0, discoveryEvents[0].Current);
        Assert.Equal(0, discoveryEvents[0].Total);
        Assert.Equal("Discovering files.", discoveryEvents[0].Message);

        Assert.Equal(1, discoveryEvents[1].Current);
        Assert.Equal(0, discoveryEvents[1].Total);
        Assert.Equal("Probing unsupported file 1: Notes.foo", discoveryEvents[1].Message);

        Assert.Equal(2, discoveryEvents[2].Current);
        Assert.Equal(0, discoveryEvents[2].Total);
        Assert.Equal(
            @"Probing unsupported file 2: Sub\Config.custom",
            discoveryEvents[2].Message);

        int lastDiscoveryIndex = progressEvents.FindLastIndex(x => x.Stage == BuildMergePreviewStage.DiscoveringFiles);

        int filteringIndex = progressEvents.FindIndex(x => x.Stage == BuildMergePreviewStage.FilteringFiles);

        Assert.True(filteringIndex > lastDiscoveryIndex);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_OperationCanceledException_When_Cancellation_Is_Requested_Before_Start()
    {
        BuildMergePreviewUseCase useCase = CreateUseCase();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(
                new BuildMergePreviewRequest(CreateSession()),
                progress: null,
                cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_OperationCanceledException_When_Cancellation_Is_Requested_During_Reading()
    {
        var file1 = new InputFile(
            fullPath: @"D:\Project\File1.cs",
            relativePath: "File1.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var file2 = new InputFile(
            fullPath: @"D:\Project\File2.cs",
            relativePath: "File2.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        using var cancellationTokenSource = new CancellationTokenSource();

        var reader = new CancellingContentReader(cancellationTokenSource);

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService([file1, file2]),
            new FakeFileFilterService([file1, file2]),
            new FakeFileInclusionOverrideService([file1, file2]),
            reader,
            new FakeContentTransformationService("transformed content"),
            new FakeMergeBuilder());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(
                new BuildMergePreviewRequest(CreateSession()),
                progress: null,
                cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Pass_Source_Excluded_Files_Only_To_Merge_Builder()
    {
        InputFile candidate = new(
            fullPath: @"D:\Project\Included.cs",
            relativePath: "Included.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        InputFile sourceExcluded = new(
            fullPath: @"D:\Project\Generated.cs",
            relativePath: "Generated.cs",
            extension: ".cs",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "source.exclude",
                "Excluded by a source-specific exclusion."),
            isMergeCandidate: false);

        var discovery = new FakeFileDiscoveryService(
            [candidate],
            sourceExcludedFiles: [sourceExcluded]);
        var filter = new FakeFileFilterService([candidate]);
        var overrides = new FakeFileInclusionOverrideService([candidate]);
        var reader = new FakeContentReader(ContentReadResult.Success("content", "utf-8"));
        var mergeBuilder = new FakeMergeBuilder();

        BuildMergePreviewUseCase useCase = new(
            new FakeMergeSessionValidator([]),
            discovery,
            filter,
            overrides,
            reader,
            new FakeContentTransformationService("content"),
            mergeBuilder);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(
            new BuildMergePreviewRequest(CreateSession()));

        Assert.DoesNotContain(filter.ApplyCalls.SelectMany(x => x.Files), x => x.FullPath == sourceExcluded.FullPath);
        Assert.DoesNotContain(overrides.ApplyCalls.SelectMany(x => x.Files), x => x.FullPath == sourceExcluded.FullPath);
        Assert.DoesNotContain(reader.ReadCalls, x => x.File.FullPath == sourceExcluded.FullPath);
        Assert.DoesNotContain(result.AutomaticFiles, x => x.FullPath == sourceExcluded.FullPath);
        Assert.Same(sourceExcluded, Assert.Single(result.SourceExcludedFiles));

        (MergeSession Session, IReadOnlyCollection<MergeSection> Sections, TimeSpan TotalDuration, IReadOnlyCollection<InputFile> SourceExcludedFiles) buildCall = Assert.Single(mergeBuilder.BuildCalls);
        Assert.Same(sourceExcluded, Assert.Single(buildCall.SourceExcludedFiles));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Preserve_Complete_Output_Accounting_Across_All_File_States()
    {
        var includedFile = new InputFile(
            fullPath: @"D:\Project\Included.cs",
            relativePath: "Included.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

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

        var profileExcludedFile = new InputFile(
            fullPath: @"D:\Project\ProfileExcluded.cs",
            relativePath: "ProfileExcluded.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var manualExcludedFile = new InputFile(
            fullPath: @"D:\Project\ManualExcluded.cs",
            relativePath: "ManualExcluded.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var manualOverride = new FileInclusionOverride(
            manualExcludedFile.FullPath,
            false);

        var brokenFile = new InputFile(
            fullPath: @"D:\Project\Broken.cs",
            relativePath: "Broken.cs",
            extension: ".cs",
            kind: FileKind.CSharp);

        var sourceExcludedFile = new InputFile(
            fullPath: @"D:\Project\Generated.cs",
            relativePath: "Generated.cs",
            extension: ".cs",
            kind: FileKind.Unknown,
            isIncluded: false,
            skipReason: new SkipReason(
                "source.exclude",
                "Excluded by a source-specific exclusion."),
            isMergeCandidate: false);

        var profileReason = new SkipReason(
            "filter.rule.exclude",
            "Excluded by profile filter.");
        var manualReason = new SkipReason(
            "manual.exclude",
            "Excluded manually by user.");

        InputFile filteredProfileExcludedFile = profileExcludedFile.Exclude(profileReason);
        InputFile overriddenManualExcludedFile = manualExcludedFile.Exclude(manualReason);

        InputFile[] filteredFiles =
        [
            includedFile,
            fallbackFile,
            filteredProfileExcludedFile,
            manualExcludedFile,
            brokenFile
        ];

        InputFile[] overriddenFiles =
        [
            includedFile,
            fallbackFile,
            filteredProfileExcludedFile,
            overriddenManualExcludedFile,
            brokenFile
        ];

        var discovery = new FakeFileDiscoveryService(
            [
                includedFile,
                fallbackFile,
                disabledFile,
                unsupportedFile,
                profileExcludedFile,
                manualExcludedFile,
                brokenFile
            ],
            sourceExcludedFiles: [sourceExcludedFile]);
        var filter = new FakeFileFilterService(filteredFiles);
        var overrides = new FakeFileInclusionOverrideService(overriddenFiles);
        var reader = new DelegatingContentReader((file, _) =>
        {
            if (file.FullPath == brokenFile.FullPath)
            {
                return ContentReadResult.Failure(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "file.read.failed",
                    "Failed to read 'Broken.cs': Access denied."));
            }

            return ContentReadResult.Success($"raw:{file.RelativePath}", "utf-8");
        });
        var transformation = new FakeContentTransformationService("transformed content");
        var builder = new FakeMergeBuilder();

        var useCase = new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            discovery,
            filter,
            overrides,
            reader,
            transformation,
            builder);

        BuildMergePreviewResult result = await useCase.ExecuteAsync(
            new BuildMergePreviewRequest(
                CreateSession(),
                [manualOverride]));

        Assert.True(result.IsSuccessful);

        IReadOnlyCollection<InputFile> filterInput = Assert.Single(filter.ApplyCalls).Files;
        Assert.Equal(5, filterInput.Count);
        Assert.Equal(
            new[]
            {
                includedFile.FullPath,
                fallbackFile.FullPath,
                profileExcludedFile.FullPath,
                manualExcludedFile.FullPath,
                brokenFile.FullPath
            }.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            filterInput
                .Select(x => x.FullPath)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray());
        Assert.DoesNotContain(filterInput, x => x.FullPath == disabledFile.FullPath);
        Assert.DoesNotContain(filterInput, x => x.FullPath == unsupportedFile.FullPath);
        Assert.DoesNotContain(filterInput, x => x.FullPath == sourceExcludedFile.FullPath);

        (IReadOnlyCollection<InputFile> Files, IReadOnlyCollection<FileInclusionOverride> Overrides) overrideCall = Assert.Single(overrides.ApplyCalls);
        IReadOnlyCollection<InputFile> overrideInput = overrideCall.Files;
        Assert.Equal([manualOverride], overrideCall.Overrides);
        Assert.Equal(5, overrideInput.Count);
        InputFile overrideProfileExcluded = Assert.Single(
            overrideInput,
            x => x.FullPath == profileExcludedFile.FullPath);
        Assert.False(overrideProfileExcluded.IsIncluded);
        Assert.Equal("filter.rule.exclude", overrideProfileExcluded.SkipReason?.Code);
        Assert.DoesNotContain(overrideInput, x => x.FullPath == disabledFile.FullPath);
        Assert.DoesNotContain(overrideInput, x => x.FullPath == unsupportedFile.FullPath);
        Assert.DoesNotContain(overrideInput, x => x.FullPath == sourceExcludedFile.FullPath);

        Assert.Equal(3, reader.ReadCalls.Count);
        Assert.Equal(
            new[]
            {
                includedFile.FullPath,
                fallbackFile.FullPath,
                brokenFile.FullPath
            }.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            reader.ReadCalls
                .Select(x => x.File.FullPath)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray());

        Assert.Equal(2, transformation.TransformCalls.Count);
        Assert.Equal(
            new[]
            {
                includedFile.FullPath,
                fallbackFile.FullPath
            }.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            transformation.TransformCalls
                .Select(x => x.File.FullPath)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray());

        Assert.Equal(7, result.AutomaticFiles.Count);

        InputFile automaticIncluded = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == includedFile.FullPath);
        Assert.True(automaticIncluded.IsIncluded);

        InputFile automaticFallback = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == fallbackFile.FullPath);
        Assert.True(automaticFallback.IsIncluded);
        Assert.True(automaticFallback.IsFallbackText);

        InputFile automaticDisabled = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == disabledFile.FullPath);
        Assert.False(automaticDisabled.IsIncluded);
        Assert.False(automaticDisabled.IsMergeCandidate);
        Assert.Equal("discovery.file-type-disabled", automaticDisabled.SkipReason?.Code);

        InputFile automaticUnsupported = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == unsupportedFile.FullPath);
        Assert.False(automaticUnsupported.IsIncluded);
        Assert.False(automaticUnsupported.IsMergeCandidate);
        Assert.Equal("discovery.unsupported-file-type", automaticUnsupported.SkipReason?.Code);

        InputFile automaticProfileExcluded = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == profileExcludedFile.FullPath);
        Assert.False(automaticProfileExcluded.IsIncluded);
        Assert.Equal("filter.rule.exclude", automaticProfileExcluded.SkipReason?.Code);

        InputFile automaticManualExcluded = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == manualExcludedFile.FullPath);
        Assert.True(automaticManualExcluded.IsIncluded);
        Assert.Null(automaticManualExcluded.SkipReason);

        InputFile automaticBroken = Assert.Single(
            result.AutomaticFiles,
            x => x.FullPath == brokenFile.FullPath);
        Assert.True(automaticBroken.IsIncluded);
        Assert.Null(automaticBroken.SkipReason);

        Assert.Equal(7, result.Session.Files.Count);

        InputFile effectiveIncluded = Assert.Single(
            result.Session.Files,
            x => x.FullPath == includedFile.FullPath);
        Assert.True(effectiveIncluded.IsIncluded);

        InputFile effectiveFallback = Assert.Single(
            result.Session.Files,
            x => x.FullPath == fallbackFile.FullPath);
        Assert.True(effectiveFallback.IsIncluded);
        Assert.True(effectiveFallback.IsFallbackText);

        InputFile effectiveDisabled = Assert.Single(
            result.Session.Files,
            x => x.FullPath == disabledFile.FullPath);
        Assert.False(effectiveDisabled.IsIncluded);
        Assert.False(effectiveDisabled.IsMergeCandidate);
        Assert.Equal("discovery.file-type-disabled", effectiveDisabled.SkipReason?.Code);

        InputFile effectiveUnsupported = Assert.Single(
            result.Session.Files,
            x => x.FullPath == unsupportedFile.FullPath);
        Assert.False(effectiveUnsupported.IsIncluded);
        Assert.False(effectiveUnsupported.IsMergeCandidate);
        Assert.Equal("discovery.unsupported-file-type", effectiveUnsupported.SkipReason?.Code);

        InputFile effectiveProfileExcluded = Assert.Single(
            result.Session.Files,
            x => x.FullPath == profileExcludedFile.FullPath);
        Assert.False(effectiveProfileExcluded.IsIncluded);
        Assert.Equal("filter.rule.exclude", effectiveProfileExcluded.SkipReason?.Code);

        InputFile effectiveManualExcluded = Assert.Single(
            result.Session.Files,
            x => x.FullPath == manualExcludedFile.FullPath);
        Assert.False(effectiveManualExcluded.IsIncluded);
        Assert.Equal("manual.exclude", effectiveManualExcluded.SkipReason?.Code);

        InputFile effectiveBroken = Assert.Single(
            result.Session.Files,
            x => x.FullPath == brokenFile.FullPath);
        Assert.True(effectiveBroken.IsMergeCandidate);
        Assert.False(effectiveBroken.IsIncluded);
        Assert.Equal("file.read.failed", effectiveBroken.SkipReason?.Code);

        Assert.Contains(result.ValidationIssues, x => x.Code == "file.read.failed");

        (MergeSession Session, IReadOnlyCollection<MergeSection> Sections, TimeSpan TotalDuration, IReadOnlyCollection<InputFile> SourceExcludedFiles) buildCall = Assert.Single(builder.BuildCalls);
        Assert.Equal(7, buildCall.Session.Files.Count);
        Assert.Equal(2, buildCall.Sections.Count);
        Assert.Equal(
            buildCall.Session.Files
                .Where(x => x.IsIncluded)
                .Select(x => x.FullPath)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            buildCall.Sections
                .Select(x => x.SourceFile.FullPath)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray());

        Assert.DoesNotContain(result.AutomaticFiles, x => x.FullPath == sourceExcludedFile.FullPath);
        Assert.DoesNotContain(result.Session.Files, x => x.FullPath == sourceExcludedFile.FullPath);
        Assert.Same(sourceExcludedFile, Assert.Single(result.SourceExcludedFiles));
        Assert.Same(sourceExcludedFile, Assert.Single(buildCall.SourceExcludedFiles));
    }

    private static MergeSession CreateSession(
        bool includeFileSeparators = true,
        bool includeRelativePathInSeparator = true,
        InputEncodingMode inputEncodingMode = InputEncodingMode.Auto,
        string? preferredInputEncodingName = null,
        string? fallbackInputEncodingName = null)
    {
        var profile = new MergeProfile(
            name: "Default",
            generalOptions: new GeneralMergeOptions(
                includeFileSeparators: includeFileSeparators,
                includeRelativePathInSeparator: includeRelativePathInSeparator,
                inputEncodingMode: inputEncodingMode,
                preferredInputEncodingName: preferredInputEncodingName,
                fallbackInputEncodingName: fallbackInputEncodingName),
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

    private static BuildMergePreviewUseCase CreateUseCase()
    {
        return new BuildMergePreviewUseCase(
            new FakeMergeSessionValidator([]),
            new FakeFileDiscoveryService(),
            new FakeFileFilterService(),
            new FakeFileInclusionOverrideService(),
            new FakeContentReader(),
            new FakeContentTransformationService(),
            new FakeMergeBuilder());
    }

    private sealed class FakeMergeSessionValidator(IReadOnlyCollection<ValidationIssue> result) : IMergeSessionValidator
    {
        private readonly IReadOnlyCollection<ValidationIssue> _result = result;

        public IReadOnlyCollection<ValidationIssue> Validate(MergeSession session) => _result;
    }

    private sealed class FakeFileDiscoveryService(
        IReadOnlyCollection<InputFile>? result = null,
        IReadOnlyCollection<FileDiscoveryProgress>? progressEvents = null,
        IReadOnlyCollection<InputFile>? sourceExcludedFiles = null) : IFileDiscoveryService
    {
        private readonly FileDiscoveryResult _result = new(
            result ?? [],
            sourceExcludedFiles);

        private readonly IReadOnlyCollection<FileDiscoveryProgress> _progressEvents =
            progressEvents ?? [];

        public List<(IReadOnlyCollection<MergeSource> Sources, MergeProfile Profile)> DiscoverCalls { get; } = [];

        public FileDiscoveryResult DiscoverFiles(
            IReadOnlyCollection<MergeSource> sources,
            MergeProfile profile,
            IProgress<FileDiscoveryProgress>? progress = null)
        {
            DiscoverCalls.Add((sources, profile));

            foreach (FileDiscoveryProgress progressEvent in _progressEvents)
            {
                progress?.Report(progressEvent);
            }

            return _result;
        }
    }

    private sealed class FakeFileFilterService(IReadOnlyCollection<InputFile>? result = null) : IFileFilterService
    {
        private readonly IReadOnlyCollection<InputFile> _result = result ?? [];

        public List<(IReadOnlyCollection<InputFile> Files, MergeProfile Profile)> ApplyCalls { get; } = [];

        public IReadOnlyCollection<InputFile> ApplyFilters(
            IReadOnlyCollection<InputFile> files,
            MergeProfile profile)
        {
            ApplyCalls.Add((files, profile));
            return _result;
        }
    }

    private sealed class FakeFileInclusionOverrideService(IReadOnlyCollection<InputFile>? result = null) : IFileInclusionOverrideService
    {
        private readonly IReadOnlyCollection<InputFile>? _result = result;

        public List<(IReadOnlyCollection<InputFile> Files, IReadOnlyCollection<FileInclusionOverride> Overrides)> ApplyCalls { get; } = [];

        public IReadOnlyCollection<InputFile> ApplyOverrides(
            IReadOnlyCollection<InputFile> files,
            IReadOnlyCollection<FileInclusionOverride> overrides)
        {
            ApplyCalls.Add((files, overrides));
            return _result ?? files;
        }
    }

    private sealed class FakeContentReader(ContentReadResult? result = null) : IContentReader
    {
        private readonly ContentReadResult _result =
            result ?? ContentReadResult.Success(string.Empty, "utf-8");

        public List<(InputFile File, InputReadOptions Options)> ReadCalls { get; } = [];

        public ContentReadResult Read(InputFile file, InputReadOptions options)
        {
            ReadCalls.Add((file, options));
            return _result;
        }
    }

    private sealed class DelegatingContentReader(
        Func<InputFile, InputReadOptions, ContentReadResult> read) : IContentReader
    {
        private readonly Func<InputFile, InputReadOptions, ContentReadResult> _read =
            read ?? throw new ArgumentNullException(nameof(read));

        public List<(InputFile File, InputReadOptions Options)> ReadCalls { get; } = [];

        public ContentReadResult Read(InputFile file, InputReadOptions options)
        {
            ReadCalls.Add((file, options));
            return _read(file, options);
        }
    }

    private sealed class FakeContentTransformationService(string result = "") : IContentTransformationService
    {
        private readonly string _result = result;

        public List<(string Content, InputFile File, MergeProfile Profile)> TransformCalls { get; } = [];

        public string Transform(string content, InputFile file, MergeProfile profile)
        {
            TransformCalls.Add((content, file, profile));
            return _result;
        }
    }

    private sealed class FakeMergeBuilder(MergeOutput? result = null) : IMergeBuilder
    {
        private readonly MergeOutput _result = result ?? new MergeOutput(
            content: "",
            sections: [],
            statistics: new MergeStatistics(0, 0, 0, 0, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));

        public List<(MergeSession Session, IReadOnlyCollection<MergeSection> Sections, TimeSpan TotalDuration, IReadOnlyCollection<InputFile> SourceExcludedFiles)> BuildCalls { get; } = [];

        public MergeOutput Build(
            MergeSession session,
            IReadOnlyCollection<MergeSection> sections,
            TimeSpan totalDuration,
            IReadOnlyCollection<InputFile>? sourceExcludedFiles = null)
        {
            BuildCalls.Add((session, sections, totalDuration, sourceExcludedFiles ?? []));
            return _result;
        }
    }

    private sealed class CapturingProgress<T>(Action<T> handler) : IProgress<T>
    {
        private readonly Action<T> _handler = handler;

        public void Report(T value)
        {
            _handler(value);
        }
    }

    private sealed class CancellingContentReader(
        CancellationTokenSource cancellationTokenSource) : IContentReader
    {
        private readonly CancellationTokenSource _cancellationTokenSource =
            cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));

        public ContentReadResult Read(InputFile file, InputReadOptions options)
        {
            _cancellationTokenSource.Cancel();
            return ContentReadResult.Success("raw content", "utf-8");
        }
    }
}