namespace FileMerger.Wpf.Features.Preview.State;

public sealed record PreviewFileTypeStateSnapshot(
    string Extension,
    bool IsEnabled);