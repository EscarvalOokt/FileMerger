using FileMerger.Application.Abstractions.Services;
using FileMerger.UpdateProtocol;

namespace FileMerger.Infrastructure.Updates;

public sealed class UpdateProtocolVersionProvider : IUpdaterVersionProvider
{
    public string GetCurrentUpdaterVersion()
    {
        return UpdateProtocolConstants.UpdaterProtocolVersion;
    }
}