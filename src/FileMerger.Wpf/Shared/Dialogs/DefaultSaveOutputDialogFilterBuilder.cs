namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class DefaultSaveOutputDialogFilterBuilder : ISaveOutputDialogFilterBuilder
{
    private readonly ISaveOutputFileTypeCatalog _saveOutputFileTypeCatalog;

    public DefaultSaveOutputDialogFilterBuilder(ISaveOutputFileTypeCatalog saveOutputFileTypeCatalog)
    {
        ArgumentNullException.ThrowIfNull(saveOutputFileTypeCatalog);
        _saveOutputFileTypeCatalog = saveOutputFileTypeCatalog;
    }

    public string BuildDefaultSaveFileFilter()
    {
        IReadOnlyCollection<SaveOutputFileTypeDefinition> fileTypes = _saveOutputFileTypeCatalog.GetAll();

        if (fileTypes.Count == 0)
            return "All files (*.*)|*.*";

        string typedFilters = string.Join(
            '|',
            fileTypes.GroupBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .Select(x => $"{x.DisplayName} files ({x.Pattern})|{x.Pattern}"));

        return $"{typedFilters}|All files (*.*)|*.*";
    }

    public string GetDefaultExtension()
    {
        return _saveOutputFileTypeCatalog.GetDefault().Extension;
    }
}