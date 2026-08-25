using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Layout;

public sealed class TabWidthToTitleVisibilityConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        double actualWidth = value is double width
            ? width
            : double.PositiveInfinity;

        return actualWidth <= WorkspaceTabLayoutConstants.MinimalStateWidth
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}