namespace FileMerger.Wpf.Shell.Help;

public sealed class KeyboardShortcutItemViewModel
{
    public KeyboardShortcutItemViewModel(string action, string gesture, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));

        if (string.IsNullOrWhiteSpace(gesture))
            throw new ArgumentException("Gesture cannot be empty.", nameof(gesture));

        Action = action;
        Gesture = gesture;
        Description = description?.Trim() ?? string.Empty;
    }

    public string Action { get; }

    public string Gesture { get; }

    public string Description { get; }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
}