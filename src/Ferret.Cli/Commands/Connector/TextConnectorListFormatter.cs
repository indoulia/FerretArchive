using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.ViewModels;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Formats ConnectorListResult as plain-text tabular output.</summary>
internal sealed class TextConnectorListFormatter : ICommandResultFormatter<ConnectorListResult>
{
    /// <inheritdoc/>
    public void Format(ConnectorListResult result, IOutputFormatter output)
    {
        if (result.Items.Count == 0)
        {
            output.WriteLine("No connectors are registered.");
            output.WriteLine();
            output.WriteLine("Next: Install a connector package and register it in Program.cs.");
            return;
        }

        const int IdWidth = 14;
        const int NameWidth = 24;
        const int VerWidth = 9;

        output.WriteLine(
            $"{"ID",-IdWidth}  {"NAME",-NameWidth}  {"VERSION",-VerWidth}  {"CAPABILITIES",-16}  CONFIGURED");
        output.WriteLine(new string('-', 80));

        foreach (var item in result.Items)
        {
            output.WriteLine(
                $"{item.Id,-IdWidth}  {item.Name,-NameWidth}  {item.Version,-VerWidth}  {item.PrimaryCapability,-16}  {(item.IsConfigured ? "yes" : "no")}");
        }
    }
}
