using System.Reflection;

namespace FileMerger.Infrastructure.Updates;

public sealed class EntryAssemblyUpdateManifestUriProvider(Assembly? assembly)
{
    public const string MetadataKey = "FileMerger.UpdateManifestUri";

    public EntryAssemblyUpdateManifestUriProvider() : this(Assembly.GetEntryAssembly())
    {
    }

    public Uri GetManifestUri()
    {
        if (assembly is null)
            throw new InvalidOperationException("The entry assembly could not be resolved.");

        AssemblyMetadataAttribute[] matches =
        [
            .. assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(attribute => string.Equals(attribute.Key, MetadataKey, StringComparison.Ordinal))
        ];

        if (matches.Length == 0)
            throw new InvalidOperationException("The update manifest URI is not configured for this build.");

        if (matches.Length > 1)
            throw new InvalidOperationException("The update manifest URI is configured more than once.");

        string configuredValue = matches[0].Value?.Trim() ?? string.Empty;
        if (configuredValue.Length == 0)
            throw new InvalidOperationException("The update manifest URI is not configured for this build.");

        if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The configured update manifest URI must be an absolute HTTPS URI.");
        }

        return uri;
    }
}