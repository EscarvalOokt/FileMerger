using System.Windows;
using System.Windows.Input;

namespace FileMerger.Wpf.Features.Session.Views;

public partial class SessionSettingsView
{
    public static readonly DependencyProperty BrowseOutputCommandProperty = DependencyProperty.Register(
        nameof(BrowseOutputCommand),
        typeof(ICommand),
        typeof(SessionSettingsView),
        new PropertyMetadata(null));

    public SessionSettingsView()
    {
        InitializeComponent();
    }

    public ICommand? BrowseOutputCommand
    {
        get => (ICommand?)GetValue(BrowseOutputCommandProperty);
        set => SetValue(BrowseOutputCommandProperty, value);
    }
}