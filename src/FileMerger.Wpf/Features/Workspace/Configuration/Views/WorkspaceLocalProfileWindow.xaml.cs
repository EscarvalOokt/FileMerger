using System.Windows;
using FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Configuration.Views;

public partial class WorkspaceLocalProfileWindow
{
    private WorkspaceLocalProfileDialogViewModel? _viewModel;

    public WorkspaceLocalProfileWindow()
    {
        InitializeComponent();

        DataContextChanged += WorkspaceLocalProfileWindow_DataContextChanged;
        Closed += WorkspaceLocalProfileWindow_Closed;
    }

    private void WorkspaceLocalProfileWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModel?.RequestClose -= ViewModel_RequestClose;

        _viewModel = e.NewValue as WorkspaceLocalProfileDialogViewModel;

        _viewModel?.RequestClose += ViewModel_RequestClose;
    }

    private void WorkspaceLocalProfileWindow_Closed(object? sender, EventArgs e)
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