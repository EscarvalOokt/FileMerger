using System.Reflection;
using FileMerger.Application.Abstractions.Services;

namespace FileMerger.Infrastructure.Updates;

public sealed class EntryAssemblyApplicationVersionProvider(Assembly? assembly) : IApplicationVersionProvider
{
    public EntryAssemblyApplicationVersionProvider() : this(Assembly.GetEntryAssembly())
    {
    }

    public string GetCurrentVersion()
    {
        if (assembly is null)
            throw new InvalidOperationException("The entry assembly could not be resolved.");

        AssemblyInformationalVersionAttribute? attribute =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (attribute is null || string.IsNullOrWhiteSpace(attribute.InformationalVersion))
        {
            throw new InvalidOperationException("The entry assembly does not define an informational version.");
        }

        return attribute.InformationalVersion.Trim();
    }
}