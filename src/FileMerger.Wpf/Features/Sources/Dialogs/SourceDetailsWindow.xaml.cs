using System.Windows;
using System.Windows.Controls;
using FileMerger.Wpf.Features.Sources.ViewModels;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Features.Sources.Dialogs;

public partial class SourceDetailsWindow : GuardedWindow
{
    public SourceDetailsWindow()
    {
        InitializeComponent();
    }

    private void ExclusionsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not SourceDetailsDialogViewModel viewModel)
            return;

        if (sender is not DataGrid dataGrid)
            return;

        viewModel.ReplaceSelectedExclusions(
            dataGrid.SelectedItems.OfType<MergeSourceExclusionItemViewModel>());
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}