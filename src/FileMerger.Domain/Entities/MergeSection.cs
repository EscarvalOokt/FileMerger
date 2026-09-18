namespace FileMerger.Domain.Entities
{
    public sealed record MergeSection
    {
        public MergeSection(InputFile sourceFile, string content, int order, string? headerText = null)
        {
            ArgumentNullException.ThrowIfNull(sourceFile);

            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order), "Order must be non-negative.");

            content ??= string.Empty;

            SourceFile = sourceFile;
            Content = content;
            Order = order;
            HeaderText = headerText;
        }

        public InputFile SourceFile { get; }
        public string Content { get; }
        public int Order { get; }
        public string? HeaderText { get; }
    }
}