using System.Windows.Controls;
using System.Windows.Input;
using FileMerger.Wpf.Features.Sources.ViewModels;

namespace FileMerger.Wpf.Features.Sources.Views;

public partial class SourcesPaneView : UserControl
{
    public SourcesPaneView()
    {
        InitializeComponent();
    }

    private SourcesPaneViewModel? ViewModel => DataContext as SourcesPaneViewModel;

    private void SourcesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null)
            return;

        ViewModel.ReplaceSelectedSources(
            SourcesDataGrid.SelectedItems.OfType<MergeSourceItemViewModel>());
    }

    private void SourcesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel?.OpenSourceDetailsCommand.CanExecute(null) == true)
            ViewModel.OpenSourceDetailsCommand.Execute(null);
    }
}