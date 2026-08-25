using System.IO;
using Microsoft.Win32;

namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class SaveFileDialogService : ISaveFileDialogService
{
    private readonly ISaveOutputDialogFilterBuilder _saveOutputDialogFilterBuilder;

    public SaveFileDialogService(
        ISaveOutputDialogFilterBuilder saveOutputDialogFilterBuilder)
    {
        ArgumentNullException.ThrowIfNull(saveOutputDialogFilterBuilder);
        _saveOutputDialogFilterBuilder = saveOutputDialogFilterBuilder;
    }

    public string? SelectSaveFilePath(string? initialPath = null, string? filter = null)
    {
        bool useDefaultFilter = string.IsNullOrWhiteSpace(filter);

        SaveFileDialog dialog = new()
        {
            Filter = useDefaultFilter
                ? _saveOutputDialogFilterBuilder.BuildDefaultSaveFileFilter()
                : filter!,
            AddExtension = true,
            OverwritePrompt = true
        };

        if (useDefaultFilter)
        {
            dialog.DefaultExt = _saveOutputDialogFilterBuilder
                .GetDefaultExtension()
                .TrimStart('.');
        }

        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            dialog.FileName = Path.GetFileName(initialPath);

            string? directory = Path.GetDirectoryName(initialPath);
            if (!string.IsNullOrWhiteSpace(directory))
                dialog.InitialDirectory = directory;
        }

        bool? result = dialog.ShowDialog();
        return result == true
            ? dialog.FileName
            : null;
    }
}