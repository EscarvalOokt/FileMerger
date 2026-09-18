using FileMerger.Application.Updates;

namespace FileMerger.Application.Abstractions.Services;

public interface IUpdateInstallationPreflightService
{
    Task ValidateAsync(VerifiedUpdatePackage verifiedPackage, CancellationToken cancellationToken = default);
}