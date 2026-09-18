using FileMerger.UpdateProtocol;

namespace FileMerger.Updater;

public sealed record UpdaterArguments(string RequestPath)
{
    public static UpdaterArguments Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count != 2 ||
            !string.Equals(arguments[0], UpdateProtocolConstants.RequestArgument, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected '{UpdateProtocolConstants.RequestArgument} <absolute-request-path>'.",
                nameof(arguments));
        }

        string requestPath = arguments[1];
        if (string.IsNullOrWhiteSpace(requestPath) || !Path.IsPathFullyQualified(requestPath))
            throw new ArgumentException("The update request path must be absolute.", nameof(arguments));

        return new UpdaterArguments(Path.GetFullPath(requestPath));
    }
}