using FileMerger.Wpf.Shared.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeOpenFileDialogService : IOpenFileDialogService
{
    public string? SelectedFile { get; init; }

    public IReadOnlyList<string> SelectedFiles { get; init; } = [];

    public int SelectFileCallCount { get; private set; }

    public int SelectFilesCallCount { get; private set; }

    public string? LastSelectFileInitialPath { get; private set; }

    public string? LastSelectFileFilter { get; private set; }

    public string? LastSelectFilesInitialPath { get; private set; }

    public string? LastSelectFilesFilter { get; private set; }

    public string? InitialPath => LastSelectFileInitialPath;

    public string? Filter => LastSelectFileFilter;

    public IReadOnlyList<string> SelectFiles(string? initialPath = null, string? filter = null)
    {
        SelectFilesCallCount++;
        LastSelectFilesInitialPath = initialPath;
        LastSelectFilesFilter = filter;

        return SelectedFiles;
    }

    public string? SelectFile(string? initialPath = null, string? filter = null)
    {
        SelectFileCallCount++;
        LastSelectFileInitialPath = initialPath;
        LastSelectFileFilter = filter;

        return SelectedFile;
    }
}