using FileMerger.Wpf.Shared.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeSaveFileDialogService : ISaveFileDialogService
{
    public string? SelectedPath { get; init; }

    public int SelectSaveFilePathCallCount { get; private set; }

    public int SelectSaveFilePathCalls => SelectSaveFilePathCallCount;

    public string? LastInitialPath { get; private set; }

    public string? LastFilter { get; private set; }

    public string? InitialPath => LastInitialPath;

    public string? Filter => LastFilter;

    public string? SelectSaveFilePath(string? initialPath = null, string? filter = null)
    {
        SelectSaveFilePathCallCount++;
        LastInitialPath = initialPath;
        LastFilter = filter;

        return SelectedPath;
    }
}