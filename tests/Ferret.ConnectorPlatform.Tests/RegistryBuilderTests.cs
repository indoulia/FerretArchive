using Ferret.ConnectorPlatform.Tests.Fakes;
using Ferret.Core.Connectors;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

public sealed class RegistryBuilderTests
{
    [Fact]
    public void Build_Returns_Registry_With_All_Factories()
    {
        var registry = RegistryBuilder.Build([new FakeConnectorFactory("filesystem")]);
        Assert.Single(registry.GetAll());
    }

    [Fact]
    public void Build_Empty_Returns_Empty_Registry()
    {
        var registry = RegistryBuilder.Build([]);
        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void Build_Throws_On_Duplicate_ConnectorId()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RegistryBuilder.Build([
                new FakeConnectorFactory("filesystem"),
                new FakeConnectorFactory("filesystem"),
            ]));
    }
}
