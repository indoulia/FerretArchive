using Ferret.Core.Connectors;

namespace Ferret.ConnectorPlatform.ViewModels;

/// <summary>Presentation model for 'ferret connector info &lt;id&gt;'.</summary>
internal sealed record ConnectorInfoView(
    ConnectorDescriptor Descriptor,
    bool IsConfigured);
