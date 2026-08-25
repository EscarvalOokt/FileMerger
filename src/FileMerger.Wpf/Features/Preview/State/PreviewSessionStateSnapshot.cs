namespace FileMerger.Wpf.Features.Preview.State;

public sealed record PreviewSessionStateSnapshot(
    string SessionName,
    string OutputPath);