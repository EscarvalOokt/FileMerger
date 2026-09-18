using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Layout;

public sealed class TabWidthToPrimaryIndicatorVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double actualWidth = values.Length > 0 && values[0] is double width ? width : double.PositiveInfinity;

        bool isSelected = values.Length > 1 && values[1] is bool selected && selected;
        bool hasActivity = values.Length > 2 && values[2] is bool activity && activity;
        bool hasDirtyIndicator = values.Length > 3 && values[3] is bool dirty && dirty;

        bool shouldShowPrimaryIndicator = actualWidth <= WorkspaceTabLayoutConstants.MinimalStateWidth &&
                                          !isSelected &&
                                          (hasActivity || hasDirtyIndicator);

        return shouldShowPrimaryIndicator ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}