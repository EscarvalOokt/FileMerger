using System.Reflection;
using System.Reflection.Emit;
using FileMerger.Infrastructure.Updates;

namespace FileMerger.Tests.Infrastructure.Updates;

public sealed class EntryAssemblyUpdateManifestUriProviderTests
{
    [Fact]
    public void GetManifestUri_Should_Read_Https_Uri_From_Assembly_Metadata()
    {
        Assembly assembly = CreateAssemblyWithMetadata("https://updates.example.test/manifest.json");
        EntryAssemblyUpdateManifestUriProvider provider = new(assembly);

        Uri uri = provider.GetManifestUri();

        Assert.Equal(new Uri("https://updates.example.test/manifest.json"), uri);
    }

    [Fact]
    public void GetManifestUri_Should_Fail_When_Metadata_Is_Missing()
    {
        AssemblyName name = new($"NoUpdateMetadata_{Guid.NewGuid():N}");
        Assembly assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        EntryAssemblyUpdateManifestUriProvider provider = new(assembly);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(provider.GetManifestUri);

        Assert.Contains("not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://updates.example.test/manifest.json")]
    [InlineData("not-a-uri")]
    public void GetManifestUri_Should_Reject_NonHttps_Or_Malformed_Uri(string value)
    {
        Assembly assembly = CreateAssemblyWithMetadata(value);
        EntryAssemblyUpdateManifestUriProvider provider = new(assembly);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(provider.GetManifestUri);

        Assert.Contains("HTTPS", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Assembly CreateAssemblyWithMetadata(string value)
    {
        AssemblyName name = new($"UpdateMetadata_{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        ConstructorInfo constructor =
            typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!;
        CustomAttributeBuilder attribute = new(
            constructor,
            [EntryAssemblyUpdateManifestUriProvider.MetadataKey, value]);
        assembly.SetCustomAttribute(attribute);

        return assembly;
    }
}