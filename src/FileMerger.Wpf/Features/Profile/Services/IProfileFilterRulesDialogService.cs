using FileMerger.Wpf.Features.Profile.ViewModels;

namespace FileMerger.Wpf.Features.Profile.Services;

public interface IProfileFilterRulesDialogService
{
    void Show(ProfileEditorViewModel editor);
}