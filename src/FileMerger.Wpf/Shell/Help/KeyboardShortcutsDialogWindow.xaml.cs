using System.Windows;

namespace FileMerger.Wpf.Shell.Help;

public partial class KeyboardShortcutsDialogWindow
{
    public KeyboardShortcutsDialogWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}