using System.ComponentModel;
using System.Windows;
using FileMerger.Wpf.Features.Updates.ViewModels;

namespace FileMerger.Wpf.Features.Updates.Views;

public partial class UpdateCheckWindow
{
    private bool _initialCheckStarted;

    public UpdateCheckWindow()
    {
        InitializeComponent();
    }

    // WPF event handlers require a void-compatible signature.
    // ReSharper disable AsyncVoidEventHandlerMethod
    private async void UpdateCheckWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialCheckStarted)
            return;

        _initialCheckStarted = true;

        if (DataContext is UpdateCheckDialogViewModel viewModel)
            await viewModel.CheckAsync();
    }
    // ReSharper restore AsyncVoidEventHandlerMethod

    private void UpdateCheckWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (DataContext is UpdateCheckDialogViewModel viewModel)
            viewModel.CancelActiveOperation();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}