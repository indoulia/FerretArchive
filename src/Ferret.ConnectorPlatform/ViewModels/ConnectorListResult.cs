namespace Ferret.ConnectorPlatform.ViewModels;

/// <summary>Presentation model for the full output of 'ferret connector list'.</summary>
internal sealed record ConnectorListResult(IReadOnlyList<ConnectorListItem> Items);
