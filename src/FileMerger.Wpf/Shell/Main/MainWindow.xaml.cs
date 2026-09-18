using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FileMerger.Wpf.Features.Files.ViewModels;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.Status;

namespace FileMerger.Wpf.Shell.Main;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Handled)
            return;

        if (e.Key != Key.Tab)
            return;

        ModifierKeys modifiers = Keyboard.Modifiers;
        bool hasControl = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        bool hasShift = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        bool hasUnsupportedModifiers = (modifiers & ~(ModifierKeys.Control | ModifierKeys.Shift)) != ModifierKeys.None;

        if (!hasControl || hasUnsupportedModifiers)
            return;

        RelayCommand? command = hasShift
            ? ViewModel?.SelectPreviousWorkspaceTabCommand
            : ViewModel?.SelectNextWorkspaceTabCommand;

        if (command is null || !command.CanExecute(null))
            return;

        command.Execute(null);
        e.Handled = true;
    }

    private void FilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null || sender is not DataGrid dataGrid)
            return;

        ViewModel.FilesPane.ReplaceSelectedFiles(dataGrid.SelectedItems.OfType<InputFileItemViewModel>());
    }

    private void FilesDataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is null || sender is not DataGridRow row)
            return;

        if (row.DataContext is not InputFileItemViewModel file)
            return;

        ViewModel.FilesPane.ContextFile = file;

        if (!row.IsSelected)
        {
            DataGrid? dataGrid = FindVisualParent<DataGrid>(row);

            if (dataGrid is not null)
            {
                dataGrid.SelectedItems.Clear();
                row.IsSelected = true;
                dataGrid.CurrentItem = file;
            }
        }

        row.Focus();
    }

    private void WorkspaceTabsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null || sender is not ListBox listBox)
            return;

        if (listBox.SelectedItem is not WorkspaceTabViewModel tab)
            return;

        ViewModel.SelectWorkspaceTab(tab);
    }

    private void WorkspaceTabItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is null || sender is not FrameworkElement element)
            return;

        if (element.DataContext is not WorkspaceTabViewModel tab)
            return;

        ViewModel.SelectWorkspaceTab(tab);
    }

    private void FileMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.RefreshRecentWorkspacesCommand.CanExecute(null) != true)
            return;

        ViewModel.RefreshRecentWorkspacesCommand.Execute(null);
    }

    private void FileFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        OpenButtonContextMenu(sender);
    }

    private static void OpenButtonContextMenu(object sender)
    {
        if (sender is not Button button || button.ContextMenu is null)
            return;

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
    }

    private void CloseWorkspaceTabButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (ViewModel is not { } viewModel || sender is not FrameworkElement element)
            return;

        if (element.DataContext is not WorkspaceTabViewModel tab)
            return;

        _ = CloseWorkspaceTabSafelyAsync(viewModel, tab);
    }

    private static async Task CloseWorkspaceTabSafelyAsync(MainViewModel viewModel, WorkspaceTabViewModel tab)
    {
        try
        {
            await viewModel.CloseWorkspaceTabAsync(tab);
        }
        catch (Exception ex)
        {
            tab.Document.OperationStatus.SetStatus(
                $"Failed to close workspace tab: {ex.Message}",
                StatusSeverity.Error);
        }
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject? current = child;

        while (current is not null)
        {
            if (current is T typed)
                return typed;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}