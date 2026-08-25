using System.Windows;
using FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Configuration.Views;

public partial class WorkspaceConfigurationWindow
{
    private WorkspaceConfigurationDialogViewModel? _viewModel;

    public WorkspaceConfigurationWindow()
    {
        InitializeComponent();

        DataContextChanged += WorkspaceConfigurationWindow_DataContextChanged;
        Closed += WorkspaceConfigurationWindow_Closed;
    }

    private void WorkspaceConfigurationWindow_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        _viewModel?.RequestClose -= ViewModel_RequestClose;

        _viewModel = e.NewValue as WorkspaceConfigurationDialogViewModel;

        _viewModel?.RequestClose += ViewModel_RequestClose;
    }

    private void WorkspaceConfigurationWindow_Closed(object? sender, EventArgs e)
    {
        _viewModel?.RequestClose -= ViewModel_RequestClose;
        _viewModel = null;
    }

    private void ViewModel_RequestClose(object? sender, bool? result)
    {
        DialogResult = result;

        if (IsVisible)
            Close();
    }
}