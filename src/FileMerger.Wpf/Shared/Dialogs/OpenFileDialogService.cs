using System.IO;
using Microsoft.Win32;

namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class OpenFileDialogService : IOpenFileDialogService
{
    private readonly IFileDialogFilterBuilder _fileDialogFilterBuilder;

    public OpenFileDialogService(IFileDialogFilterBuilder fileDialogFilterBuilder)
    {
        ArgumentNullException.ThrowIfNull(fileDialogFilterBuilder);
        _fileDialogFilterBuilder = fileDialogFilterBuilder;
    }

    public IReadOnlyList<string> SelectFiles(string? initialPath = null, string? filter = null)
    {
        OpenFileDialog dialog = CreateDialog(
            initialPath: initialPath,
            filter: filter,
            multiselect: true);

        bool? result = dialog.ShowDialog();
        return result == true
            ? dialog.FileNames.ToArray()
            : [];
    }

    public string? SelectFile(string? initialPath = null, string? filter = null)
    {
        OpenFileDialog dialog = CreateDialog(
            initialPath: initialPath,
            filter: filter,
            multiselect: false);

        bool? result = dialog.ShowDialog();
        return result == true
            ? dialog.FileName
            : null;
    }

    private OpenFileDialog CreateDialog(
        string? initialPath,
        string? filter,
        bool multiselect)
    {
        OpenFileDialog dialog = new()
        {
            Filter = string.IsNullOrWhiteSpace(filter)
                ? _fileDialogFilterBuilder.BuildDefaultOpenFileFilter()
                : filter,
            Multiselect = multiselect
        };

        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            if (Directory.Exists(initialPath))
            {
                dialog.InitialDirectory = initialPath;
            }
            else
            {
                dialog.FileName = Path.GetFileName(initialPath);

                string? directory = Path.GetDirectoryName(initialPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    dialog.InitialDirectory = directory;
            }
        }

        return dialog;
    }
}