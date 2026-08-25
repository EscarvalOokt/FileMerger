namespace FileMerger.Wpf.Shared.Dialogs;

public interface ISaveOutputFileTypeCatalog
{
    IReadOnlyCollection<SaveOutputFileTypeDefinition> GetAll();
    SaveOutputFileTypeDefinition GetDefault();
}