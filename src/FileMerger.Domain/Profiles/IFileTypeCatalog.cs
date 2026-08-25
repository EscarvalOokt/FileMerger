using FileMerger.Domain.ValueObjects;

namespace FileMerger.Domain.Profiles;

public interface IFileTypeCatalog
{
    IReadOnlyCollection<FileTypeDefinition> GetAll();
    IReadOnlyCollection<FileTypeDefinition> GetDefault();
    FileTypeDefinition? FindByExtension(string extension);
}