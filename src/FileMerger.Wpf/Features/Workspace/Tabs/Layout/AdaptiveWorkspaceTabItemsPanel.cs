using System.Windows;
using System.Windows.Controls;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Layout;

public sealed class AdaptiveWorkspaceTabItemsPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        int count = InternalChildren.Count;
        if (count == 0)
            return new Size();

        double totalSpacing = Math.Max(0, count - 1) * WorkspaceTabLayoutConstants.TabSpacing;
        double availableTabsWidth = Math.Max(0.0, availableSize.Width - totalSpacing);

        double tabWidth = Math.Clamp(
            availableTabsWidth / count,
            WorkspaceTabLayoutConstants.MinTabWidth,
            WorkspaceTabLayoutConstants.MaxTabWidth);

        Size childSize = new(tabWidth, availableSize.Height);
        double maxHeight = 0.0;

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(childSize);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        double desiredWidth = count * tabWidth + totalSpacing;

        if (!double.IsInfinity(availableSize.Width))
            desiredWidth = Math.Min(desiredWidth, availableSize.Width);

        return new Size(desiredWidth, maxHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int count = InternalChildren.Count;
        if (count == 0)
            return finalSize;

        double totalSpacing = Math.Max(0, count - 1) * WorkspaceTabLayoutConstants.TabSpacing;
        double availableTabsWidth = Math.Max(0.0, finalSize.Width - totalSpacing);

        double tabWidth = Math.Clamp(
            availableTabsWidth / count,
            WorkspaceTabLayoutConstants.MinTabWidth,
            WorkspaceTabLayoutConstants.MaxTabWidth);

        double x = 0.0;

        foreach (UIElement child in InternalChildren)
        {
            child.Arrange(new Rect(x, 0.0, tabWidth, finalSize.Height));
            x += tabWidth + WorkspaceTabLayoutConstants.TabSpacing;
        }

        return finalSize;
    }
}