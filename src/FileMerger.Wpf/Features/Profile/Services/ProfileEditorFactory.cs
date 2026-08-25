using FileMerger.Domain.Profiles;
using FileMerger.Wpf.Features.Profile.ViewModels;

namespace FileMerger.Wpf.Features.Profile.Services;

public sealed class ProfileEditorFactory : IProfileEditorFactory
{
    private readonly IFileTypeCatalog _fileTypeCatalog;

    public ProfileEditorFactory(IFileTypeCatalog fileTypeCatalog)
    {
        ArgumentNullException.ThrowIfNull(fileTypeCatalog);
        _fileTypeCatalog = fileTypeCatalog;
    }

    public ProfileEditorViewModel Create()
    {
        ProfileEditorViewModel editor = new(_fileTypeCatalog);
        editor.LoadDefaults();
        return editor;
    }
}