using System.Windows;
using FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;
using FileMerger.Wpf.Features.Workspace.Configuration.Views;
using FileMerger.Wpf.Shell.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;

public sealed class WorkspaceConfigurationDialogService : IWorkspaceConfigurationDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowOwnerResolver _ownerResolver;
    private readonly IWorkspaceDocumentFactory _documentFactory;
    private readonly IWorkspaceDocumentDirtyStateService _dirtyStateService;
    private readonly IWorkspaceDocumentOutputService _outputService;

    public WorkspaceConfigurationDialogService(
        IServiceProvider serviceProvider,
        IWindowOwnerResolver ownerResolver,
        IWorkspaceDocumentFactory documentFactory,
        IWorkspaceDocumentDirtyStateService dirtyStateService,
        IWorkspaceDocumentOutputService outputService)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(ownerResolver);
        ArgumentNullException.ThrowIfNull(documentFactory);
        ArgumentNullException.ThrowIfNull(dirtyStateService);
        ArgumentNullException.ThrowIfNull(outputService);

        _serviceProvider = serviceProvider;
        _ownerResolver = ownerResolver;
        _documentFactory = documentFactory;
        _dirtyStateService = dirtyStateService;
        _outputService = outputService;
    }

    public Task<bool> ShowDialogAsync(WorkspaceDocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        WorkspaceConfigurationWindow window =
            _serviceProvider.GetRequiredService<WorkspaceConfigurationWindow>();

        WorkspaceConfigurationDialogViewModel viewModel = new(
            document,
            _documentFactory,
            _dirtyStateService,
            _outputService);

        window.DataContext = viewModel;

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        bool? result = window.ShowDialog();

        return Task.FromResult(result == true);
    }
}