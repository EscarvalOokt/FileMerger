namespace FileMerger.Application.UseCases.BuildPreview;

public sealed record BuildMergePreviewProgress(
    BuildMergePreviewStage Stage,
    int Current,
    int Total,
    string Message);