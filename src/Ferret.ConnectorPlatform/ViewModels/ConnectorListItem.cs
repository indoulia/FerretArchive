namespace Ferret.ConnectorPlatform.ViewModels;

/// <summary>Presentation model for a single row in 'ferret connector list'.</summary>
internal sealed record ConnectorListItem(
    string Id,
    string Name,
    string Version,
    string PrimaryCapability,
    bool IsConfigured);
