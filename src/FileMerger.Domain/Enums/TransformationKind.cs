namespace FileMerger.Domain.Enums
{
    public enum TransformationKind
    {
        NormalizeLineEndings = 1,
        TrimTrailingEmptyLines = 2,
        CollapseMultipleEmptyLines = 3
    }
}