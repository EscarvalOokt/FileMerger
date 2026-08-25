using FileMerger.Wpf.Shared.Integration;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeClipboardService : IClipboardService
{
    public string? LastText { get; private set; }

    public int SetTextCallCount { get; private set; }

    public void SetText(string text)
    {
        LastText = text;
        SetTextCallCount++;
    }
}