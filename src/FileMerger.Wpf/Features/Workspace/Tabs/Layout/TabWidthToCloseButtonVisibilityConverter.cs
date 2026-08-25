using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Layout;

public sealed class TabWidthToCloseButtonVisibilityConverter : IMultiValueConverter
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

        bool isSelected = values.Length > 1 && values[1] is bool selected && selected;

        if (isSelected)
            return Visibility.Visible;

        return actualWidth <= WorkspaceTabLayoutConstants.MinimalStateWidth
            ? Visibility.Collapsed
            : Visibility.Visible;
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