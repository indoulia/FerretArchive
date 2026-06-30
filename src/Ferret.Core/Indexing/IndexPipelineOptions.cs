using Ferret.Core.Connectors;

namespace Ferret.Core.Indexing;

/// <summary>Options controlling a single index pipeline run.</summary>
public sealed class IndexPipelineOptions
{
    /// <summary>Gets the shared default instance (no instance filter, no force rebuild).</summary>
    public static IndexPipelineOptions Default { get; } = new();

    /// <summary>Gets the connector instance to restrict indexing to, or <c>null</c> to index all instances.</summary>
    public ConnectorInstanceId? InstanceId { get; init; }

    /// <summary>Gets a value indicating whether to force a full rebuild, discarding any incremental state.</summary>
    public bool ForceRebuild { get; init; }
}
