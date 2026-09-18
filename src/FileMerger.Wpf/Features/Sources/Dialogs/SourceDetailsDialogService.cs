using System.Windows;
using FileMerger.Wpf.Features.Sources.ViewModels;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Features.Sources.Dialogs;

public sealed class SourceDetailsDialogService : ISourceDetailsDialogService
{
    private readonly IFolderBrowserService _folderBrowserService;
    private readonly IOpenFileDialogService _openFileDialogService;
    private readonly IWindowOwnerResolver _ownerResolver;

    public SourceDetailsDialogService(
        IWindowOwnerResolver ownerResolver,
        IFolderBrowserService folderBrowserService,
        IOpenFileDialogService openFileDialogService)
    {
        ArgumentNullException.ThrowIfNull(ownerResolver);
        ArgumentNullException.ThrowIfNull(folderBrowserService);
        ArgumentNullException.ThrowIfNull(openFileDialogService);

        _ownerResolver = ownerResolver;
        _folderBrowserService = folderBrowserService;
        _openFileDialogService = openFileDialogService;
    }

    public void Show(MergeSourceItemViewModel source)
    {
        ArgumentNullException.ThrowIfNull(source);

        SourceDetailsDialogViewModel viewModel = new(source, _folderBrowserService, _openFileDialogService);

        SourceDetailsWindow window = new()
        {
            DataContext = viewModel
        };

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        window.ShowDialog();
    }
}