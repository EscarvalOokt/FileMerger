using FileMerger.Application.Updates;

namespace FileMerger.Application.Abstractions.Services;

public interface IUpdatePackageDownloader
{
    Task<DownloadedUpdatePackage> DownloadAsync(
        UpdatePackage package,
        IProgress<UpdatePackagePreparationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}