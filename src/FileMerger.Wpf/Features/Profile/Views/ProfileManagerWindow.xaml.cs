using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Features.Profile.Views;

public partial class ProfileManagerWindow : GuardedWindow
{
    private bool _suppressSelectionSync;
    private INotifyPropertyChanged? _viewModelPropertyChangedSource;

    public ProfileManagerWindow()
    {
        InitializeComponent();

        DataContextChanged += Window_DataContextChanged;
    }

    private ProfileManagerViewModel? ViewModel => DataContext as ProfileManagerViewModel;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SyncSelectionFromViewModel();
    }

    private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModelPropertyChangedSource?.PropertyChanged -= ViewModel_PropertyChanged;

        _viewModelPropertyChangedSource = e.NewValue as INotifyPropertyChanged;

        _viewModelPropertyChangedSource?.PropertyChanged += ViewModel_PropertyChanged;

        SyncSelectionFromViewModel();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProfileManagerViewModel.SelectedProfile))
            SyncSelectionFromViewModel();
    }

    private async void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionSync)
            return;

        if (ViewModel is null)
            return;

        if (sender is not ListBox listBox)
            return;

        var candidate = listBox.SelectedItem as ProfileLibraryListItemViewModel;

        await ViewModel.RequestSelectProfileAsync(candidate);
        SyncSelectionFromViewModel();
    }

    private void SyncSelectionFromViewModel()
    {
        if (ViewModel is null)
            return;

        ListBox? profilesList = FindProfilesListBox();
        if (profilesList is null)
            return;

        _suppressSelectionSync = true;
        try
        {
            if (!ReferenceEquals(profilesList.SelectedItem, ViewModel.SelectedProfile))
                profilesList.SelectedItem = ViewModel.SelectedProfile;
        }
        finally
        {
            _suppressSelectionSync = false;
        }
    }

    private ListBox? FindProfilesListBox()
    {
        if (ViewModel is null)
            return null;

        return FindVisualDescendants<ListBox>(this)
            .FirstOrDefault(listBox => ReferenceEquals(listBox.ItemsSource, ViewModel.ProfilesView));
    }

    private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is null)
            yield break;

        int childrenCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childrenCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);

            if (child is T typedChild)
                yield return typedChild;

            foreach (T descendant in FindVisualDescendants<T>(child))
                yield return descendant;
        }
    }

    private void MoreActionsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenButtonContextMenu(sender);
    }

    private void NewProfileMenuButton_Click(object sender, RoutedEventArgs e)
    {
        OpenButtonContextMenu(sender);
    }

    private static void OpenButtonContextMenu(object sender)
    {
        if (sender is not Button button || button.ContextMenu is null)
            return;

        button.ContextMenu.DataContext = button.DataContext;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
    }
}