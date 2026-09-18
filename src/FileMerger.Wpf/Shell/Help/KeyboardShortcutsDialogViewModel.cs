namespace FileMerger.Wpf.Shell.Help;

public sealed class KeyboardShortcutsDialogViewModel
{
    public KeyboardShortcutsDialogViewModel(IEnumerable<KeyboardShortcutGroupViewModel> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        Groups = [.. groups];
    }

    public IReadOnlyList<KeyboardShortcutGroupViewModel> Groups { get; }

    public static KeyboardShortcutsDialogViewModel CreateDefault()
    {
        return new KeyboardShortcutsDialogViewModel(
        [
            new KeyboardShortcutGroupViewModel(
                "File",
                [
                    new KeyboardShortcutItemViewModel("New Workspace Tab", "Ctrl+T", "Create a new workspace tab."),
                    new KeyboardShortcutItemViewModel("Open Workspace...", "Ctrl+O", "Open a saved workspace file."),
                    new KeyboardShortcutItemViewModel("Save Workspace", "Ctrl+S", "Save the current workspace."),
                    new KeyboardShortcutItemViewModel(
                        "Save Workspace As...",
                        "Ctrl+Shift+S",
                        "Save the current workspace to a new file.")
                ]),

            new KeyboardShortcutGroupViewModel(
                "Workspace",
                [
                    new KeyboardShortcutItemViewModel(
                        "Duplicate Tab",
                        "Ctrl+Shift+D",
                        "Duplicate the active workspace tab."),
                    new KeyboardShortcutItemViewModel("Rename Tab...", "F2", "Rename the active workspace tab."),
                    new KeyboardShortcutItemViewModel("Close Tab", "Ctrl+W", "Close the active workspace tab."),
                    new KeyboardShortcutItemViewModel(
                        "Close Other Tabs",
                        "Ctrl+Shift+W",
                        "Close all workspace tabs except the active one."),
                    new KeyboardShortcutItemViewModel("Next Tab", "Ctrl+Tab", "Select the next workspace tab."),
                    new KeyboardShortcutItemViewModel(
                        "Previous Tab",
                        "Ctrl+Shift+Tab",
                        "Select the previous workspace tab.")
                ]),

            new KeyboardShortcutGroupViewModel("Profile", []),

            new KeyboardShortcutGroupViewModel("Preview", []),

            new KeyboardShortcutGroupViewModel(
                "Help",
                [
                    new KeyboardShortcutItemViewModel("Keyboard Shortcuts", "F1", "Open this shortcuts reference.")
                ])
        ]);
    }
}