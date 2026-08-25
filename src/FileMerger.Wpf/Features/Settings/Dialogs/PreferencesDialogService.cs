using System.Windows;
using FileMerger.Wpf.Features.Settings.ViewModels;
using FileMerger.Wpf.Features.Settings.Views;
using FileMerger.Wpf.Shell.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf.Features.Settings.Dialogs;

public sealed class PreferencesDialogService : IPreferencesDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowOwnerResolver _ownerResolver;

    public PreferencesDialogService(
        IServiceProvider serviceProvider,
        IWindowOwnerResolver ownerResolver)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(ownerResolver);

        _serviceProvider = serviceProvider;
        _ownerResolver = ownerResolver;
    }

    public Task<bool> ShowDialogAsync()
    {
        PreferencesWindow window =
            _serviceProvider.GetRequiredService<PreferencesWindow>();

        PreferencesDialogViewModel viewModel =
            _serviceProvider.GetRequiredService<PreferencesDialogViewModel>();

        window.DataContext = viewModel;

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        bool? result = window.ShowDialog();

        return Task.FromResult(result == true);
    }
}