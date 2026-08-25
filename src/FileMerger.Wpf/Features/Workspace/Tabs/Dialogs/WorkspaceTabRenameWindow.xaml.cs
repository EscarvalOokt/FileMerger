using System.Windows;
using System.Windows.Controls;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Dialogs;

public partial class WorkspaceTabRenameWindow : ShellDialogWindow
{
    public WorkspaceTabRenameWindow()
    {
        InitializeComponent();
    }

    private void WorkspaceNameTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        textBox.Focus();
        textBox.SelectAll();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceTabRenameViewModel viewModel)
            return;

        if (!viewModel.CanConfirm)
            return;

        DialogResult = true;
    }
}