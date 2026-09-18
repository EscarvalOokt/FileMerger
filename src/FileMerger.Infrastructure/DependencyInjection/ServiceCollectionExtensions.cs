using FileMerger.Application.Abstractions.Services;
using FileMerger.Infrastructure.Services.Content;
using FileMerger.Infrastructure.Services.Discovery;
using FileMerger.Infrastructure.Services.Filtering;
using FileMerger.Infrastructure.Services.Merge;
using FileMerger.Infrastructure.Services.Validation;
using FileMerger.Infrastructure.Updates;
using FileMerger.UpdateProtocol;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileMergerInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMergeSessionValidator, MergeSessionValidator>();
        services.AddSingleton<IUnsupportedTextFileDetector, UnsupportedTextFileDetector>();
        services.AddSingleton<IFileDiscoveryService, FileDiscoveryService>();
        services.AddSingleton<IFileFilterService, FileFilterService>();
        services.AddSingleton<IFileInclusionOverrideService, FileInclusionOverrideService>();
        services.AddSingleton<IContentReader, ContentReader>();
        services.AddSingleton<IContentTransformationService, ContentTransformationService>();
        services.AddSingleton<IMergeBuilder, MergeBuilder>();
        services.AddSingleton<IMergeWriter, MergeWriter>();

        services.AddSingleton<IApplicationVersionProvider, EntryAssemblyApplicationVersionProvider>();
        services.AddSingleton<IUpdaterVersionProvider, UpdateProtocolVersionProvider>();
        services.AddSingleton<EntryAssemblyUpdateManifestUriProvider>();
        services.AddSingleton<UpdateHttpOriginPolicy>();
        services.AddSingleton<UpdateStagingPathPolicy>();
        services.AddSingleton<ApplicationInstallationPathProvider>();
        services.AddSingleton<UpdateProtocolFileStore>();
        services.AddSingleton(_ => new HttpClient(
            new HttpClientHandler
            {
                AllowAutoRedirect = false
            }));
        services.AddSingleton<IReleaseManifestSource, HttpReleaseManifestSource>();
        services.AddSingleton<IUpdatePackageDownloader, HttpUpdatePackageDownloader>();
        services.AddSingleton<IUpdatePackageValidator, ZipUpdatePackageValidator>();
        services.AddSingleton<IUpdateInstallationPreflightService, UpdateInstallationPreflightService>();
        services.AddSingleton<IUpdateInstallerLauncher, UpdateInstallerLauncher>();
        services.AddSingleton<IUpdateRestartVerificationService, UpdateRestartVerificationService>();

        return services;
    }
}