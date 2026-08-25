using FileMerger.Domain.Entities;

namespace FileMerger.Application.Abstractions.Services;

public sealed record FileDiscoveryResult
{
    public FileDiscoveryResult(
        IReadOnlyCollection<InputFile> inventoryFiles,
        IReadOnlyCollection<InputFile>? sourceExcludedFiles = null)
    {
        ArgumentNullException.ThrowIfNull(inventoryFiles);

        InventoryFiles = inventoryFiles;
        MergeCandidates = [.. inventoryFiles.Where(x => x.IsMergeCandidate)];
        SourceExcludedFiles = sourceExcludedFiles ?? [];
    }

    public IReadOnlyCollection<InputFile> InventoryFiles { get; }
    public IReadOnlyCollection<InputFile> MergeCandidates { get; }
    public IReadOnlyCollection<InputFile> SourceExcludedFiles { get; }
}