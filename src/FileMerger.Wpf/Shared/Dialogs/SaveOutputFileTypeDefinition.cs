namespace FileMerger.Wpf.Shared.Dialogs;

public sealed record SaveOutputFileTypeDefinition
{
    public SaveOutputFileTypeDefinition(string extension, string displayName, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be empty.", nameof(extension));

        if (!extension.StartsWith('.'))
            throw new ArgumentException("Extension must start with '.'.", nameof(extension));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));

        Extension = extension;
        DisplayName = displayName;
        IsDefault = isDefault;
    }

    public string Extension { get; }
    public string DisplayName { get; }
    public bool IsDefault { get; }

    public string Pattern => $"*{Extension}";
}