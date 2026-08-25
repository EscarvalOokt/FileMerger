namespace FileMerger.Wpf.Features.Preview.State
{
    public sealed record PreviewStateSnapshot(
        IReadOnlyCollection<MergeSourceStateSnapshot> Sources,
        PreviewSessionStateSnapshot Session,
        PreviewProfileStateSnapshot Profile,
        IReadOnlyDictionary<string, bool> FileInclusionOverrides);
}