namespace FileMerger.Wpf.Features.Files.ViewModels;

public enum FileListFacet
{
    Included = 0,
    NotIncluded = 1,
    ExcludedByProfileRule = 2,
    DisabledType = 3,
    Unsupported = 4,
    Fallback = 5,
    Overridden = 6,
    NotApplied = 7
}