using Ferret.Cli.Cli;
using Ferret.Cli.Commands.Connector;
using Ferret.ConnectorPlatform.ViewModels;
using Xunit;

namespace Ferret.ConnectorPlatform.Tests;

/// <summary>Tests for <see cref="TextConnectorListFormatter"/>.</summary>
public sealed class TextConnectorListFormatterTests
{
    /// <summary>Format output for a single item must contain the connector ID.</summary>
    [Fact]
    public void Format_Contains_Connector_Id()
    {
        var output = FormatSingleItem("filesystem", "Filesystem Connector", "AssetDiscovery");
        Assert.Contains("filesystem", output, StringComparison.Ordinal);
    }

    /// <summary>Format output for a single item must contain the connector name.</summary>
    [Fact]
    public void Format_Contains_Connector_Name()
    {
        var output = FormatSingleItem("filesystem", "Filesystem Connector", "AssetDiscovery");
        Assert.Contains("Filesystem Connector", output, StringComparison.Ordinal);
    }

    /// <summary>Format output for an empty list must show the no-connectors message.</summary>
    [Fact]
    public void Format_Empty_List_Shows_No_Connectors_Message()
    {
        using var sw = new StringWriter();
        var formatter = new TextConnectorListFormatter();
        formatter.Format(new ConnectorListResult([]), new ConsoleFormatter(sw, VerbosityLevel.Normal));
        Assert.Contains("No connectors", sw.ToString(), StringComparison.Ordinal);
    }

    private static string FormatSingleItem(string id, string name, string capability)
    {
        using var sw = new StringWriter();
        var formatter = new TextConnectorListFormatter();
        var item = new ConnectorListItem(id, name, "1.0.0", capability, false);
        formatter.Format(
            new ConnectorListResult([item]),
            new ConsoleFormatter(sw, VerbosityLevel.Normal));
        return sw.ToString();
    }
}
