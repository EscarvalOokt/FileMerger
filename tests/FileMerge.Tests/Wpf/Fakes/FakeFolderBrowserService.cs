using FileMerger.Wpf.Shared.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeFolderBrowserService : IFolderBrowserService
{
    public IReadOnlyList<string> SelectedFolders { get; init; } = [];

    public int SelectFoldersCallCount { get; private set; }

    public string? LastInitialDirectory { get; private set; }

    public string? InitialDirectory => LastInitialDirectory;

    public IReadOnlyList<string> SelectFolders(string? initialDirectory = null)
    {
        SelectFoldersCallCount++;
        LastInitialDirectory = initialDirectory;

        return SelectedFolders;
    }
}