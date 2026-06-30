using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorDescriptorTests
{
    [Fact]
    public void ConnectorDescriptor_SupportedPlatforms_Defaults_To_Empty()
    {
        var desc = new ConnectorDescriptor
        {
            Id = new ConnectorId("filesystem"),
            Metadata = ConnectorMetadata.Create("filesystem", "Filesystem", "desc", ConnectorType.Filesystem, "1.0"),
            Capabilities = [ConnectorCapabilities.AssetDiscovery],
        };
        Assert.Empty(desc.SupportedPlatforms);
    }

    [Fact]
    public void ConnectorDescriptor_Has_No_Public_Setters()
    {
        var props = typeof(ConnectorDescriptor).GetProperties();
        Assert.All(
            props,
            p =>
            {
                var setMethod = p.SetMethod;

                // init-only properties are acceptable; reject true public setters
                var hasPublicSetter = setMethod?.IsPublic ?? false;
                var isInitOnly = setMethod?.ReturnParameter?.GetRequiredCustomModifiers()
                    .Any(m => m.Name == "IsExternalInit") ?? false;
                Assert.False(
                    hasPublicSetter && !isInitOnly,
                    $"Property {p.Name} must not have a public setter");
            });
    }
}
