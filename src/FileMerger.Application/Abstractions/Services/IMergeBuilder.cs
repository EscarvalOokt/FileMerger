using FileMerger.Domain.Entities;

namespace FileMerger.Application.Abstractions.Services;

public interface IMergeBuilder
{
    MergeOutput Build(
        MergeSession session,
        IReadOnlyCollection<MergeSection> sections,
        TimeSpan totalDuration,
        IReadOnlyCollection<InputFile>? sourceExcludedFiles = null);
}