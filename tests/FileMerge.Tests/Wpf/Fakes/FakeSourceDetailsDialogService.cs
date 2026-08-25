using FileMerger.Wpf.Features.Sources.Dialogs;
using FileMerger.Wpf.Features.Sources.ViewModels;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeSourceDetailsDialogService : ISourceDetailsDialogService
{
    public int ShowCallCount { get; private set; }

    public MergeSourceItemViewModel? LastSource { get; private set; }

    public List<MergeSourceItemViewModel> ShownSources { get; } = [];

    public void Show(MergeSourceItemViewModel source)
    {
        ShowCallCount++;
        LastSource = source;
        ShownSources.Add(source);
    }
}