using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects
{
    public sealed record ContentTransformationRule
    {
        public ContentTransformationRule(
            TransformationKind kind,
            int order,
            bool isEnabled = true,
            IReadOnlyCollection<FileKind>? appliesTo = null)
        {
            if (order < 0)
                throw new ArgumentOutOfRangeException(nameof(order), "Order must be non-negative.");

            Kind = kind;
            Order = order;
            IsEnabled = isEnabled;
            AppliesTo = appliesTo ?? [];
        }

        public TransformationKind Kind { get; }
        public int Order { get; }
        public bool IsEnabled { get; }
        public IReadOnlyCollection<FileKind> AppliesTo { get; }
    }
}