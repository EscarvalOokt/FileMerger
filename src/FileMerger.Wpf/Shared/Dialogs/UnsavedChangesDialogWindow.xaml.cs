using System.Windows;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Shared.Dialogs;

public partial class UnsavedChangesDialogWindow : ShellDialogWindow
{
    public UnsavedChangesDialogWindow()
    {
        InitializeComponent();
    }

    private UnsavedChangesDialogViewModel? ViewModel => DataContext as UnsavedChangesDialogViewModel;

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ChooseAndClose(UnsavedChangesDecision.Save);
    }

    private void DiscardButton_Click(object sender, RoutedEventArgs e)
    {
        ChooseAndClose(UnsavedChangesDecision.Discard);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ChooseAndClose(UnsavedChangesDecision.Cancel);
    }

    private void ChooseAndClose(UnsavedChangesDecision decision)
    {
        ViewModel?.Choose(decision);
        DialogResult = true;
    }
}