namespace FileMerger.Application.UseCases.BuildPreview;

public enum BuildMergePreviewStage
{
    Validating = 0,
    DiscoveringFiles = 1,
    FilteringFiles = 2,
    ApplyingOverrides = 3,
    ReadingFiles = 4,
    BuildingOutput = 5,
    Completed = 6
}