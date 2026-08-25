using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Layout;

public sealed class TabWidthToDirtyIndicatorVisibilityConverter : IMultiValueConverter
{
    public object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        double actualWidth = values.Length > 0 && values[0] is double width
            ? width
            : double.PositiveInfinity;

        bool hasDirtyIndicator = values.Length > 1 && values[1] is bool dirty && dirty;

        return hasDirtyIndicator && actualWidth > WorkspaceTabLayoutConstants.MinimalStateWidth
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}