using FileMerger.Application.Updates;

namespace FileMerger.Application.Abstractions.Services;

public interface IUpdatePackageValidator
{
    Task<VerifiedUpdatePackage> ValidateAsync(
        DownloadedUpdatePackage downloadedPackage,
        IProgress<UpdatePackagePreparationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}