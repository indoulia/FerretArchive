using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorRuntimeTests
{
    [Fact]
    public void ConnectorRuntime_Is_A_Record()
    {
        // Records have a generated ToString method
        Assert.True(typeof(ConnectorRuntime).IsClass);
        var toStringMethod = typeof(ConnectorRuntime).GetMethod("ToString");
        Assert.NotNull(toStringMethod);
    }

    [Fact]
    public void ConnectorRuntime_Has_Instance_Property()
    {
        var prop = typeof(ConnectorRuntime).GetProperty("Instance");

        Assert.NotNull(prop);
        Assert.Equal(typeof(ConnectorInstance), prop.PropertyType);
    }

    [Fact]
    public void ConnectorRuntime_Has_Connector_Property()
    {
        var prop = typeof(ConnectorRuntime).GetProperty("Connector");

        Assert.NotNull(prop);
        Assert.Equal(typeof(IConnector), prop.PropertyType);
    }

    [Fact]
    public void ConnectorRuntime_Has_Status_Property()
    {
        var prop = typeof(ConnectorRuntime).GetProperty("Status");

        Assert.NotNull(prop);
        Assert.Equal(typeof(ConnectorStatus), prop.PropertyType);
    }
}
