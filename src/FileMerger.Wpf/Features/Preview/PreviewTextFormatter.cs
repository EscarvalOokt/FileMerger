namespace FileMerger.Wpf.Features.Preview;

public static class PreviewTextFormatter
{
    public const int MaxPreviewCharacters = 500_000;

    public static PreviewTextFormatResult Format(string fullContent)
    {
        return Format(fullContent, MaxPreviewCharacters);
    }

    public static PreviewTextFormatResult Format(string fullContent, int maxPreviewCharacters)
    {
        ArgumentNullException.ThrowIfNull(fullContent);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPreviewCharacters);

        int totalCharacters = fullContent.Length;

        if (totalCharacters <= maxPreviewCharacters)
        {
            return new PreviewTextFormatResult(
                Text: fullContent,
                TotalCharacters: totalCharacters,
                DisplayedCharacters: totalCharacters,
                WasTruncated: false,
                OmittedCharacters: 0);
        }

        int omittedCharacters = totalCharacters - maxPreviewCharacters;

        string text = string.Concat(
            fullContent[..maxPreviewCharacters],
            Environment.NewLine,
            Environment.NewLine,
            "// -----------------------------------------------",
            Environment.NewLine,
            $"// Preview truncated. {omittedCharacters:N0} character(s) omitted.",
            Environment.NewLine,
            "// Save still writes the full output.",
            Environment.NewLine,
            "// -----------------------------------------------");

        return new PreviewTextFormatResult(
            Text: text,
            TotalCharacters: totalCharacters,
            DisplayedCharacters: maxPreviewCharacters,
            WasTruncated: true,
            OmittedCharacters: omittedCharacters);
    }
}