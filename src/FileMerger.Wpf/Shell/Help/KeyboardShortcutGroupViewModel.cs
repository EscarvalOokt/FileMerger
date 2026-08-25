namespace FileMerger.Wpf.Shell.Help;

public sealed class KeyboardShortcutGroupViewModel
{
    public KeyboardShortcutGroupViewModel(
        string title,
        IEnumerable<KeyboardShortcutItemViewModel> shortcuts,
        string emptyText = "No keyboard shortcuts assigned yet.")
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        ArgumentNullException.ThrowIfNull(shortcuts);

        Title = title;
        Shortcuts = [.. shortcuts];
        EmptyText = string.IsNullOrWhiteSpace(emptyText)
            ? "No keyboard shortcuts assigned yet."
            : emptyText.Trim();
    }

    public string Title { get; }

    public IReadOnlyList<KeyboardShortcutItemViewModel> Shortcuts { get; }

    public string EmptyText { get; }

    public bool HasShortcuts => Shortcuts.Count > 0;
}