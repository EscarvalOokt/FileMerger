using System.Reflection;
using System.Reflection.Emit;
using FileMerger.Infrastructure.Updates;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class EntryAssemblyApplicationVersionProviderTests
{
    [Fact]
    public void GetCurrentVersion_Should_Read_InformationalVersion_From_Assembly()
    {
        Assembly assembly = CreateAssemblyWithInformationalVersion("0.2.0-alpha.7+test");
        EntryAssemblyApplicationVersionProvider provider = new(assembly);

        string version = provider.GetCurrentVersion();

        Assert.Equal("0.2.0-alpha.7+test", version);
    }

    [Fact]
    public void GetCurrentVersion_Should_Fail_When_InformationalVersion_Is_Missing()
    {
        AssemblyName name = new($"NoInformationalVersion_{Guid.NewGuid():N}");
        Assembly assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        EntryAssemblyApplicationVersionProvider provider = new(assembly);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(provider.GetCurrentVersion);

        Assert.Contains("informational version", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Assembly CreateAssemblyWithInformationalVersion(string version)
    {
        AssemblyName name = new($"InformationalVersion_{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        ConstructorInfo constructor = typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!;
        CustomAttributeBuilder attribute = new(constructor, [version]);
        assembly.SetCustomAttribute(attribute);

        return assembly;
    }
}