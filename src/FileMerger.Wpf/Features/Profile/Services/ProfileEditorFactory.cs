using FileMerger.Domain.Profiles;
using FileMerger.Wpf.Features.Profile.ViewModels;

namespace FileMerger.Wpf.Features.Profile.Services;

public sealed class ProfileEditorFactory : IProfileEditorFactory
{
    private readonly IFileTypeCatalog _fileTypeCatalog;
    private readonly IProfileFilterRulesDialogService _filterRulesDialogService;

    public ProfileEditorFactory(
        IFileTypeCatalog fileTypeCatalog,
        IProfileFilterRulesDialogService filterRulesDialogService)
    {
        ArgumentNullException.ThrowIfNull(fileTypeCatalog);
        ArgumentNullException.ThrowIfNull(filterRulesDialogService);

        _fileTypeCatalog = fileTypeCatalog;
        _filterRulesDialogService = filterRulesDialogService;
    }

    public ProfileEditorViewModel Create()
    {
        ProfileEditorViewModel editor = new(_fileTypeCatalog, _filterRulesDialogService);
        editor.LoadDefaults();
        return editor;
    }
}