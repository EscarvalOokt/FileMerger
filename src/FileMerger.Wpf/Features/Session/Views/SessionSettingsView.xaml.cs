using System.Windows;
using System.Windows.Input;

namespace FileMerger.Wpf.Features.Session.Views;

public partial class SessionSettingsView
{
    public SessionSettingsView()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty BrowseOutputCommandProperty =
        DependencyProperty.Register(
            nameof(BrowseOutputCommand),
            typeof(ICommand),
            typeof(SessionSettingsView),
            new PropertyMetadata(null));

    public ICommand? BrowseOutputCommand
    {
        get => (ICommand?)GetValue(BrowseOutputCommandProperty);
        set => SetValue(BrowseOutputCommandProperty, value);
    }
}