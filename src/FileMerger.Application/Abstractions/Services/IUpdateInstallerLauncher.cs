using FileMerger.Application.Updates;

namespace FileMerger.Application.Abstractions.Services;

public interface IUpdateInstallerLauncher
{
    Task StartAsync(VerifiedUpdatePackage verifiedPackage, CancellationToken cancellationToken = default);
}