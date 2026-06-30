namespace Ferret.Core.Connectors;

/// <summary>Well-known connector capabilities as immutable singletons. Use these instead of constructing new ConnectorCapability instances.</summary>
public static class ConnectorCapabilities
{
    /// <summary>Connector can enumerate assets as AssetDescriptors.</summary>
    public static readonly ConnectorCapability AssetDiscovery =
        new("asset-discovery", "Asset Discovery", "1.0", "Enumerate files and directories as AssetDescriptors.");

    /// <summary>Connector can detect assets added, changed, or deleted since last sync.</summary>
    public static readonly ConnectorCapability ChangeDetection =
        new("change-detection", "Change Detection", "1.0", "Detect assets added, changed, or deleted since last sync.");

    /// <summary>Connector supports real-time event streaming.</summary>
    public static readonly ConnectorCapability EventStreaming =
        new("event-streaming", "Event Streaming", "1.0", "Stream change events as they occur in real time.");

    /// <summary>Connector can write back to the source.</summary>
    public static readonly ConnectorCapability Write =
        new("write", "Write Back", "1.0", "Create, update, or delete assets in the source.");

    /// <summary>Connector supports point-in-time snapshots.</summary>
    public static readonly ConnectorCapability Snapshot =
        new("snapshot", "Snapshot", "1.0", "Capture a point-in-time snapshot of all assets.");

    /// <summary>Connector exposes relationships between assets.</summary>
    public static readonly ConnectorCapability Relationships =
        new("relationships", "Relationships", "1.0", "Expose references and relationships between assets.");

    /// <summary>Connector can delegate search queries to the source's native search engine.</summary>
    public static readonly ConnectorCapability NativeSearch =
        new("native-search", "Native Search", "1.0", "Delegate search queries to the source's native engine.");

    /// <summary>Connector supports post-discovery enrichment. Reserved for Sprint 9.</summary>
    public static readonly ConnectorCapability AssetEnrichment =
        new("asset-enrichment", "Asset Enrichment", "1.0", "Enrich AssetDescriptors with additional metadata after discovery.");

    /// <summary>Gets all well-known capabilities in definition order.</summary>
    public static IReadOnlyList<ConnectorCapability> All { get; } = [
        AssetDiscovery, ChangeDetection, EventStreaming, Write,
        Snapshot, Relationships, NativeSearch, AssetEnrichment,
    ];
}
