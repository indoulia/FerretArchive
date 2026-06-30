namespace Ferret.Core.Connectors;

/// <summary>Describes a specific capability a connector can provide. Use ConnectorCapabilities for well-known singletons.</summary>
/// <param name="Id">Unique capability identifier (e.g. "asset-discovery").</param>
/// <param name="Name">Human-readable capability name.</param>
/// <param name="Version">Semantic version of this capability.</param>
/// <param name="Description">Short description for display in CLI and dashboards.</param>
public sealed record ConnectorCapability(string Id, string Name, string Version, string Description);
