namespace FileMerger.Wpf.Features.Preview;

public sealed record PreviewTextFormatResult(
    string Text,
    int TotalCharacters,
    int DisplayedCharacters,
    bool WasTruncated,
    int OmittedCharacters);