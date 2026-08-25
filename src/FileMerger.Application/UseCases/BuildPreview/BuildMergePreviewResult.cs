using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.UseCases.BuildPreview;

public sealed record BuildMergePreviewResult
{
    public BuildMergePreviewResult(
        MergeSession session,
        MergeOutput? output,
        IReadOnlyCollection<InputFile> automaticFiles,
        IReadOnlyCollection<ValidationIssue> validationIssues,
        bool isSuccessful,
        IReadOnlyCollection<InputFile>? sourceExcludedFiles = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(automaticFiles);
        ArgumentNullException.ThrowIfNull(validationIssues);

        Session = session;
        Output = output;
        AutomaticFiles = automaticFiles;
        ValidationIssues = validationIssues;
        IsSuccessful = isSuccessful;
        SourceExcludedFiles = sourceExcludedFiles ?? [];
    }

    public MergeSession Session { get; }
    public MergeOutput? Output { get; }
    public IReadOnlyCollection<InputFile> AutomaticFiles { get; }
    public IReadOnlyCollection<ValidationIssue> ValidationIssues { get; }
    public bool IsSuccessful { get; }
    public IReadOnlyCollection<InputFile> SourceExcludedFiles { get; }
}