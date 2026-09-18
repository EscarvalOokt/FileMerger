using Microsoft.Win32;

namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class FolderBrowserService : IFolderBrowserService
{
    public IReadOnlyList<string> SelectFolders(string? initialDirectory = null)
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = true
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        bool? result = dialog.ShowDialog();
        return result == true ? dialog.FolderNames.ToArray() : [];
    }
}