using Ferret.ConnectorPlatform.Tests.Fakes;
using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

public sealed class ConnectorRegistryTests
{
    [Fact]
    public void GetAll_Returns_All_Registered_Descriptors()
    {
        var registry = RegistryBuilder.Build([
            new FakeConnectorFactory("filesystem", ConnectorCapabilities.AssetDiscovery),
            new FakeConnectorFactory("git", ConnectorCapabilities.AssetDiscovery, ConnectorCapabilities.ChangeDetection),
        ]);

        Assert.Equal(2, registry.GetAll().Count);
    }

    [Fact]
    public void GetById_Returns_Descriptor_For_Known_Id()
    {
        var registry = RegistryBuilder.Build([
            new FakeConnectorFactory("filesystem", ConnectorCapabilities.AssetDiscovery),
        ]);

        var desc = registry.GetById(new ConnectorId("filesystem"));
        Assert.NotNull(desc);
        Assert.Equal("filesystem", desc.Id.Value);
    }

    [Fact]
    public void GetById_Returns_Null_For_Unknown_Id()
    {
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        Assert.Null(registry.GetById(new ConnectorId("unknown")));
    }

    [Fact]
    public void IsRegistered_Returns_True_For_Known_Id()
    {
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        Assert.True(registry.IsRegistered(new ConnectorId("filesystem")));
    }

    [Fact]
    public void IsRegistered_Returns_False_For_Unknown_Id()
    {
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        Assert.False(registry.IsRegistered(new ConnectorId("git")));
    }

    [Fact]
    public void GetByCapability_Returns_Matching_Descriptors()
    {
        var registry = RegistryBuilder.Build([
            new FakeConnectorFactory("filesystem", ConnectorCapabilities.AssetDiscovery),
            new FakeConnectorFactory("git", ConnectorCapabilities.AssetDiscovery, ConnectorCapabilities.ChangeDetection),
            new FakeConnectorFactory("slack"),
        ]);

        var results = registry.GetByCapability(ConnectorCapabilities.ChangeDetection);
        Assert.Single(results);
        Assert.Equal("git", results[0].Id.Value);
    }
}
