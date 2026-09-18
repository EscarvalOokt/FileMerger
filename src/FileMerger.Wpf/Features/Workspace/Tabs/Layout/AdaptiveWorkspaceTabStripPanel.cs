using System.Windows;
using System.Windows.Controls;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Layout;

public sealed class AdaptiveWorkspaceTabStripPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (InternalChildren.Count == 0)
            return new Size();

        UIElement tabsElement = InternalChildren[0];
        UIElement? newTabButton = InternalChildren.Count > 1 ? InternalChildren[1] : null;

        newTabButton?.Measure(new Size(double.PositiveInfinity, availableSize.Height));

        double buttonWidth = newTabButton?.DesiredSize.Width ?? 0.0;
        double tabsAvailableWidth = Math.Max(0.0, availableSize.Width - buttonWidth);

        tabsElement.Measure(new Size(tabsAvailableWidth, availableSize.Height));

        double desiredWidth = Math.Min(availableSize.Width, tabsElement.DesiredSize.Width + buttonWidth);

        double desiredHeight = Math.Max(tabsElement.DesiredSize.Height, newTabButton?.DesiredSize.Height ?? 0.0);

        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InternalChildren.Count == 0)
            return finalSize;

        UIElement tabsElement = InternalChildren[0];
        UIElement? newTabButton = InternalChildren.Count > 1 ? InternalChildren[1] : null;

        double buttonWidth = newTabButton?.DesiredSize.Width ?? 0.0;
        double tabsWidth = Math.Max(0.0, finalSize.Width - buttonWidth);

        tabsElement.Arrange(new Rect(0.0, 0.0, tabsWidth, finalSize.Height));

        newTabButton?.Arrange(new Rect(tabsWidth, 0.0, buttonWidth, finalSize.Height));

        return finalSize;
    }
}