using FileMerger.Wpf.Features.Sources.ViewModels;

namespace FileMerger.Wpf.Features.Sources.Dialogs;

public interface ISourceDetailsDialogService
{
    void Show(MergeSourceItemViewModel source);
}