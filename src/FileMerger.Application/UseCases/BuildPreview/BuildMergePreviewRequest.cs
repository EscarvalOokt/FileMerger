using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;

namespace FileMerger.Application.UseCases.BuildPreview;

public sealed record BuildMergePreviewRequest
{
    public BuildMergePreviewRequest(
        MergeSession session,
        IReadOnlyCollection<FileInclusionOverride>? inclusionOverrides = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
        InclusionOverrides = inclusionOverrides ?? [];
    }

    public MergeSession Session { get; }
    public IReadOnlyCollection<FileInclusionOverride> InclusionOverrides { get; }
}