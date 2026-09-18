using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FileMerger.Wpf.Features.Profile.ViewModels;

namespace FileMerger.Wpf.Features.Profile.Views;

public partial class ProfileFilterRulesDialogWindow
{
    public ProfileFilterRulesDialogWindow()
    {
        InitializeComponent();
    }

    private void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void FilterRulesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit ||
            e.Row.Item is not ProfileFilterRuleItemViewModel rule ||
            sender is not DataGrid dataGrid)
        {
            return;
        }

        _ = Dispatcher.InvokeAsync(
            () =>
            {
                if (!rule.HasValidationError || !dataGrid.Items.Contains(rule))
                    return;

                DataGridColumn? validationColumn = FindValidationColumn(dataGrid);
                if (validationColumn is not null)
                    dataGrid.ScrollIntoView(rule, validationColumn);
            },
            DispatcherPriority.Background);
    }

    private static DataGridColumn? FindValidationColumn(DataGrid dataGrid)
    {
        foreach (DataGridColumn column in dataGrid.Columns)
        {
            if (Equals(column.Header, "Validation"))
                return column;
        }

        return null;
    }
}