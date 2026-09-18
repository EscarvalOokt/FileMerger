namespace FileMerger.Application.Abstractions.Services;

public interface IReleaseManifestSource
{
    Task<string> GetManifestJsonAsync(CancellationToken cancellationToken = default);
}