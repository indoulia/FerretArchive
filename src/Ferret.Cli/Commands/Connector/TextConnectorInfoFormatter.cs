using Ferret.Cli.Cli;
using Ferret.ConnectorPlatform.ViewModels;
using Ferret.Core.Connectors;

namespace Ferret.Cli.Commands.Connector;

/// <summary>Formats ConnectorInfoView as plain-text detail output.</summary>
internal sealed class TextConnectorInfoFormatter : ICommandResultFormatter<ConnectorInfoView>
{
    /// <inheritdoc/>
    public void Format(ConnectorInfoView view, IOutputFormatter output)
    {
        var d = view.Descriptor;
        output.WriteLine($"{d.Metadata.Name}  v{d.Metadata.Version}");
        output.WriteLine($"  ID:           {d.Id.Value}");
        output.WriteLine($"  Type:         {d.Metadata.ConnectorType}");
        output.WriteLine($"  Description:  {d.Metadata.Description}");
        output.WriteLine();
        output.WriteLine("  Capabilities");

        foreach (var known in ConnectorCapabilities.All)
        {
            var match = d.Capabilities.FirstOrDefault(c => c.Id == known.Id);
            var marker = match != null ? "✓" : "✗";
            var label = match != null ? $"{known.Name}  v{match.Version}" : known.Name;
            output.WriteLine($"    {marker}  {label}");
        }

        if (d.SupportedPlatforms.Count > 0)
        {
            output.WriteLine();
            output.WriteLine($"  Platforms:  {string.Join(", ", d.SupportedPlatforms)}");
        }

        output.WriteLine();
        var status = view.IsConfigured ? "Configured" : "Available (not configured)";
        output.WriteLine($"  Status:     {status}");
    }
}
