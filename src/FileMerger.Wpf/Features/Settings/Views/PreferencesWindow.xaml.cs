using System.Windows;
using FileMerger.Wpf.Features.Settings.ViewModels;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Features.Settings.Views;

public partial class PreferencesWindow : GuardedWindow
{
    private PreferencesDialogViewModel? _viewModel;

    public PreferencesWindow()
    {
        InitializeComponent();

        DataContextChanged += PreferencesWindow_DataContextChanged;
        Closed += PreferencesWindow_Closed;
    }

    private void PreferencesWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModel?.RequestClose -= ViewModel_RequestClose;

        _viewModel = e.NewValue as PreferencesDialogViewModel;

        _viewModel?.RequestClose += ViewModel_RequestClose;
    }

    private void PreferencesWindow_Closed(object? sender, EventArgs e)
    {
        _viewModel?.RequestClose -= ViewModel_RequestClose;

        _viewModel = null;
    }

    private void ViewModel_RequestClose(object? sender, bool? result)
    {
        DialogResult = result;

        if (IsVisible)
            Close();
    }
}