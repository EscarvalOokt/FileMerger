using System.Windows;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Profile.Views;
using FileMerger.Wpf.Shell.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf.Features.Profile.Services;

public sealed class ProfileManagerWindowService : IProfileManagerWindowService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowOwnerResolver _ownerResolver;

    public ProfileManagerWindowService(
        IServiceProvider serviceProvider,
        IWindowOwnerResolver ownerResolver)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(ownerResolver);

        _serviceProvider = serviceProvider;
        _ownerResolver = ownerResolver;
    }

    public async Task ShowDialogAsync()
    {
        ProfileManagerWindow window = _serviceProvider.GetRequiredService<ProfileManagerWindow>();
        ProfileManagerViewModel viewModel = _serviceProvider.GetRequiredService<ProfileManagerViewModel>();

        window.DataContext = viewModel;

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        await viewModel.InitializeAsync();
        window.ShowDialog();
    }
}