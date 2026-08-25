using FileMerger.Domain.Profiles;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class SupportedInputFileDialogFilterBuilder : IFileDialogFilterBuilder
{
    private readonly IFileTypeCatalog _fileTypeCatalog;

    public SupportedInputFileDialogFilterBuilder(IFileTypeCatalog fileTypeCatalog)
    {
        ArgumentNullException.ThrowIfNull(fileTypeCatalog);
        _fileTypeCatalog = fileTypeCatalog;
    }

    public string BuildDefaultOpenFileFilter()
    {
        IReadOnlyCollection<FileTypeDefinition> fileTypes = _fileTypeCatalog.GetAll();

        string allPatterns = string.Join(
            ';',
            fileTypes.Select(x => $"*{x.Extension}")
                .Distinct(StringComparer.OrdinalIgnoreCase));

        string allLabel = string.Join(
            ";",
            fileTypes.Select(x => $"*{x.Extension}")
                .Distinct(StringComparer.OrdinalIgnoreCase));

        List<string> parts =
        [
            $"Supported files ({allLabel})|{allPatterns}"
        ];

        foreach (FileTypeDefinition fileType in fileTypes)
            parts.Add($"{fileType.DisplayName} (*{fileType.Extension})|*{fileType.Extension}");

        parts.Add("All files (*.*)|*.*");

        return string.Join("|", parts);
    }
}