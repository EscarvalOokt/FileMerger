using System.Windows;

namespace FileMerger.Wpf.Shared.Integration;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Clipboard.SetText(text);
    }
}