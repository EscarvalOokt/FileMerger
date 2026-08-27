namespace FileMerger.Domain.Enums;

public enum SkippedFileCategory
{
    DisabledFileType = 0,
    UnsupportedFile = 1,
    ProfileExclusion = 2,
    ManualExclusion = 3,
    SourceExclusion = 4,
    ProcessingFailure = 5,
    Other = 6
}