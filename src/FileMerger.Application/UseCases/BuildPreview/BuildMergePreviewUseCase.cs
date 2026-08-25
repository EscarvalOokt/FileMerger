using System.Diagnostics;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.UseCases.BuildPreview;

public sealed class BuildMergePreviewUseCase
{
    private readonly IMergeSessionValidator _validator;
    private readonly IFileDiscoveryService _fileDiscoveryService;
    private readonly IFileFilterService _fileFilterService;
    private readonly IFileInclusionOverrideService _fileInclusionOverrideService;
    private readonly IContentReader _contentReader;
    private readonly IContentTransformationService _contentTransformationService;
    private readonly IMergeBuilder _mergeBuilder;

    public BuildMergePreviewUseCase(
        IMergeSessionValidator validator,
        IFileDiscoveryService fileDiscoveryService,
        IFileFilterService fileFilterService,
        IFileInclusionOverrideService fileInclusionOverrideService,
        IContentReader contentReader,
        IContentTransformationService contentTransformationService,
        IMergeBuilder mergeBuilder)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fileDiscoveryService);
        ArgumentNullException.ThrowIfNull(fileFilterService);
        ArgumentNullException.ThrowIfNull(fileInclusionOverrideService);
        ArgumentNullException.ThrowIfNull(contentReader);
        ArgumentNullException.ThrowIfNull(contentTransformationService);
        ArgumentNullException.ThrowIfNull(mergeBuilder);

        _validator = validator;
        _fileDiscoveryService = fileDiscoveryService;
        _fileFilterService = fileFilterService;
        _fileInclusionOverrideService = fileInclusionOverrideService;
        _contentReader = contentReader;
        _contentTransformationService = contentTransformationService;
        _mergeBuilder = mergeBuilder;
    }

    public Task<BuildMergePreviewResult> ExecuteAsync(
        BuildMergePreviewRequest request,
        IProgress<BuildMergePreviewProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            cancellationToken.ThrowIfCancellationRequested();

            MergeSession session = request.Session;

            progress?.Report(new BuildMergePreviewProgress(
                Stage: BuildMergePreviewStage.Validating,
                Current: 0,
                Total: 1,
                Message: "Validating session."));

            List<ValidationIssue> validationIssues = [.. _validator.Validate(session)];
            MergeSession validatedSession = session.WithValidationIssues(validationIssues);

            bool hasErrors = validationIssues.Any(x => x.Severity == ValidationSeverity.Error);
            if (hasErrors)
            {
                return new BuildMergePreviewResult(
                    session: validatedSession,
                    output: null,
                    automaticFiles: [],
                    validationIssues: validationIssues,
                    isSuccessful: false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new BuildMergePreviewProgress(
                Stage: BuildMergePreviewStage.DiscoveringFiles,
                Current: 0,
                Total: 0,
                Message: "Discovering files."));

            IProgress<FileDiscoveryProgress>? discoveryProgress = progress is null
                ? null
                : new DiscoveryProgressAdapter(progress);

            FileDiscoveryResult discoveryResult =
                _fileDiscoveryService.DiscoverFiles(
                    validatedSession.Sources,
                    validatedSession.Profile,
                    discoveryProgress);

            IReadOnlyCollection<InputFile> mergeCandidates = discoveryResult.MergeCandidates;

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new BuildMergePreviewProgress(
                Stage: BuildMergePreviewStage.FilteringFiles,
                Current: 0,
                Total: mergeCandidates.Count,
                Message: $"Applying filters to {mergeCandidates.Count} file(s)."));

            IReadOnlyCollection<InputFile> filteredFiles =
                _fileFilterService.ApplyFilters(mergeCandidates, validatedSession.Profile);

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new BuildMergePreviewProgress(
                Stage: BuildMergePreviewStage.ApplyingOverrides,
                Current: 0,
                Total: filteredFiles.Count,
                Message: "Applying manual file overrides."));

            IReadOnlyCollection<InputFile> overriddenFiles =
                _fileInclusionOverrideService.ApplyOverrides(
                    filteredFiles,
                    request.InclusionOverrides);

            MergeSession sessionWithFiles = validatedSession.WithFiles(overriddenFiles);

            List<ValidationIssue> allIssues = [.. validationIssues];
            List<MergeSection> sections = [];

            InputReadOptions readOptions = new(
                sessionWithFiles.Profile.GeneralOptions.InputEncodingMode,
                sessionWithFiles.Profile.GeneralOptions.PreferredInputEncodingName,
                sessionWithFiles.Profile.GeneralOptions.FallbackInputEncodingName);

            InputFile[] includedFiles = [.. sessionWithFiles.Files.Where(x => x.IsIncluded)];

            for (int i = 0; i < includedFiles.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                InputFile file = includedFiles[i];

                progress?.Report(new BuildMergePreviewProgress(
                    Stage: BuildMergePreviewStage.ReadingFiles,
                    Current: i + 1,
                    Total: includedFiles.Length,
                    Message: $"Reading {i + 1}/{includedFiles.Length}: {file.RelativePath}"));

                ContentReadResult readResult = _contentReader.Read(file, readOptions);
                if (!readResult.IsSuccessful)
                {
                    if (readResult.Issue is not null)
                        allIssues.Add(readResult.Issue);

                    continue;
                }

                string transformedContent = _contentTransformationService.Transform(
                    readResult.Content ?? string.Empty,
                    file,
                    sessionWithFiles.Profile);

                string? headerText = sessionWithFiles.Profile.GeneralOptions.IncludeFileSeparators
                    ? BuildSectionHeaderText(file, readResult.EncodingName, sessionWithFiles)
                    : null;

                sections.Add(new MergeSection(
                    sourceFile: file,
                    content: transformedContent,
                    order: sections.Count,
                    headerText: headerText));
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new BuildMergePreviewProgress(
                Stage: BuildMergePreviewStage.BuildingOutput,
                Current: 0,
                Total: 1,
                Message: "Building merged output."));

            MergeOutput output = _mergeBuilder.Build(
                sessionWithFiles,
                sections,
                stopwatch.Elapsed,
                discoveryResult.SourceExcludedFiles);

            MergeSession finalSession = sessionWithFiles
                .WithValidationIssues(allIssues)
                .WithLastOutput(output);

            progress?.Report(new BuildMergePreviewProgress(
                Stage: BuildMergePreviewStage.Completed,
                Current: 1,
                Total: 1,
                Message: $"Preview built. Sections: {sections.Count}"));

            IReadOnlyCollection<InputFile> automaticFiles = BuildAutomaticInventory(
                discoveryResult.InventoryFiles,
                filteredFiles);

            return new BuildMergePreviewResult(
                session: finalSession,
                output: output,
                automaticFiles: automaticFiles,
                validationIssues: allIssues,
                isSuccessful: true,
                sourceExcludedFiles: discoveryResult.SourceExcludedFiles);
        }, cancellationToken);
    }


    private static IReadOnlyCollection<InputFile> BuildAutomaticInventory(
        IReadOnlyCollection<InputFile> inventoryFiles,
        IReadOnlyCollection<InputFile> filteredFiles)
    {
        var filteredMap = filteredFiles
            .GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.Last(),
                StringComparer.OrdinalIgnoreCase);

        return
        [
            .. inventoryFiles.Select(file =>
                filteredMap.GetValueOrDefault(file.FullPath, file))
        ];
    }

    private sealed class DiscoveryProgressAdapter(
        IProgress<BuildMergePreviewProgress> progress) : IProgress<FileDiscoveryProgress>
    {
        private readonly IProgress<BuildMergePreviewProgress> _progress =
            progress ?? throw new ArgumentNullException(nameof(progress));

        public void Report(FileDiscoveryProgress value)
        {
            ArgumentNullException.ThrowIfNull(value);

            _progress.Report(new BuildMergePreviewProgress(
                Stage: BuildMergePreviewStage.DiscoveringFiles,
                Current: value.ProbedFiles,
                Total: 0,
                Message: $"Probing unsupported file {value.ProbedFiles}: {value.RelativePath}"));
        }
    }

    private static string BuildSectionHeaderText(
        InputFile file,
        string? encodingName,
        MergeSession session)
    {
        if (!session.Profile.GeneralOptions.IncludeRelativePathInSeparator)
        {
            return string.IsNullOrWhiteSpace(encodingName)
                ? Path.GetFileName(file.FullPath)
                : $"{Path.GetFileName(file.FullPath)} [{encodingName}]";
        }

        return string.IsNullOrWhiteSpace(encodingName)
            ? file.RelativePath
            : $"{file.RelativePath} [{encodingName}]";
    }
}