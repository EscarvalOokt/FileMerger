using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.Abstractions.Services;

public interface IFileDiscoveryService
{
    FileDiscoveryResult DiscoverFiles(
        IReadOnlyCollection<MergeSource> sources,
        MergeProfile profile,
        IProgress<FileDiscoveryProgress>? progress = null);
}