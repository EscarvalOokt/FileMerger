using FileMerger.Application.Abstractions.Services;
using FileMerger.Infrastructure.Services.Content;
using FileMerger.Infrastructure.Services.Discovery;
using FileMerger.Infrastructure.Services.Filtering;
using FileMerger.Infrastructure.Services.Merge;
using FileMerger.Infrastructure.Services.Validation;
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

        return services;
    }
}