using System.Windows;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Shared.Dialogs;

public partial class ConfirmDialogWindow : ShellDialogWindow
{
    public ConfirmDialogWindow()
    {
        InitializeComponent();
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}