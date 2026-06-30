using Ferret.Core.Connectors;

using Xunit;

namespace Ferret.Core.Tests.Connectors;

public sealed class ConnectorInstanceTests
{
    [Fact]
    public void SchemaVersion_Defaults_To_1_0()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Workspace",
        };

        Assert.Equal("1.0", instance.SchemaVersion);
    }

    [Fact]
    public void IsEnabled_Defaults_To_True()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Workspace",
        };

        Assert.True(instance.IsEnabled);
    }

    [Fact]
    public void Tags_Defaults_To_Empty()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Workspace",
        };

        Assert.Empty(instance.Tags);
    }

    [Fact]
    public void Configuration_Defaults_To_Empty()
    {
        var instance = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("default"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "Workspace",
        };

        Assert.Same(ConnectorConfiguration.Empty, instance.Configuration);
    }

    [Fact]
    public void Value_Equality_By_All_Properties()
    {
        var a = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("x"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "X",
        };
        var b = new ConnectorInstance
        {
            Id = new ConnectorInstanceId("x"),
            ConnectorType = new ConnectorId("filesystem"),
            DisplayName = "X",
        };

        Assert.Equal(a, b);
    }
}
