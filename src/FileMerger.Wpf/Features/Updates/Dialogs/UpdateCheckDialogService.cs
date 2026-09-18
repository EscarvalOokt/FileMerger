using System.Windows;
using FileMerger.Wpf.Features.Updates.ViewModels;
using FileMerger.Wpf.Features.Updates.Views;
using FileMerger.Wpf.Shell.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf.Features.Updates.Dialogs;

public sealed class UpdateCheckDialogService : IUpdateCheckDialogService
{
    private readonly IWindowOwnerResolver _ownerResolver;
    private readonly IServiceProvider _serviceProvider;

    public UpdateCheckDialogService(IServiceProvider serviceProvider, IWindowOwnerResolver ownerResolver)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(ownerResolver);

        _serviceProvider = serviceProvider;
        _ownerResolver = ownerResolver;
    }

    public void ShowDialog()
    {
        UpdateCheckWindow window = _serviceProvider.GetRequiredService<UpdateCheckWindow>();
        UpdateCheckDialogViewModel viewModel = _serviceProvider.GetRequiredService<UpdateCheckDialogViewModel>();

        window.DataContext = viewModel;

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        window.ShowDialog();
    }
}