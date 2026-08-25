namespace FileMerger.Domain.Enums
{
    public enum TransformationKind
    {
        RemoveUsingDirectives = 0,
        NormalizeLineEndings = 1,
        TrimTrailingEmptyLines = 2,
        CollapseMultipleEmptyLines = 3
    }
}