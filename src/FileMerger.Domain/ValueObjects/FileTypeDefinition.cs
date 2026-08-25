using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects
{
    public sealed record FileTypeDefinition
    {
        public FileTypeDefinition(
            string extension,
            string displayName,
            FileKind kind,
            bool isEnabled = true,
            bool supportsLanguageSpecificProcessing = false)
        {
            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("Extension cannot be empty.", nameof(extension));

            if (!extension.StartsWith('.'))
                throw new ArgumentException("Extension must start with '.'", nameof(extension));

            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));

            Extension = extension;
            DisplayName = displayName;
            Kind = kind;
            IsEnabled = isEnabled;
            SupportsLanguageSpecificProcessing = supportsLanguageSpecificProcessing;
        }

        public string Extension { get; }
        public string DisplayName { get; }
        public FileKind Kind { get; }
        public bool IsEnabled { get; init; }
        public bool SupportsLanguageSpecificProcessing { get; }
    }
}