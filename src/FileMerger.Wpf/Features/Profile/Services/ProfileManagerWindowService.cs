using System.Windows;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Profile.Views;
using FileMerger.Wpf.Shell.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf.Features.Profile.Services;

public sealed class ProfileManagerWindowService : IProfileManagerWindowService
{
    private readonly IWindowOwnerResolver _ownerResolver;
    private readonly IServiceProvider _serviceProvider;

    public ProfileManagerWindowService(IServiceProvider serviceProvider, IWindowOwnerResolver ownerResolver)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(ownerResolver);

        _serviceProvider = serviceProvider;
        _ownerResolver = ownerResolver;
    }

    public Task ShowDialogAsync()
    {
        ProfileManagerViewModel viewModel = _serviceProvider.GetRequiredService<ProfileManagerViewModel>();
        return ShowDialogAsync(viewModel);
    }

    public Task ShowDialogAsync(ICurrentSessionProfileHost profileHost, ProfileManagerContext context)
    {
        ArgumentNullException.ThrowIfNull(profileHost);

        ProfileManagerViewModel viewModel = ActivatorUtilities.CreateInstance<ProfileManagerViewModel>(
            _serviceProvider,
            profileHost,
            context);

        return ShowDialogAsync(viewModel);
    }

    private async Task ShowDialogAsync(ProfileManagerViewModel viewModel)
    {
        ProfileManagerWindow window = _serviceProvider.GetRequiredService<ProfileManagerWindow>();
        window.DataContext = viewModel;

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        await viewModel.InitializeAsync();
        window.ShowDialog();
    }
}