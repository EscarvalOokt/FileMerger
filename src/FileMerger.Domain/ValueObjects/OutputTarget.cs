namespace FileMerger.Domain.ValueObjects
{
    public sealed record OutputTarget
    {
        public OutputTarget(string path, string encodingName = "utf-8")
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Output path cannot be empty.", nameof(path));

            if (string.IsNullOrWhiteSpace(encodingName))
                throw new ArgumentException("Encoding name cannot be empty.", nameof(encodingName));

            Path = path;
            EncodingName = encodingName;
        }

        public string Path { get; }
        public string EncodingName { get; }
    }
}