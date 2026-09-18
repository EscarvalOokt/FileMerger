namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class BuiltInSaveOutputFileTypeCatalog : ISaveOutputFileTypeCatalog
{
    private static readonly SaveOutputFileTypeDefinition Text = new(".txt", "Text", isDefault: true);

    private static readonly SaveOutputFileTypeDefinition Markdown = new(".md", "Markdown");

    private static readonly SaveOutputFileTypeDefinition Log = new(".log", "Log");

    private static readonly IReadOnlyCollection<SaveOutputFileTypeDefinition> _all =
    [
        Text,
        Markdown,
        Log
    ];

    public IReadOnlyCollection<SaveOutputFileTypeDefinition> GetAll()
    {
        return _all;
    }

    public SaveOutputFileTypeDefinition GetDefault()
    {
        return _all.First(x => x.IsDefault);
    }
}