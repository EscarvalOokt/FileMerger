using FileMerger.Domain.ValueObjects;

namespace FileMerger.Domain.Profiles;

public sealed class BuiltInFileTypeCatalog : IFileTypeCatalog
{
    public IReadOnlyCollection<FileTypeDefinition> GetAll()
    {
        return KnownFileTypes.All;
    }

    public IReadOnlyCollection<FileTypeDefinition> GetDefault()
    {
        return KnownFileTypes.Default;
    }

    public FileTypeDefinition? FindByExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        return KnownFileTypes.All.FirstOrDefault(x => string.Equals(x.Extension, extension, StringComparison.OrdinalIgnoreCase));
    }
}