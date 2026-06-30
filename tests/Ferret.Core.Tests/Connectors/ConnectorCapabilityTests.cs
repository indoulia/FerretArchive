using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorCapabilityTests
{
    [Fact]
    public void AssetDiscovery_Singleton_Is_Referentially_Stable()
    {
        Assert.Same(ConnectorCapabilities.AssetDiscovery, ConnectorCapabilities.AssetDiscovery);
    }

    [Fact]
    public void All_Contains_AssetDiscovery()
    {
        Assert.Contains(ConnectorCapabilities.AssetDiscovery, ConnectorCapabilities.All);
    }

    [Fact]
    public void All_Has_Eight_Entries()
    {
        Assert.Equal(8, ConnectorCapabilities.All.Count);
    }

    [Fact]
    public void ConnectorCapability_Equality_By_Id()
    {
        var a = new ConnectorCapability("asset-discovery", "Asset Discovery", "1.0", "desc");
        var b = new ConnectorCapability("asset-discovery", "Asset Discovery", "1.0", "desc");
        Assert.Equal(a, b);
    }
}
