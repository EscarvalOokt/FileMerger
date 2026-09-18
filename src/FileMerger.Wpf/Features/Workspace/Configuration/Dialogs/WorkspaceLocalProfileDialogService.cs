using System.Windows;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;
using FileMerger.Wpf.Features.Workspace.Configuration.Views;
using FileMerger.Wpf.Shell.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;

public sealed class WorkspaceLocalProfileDialogService : IWorkspaceLocalProfileDialogService
{
    private readonly IWindowOwnerResolver _ownerResolver;
    private readonly IProfileEditorFactory _profileEditorFactory;
    private readonly IServiceProvider _serviceProvider;

    public WorkspaceLocalProfileDialogService(
        IServiceProvider serviceProvider,
        IWindowOwnerResolver ownerResolver,
        IProfileEditorFactory profileEditorFactory)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(ownerResolver);
        ArgumentNullException.ThrowIfNull(profileEditorFactory);

        _serviceProvider = serviceProvider;
        _ownerResolver = ownerResolver;
        _profileEditorFactory = profileEditorFactory;
    }

    public Task<WorkspaceLocalProfileEditResult?> ShowDialogAsync(string profileName, WorkspaceProfileDto profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        WorkspaceLocalProfileWindow window = _serviceProvider.GetRequiredService<WorkspaceLocalProfileWindow>();
        WorkspaceLocalProfileDialogViewModel viewModel = new(profileName, profile, _profileEditorFactory);

        window.DataContext = viewModel;

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        bool? result = window.ShowDialog();

        return Task.FromResult(result == true ? viewModel.Result : null);
    }
}