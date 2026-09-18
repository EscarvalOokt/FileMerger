namespace FileMerger.Domain.ValueObjects
{
    public sealed record MergeProfile
    {
        public MergeProfile(
            string name,
            GeneralMergeOptions generalOptions,
            IReadOnlyCollection<FileTypeDefinition>? fileTypes = null,
            IReadOnlyCollection<FileFilterRule>? filterRules = null,
            IReadOnlyCollection<ContentTransformationRule>? transformations = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be empty.", nameof(name));

            Name = name;
            GeneralOptions = generalOptions ?? throw new ArgumentNullException(nameof(generalOptions));
            FileTypes = fileTypes ?? [];
            FilterRules = filterRules ?? [];
            Transformations = transformations ?? [];
        }

        public string Name { get; }
        public GeneralMergeOptions GeneralOptions { get; }
        public IReadOnlyCollection<FileTypeDefinition> FileTypes { get; }
        public IReadOnlyCollection<FileFilterRule> FilterRules { get; }
        public IReadOnlyCollection<ContentTransformationRule> Transformations { get; }
    }
}