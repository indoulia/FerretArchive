using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Documents;

/// <summary>
/// The canonical output of the parsing stage — the Document Platform's parallel to AssetDescriptor
/// in the Connector Platform. Immutable: any transformation creates a new Document instance.
/// Provenance is always preserved: Document → AssetDescriptor → IConnector → IConnectorRegistry.
/// </summary>
public sealed record Document
{
    /// <summary>Gets the document identifier, derived deterministically from <see cref="SourceAssetId"/>.
    /// DocumentId equals SourceAssetId.Value — one asset, one document in Sprint 9.</summary>
    public required DocumentId Id { get; init; }

    /// <summary>Gets the identifier of the source asset that produced this document.</summary>
    public required AssetId SourceAssetId { get; init; }

    /// <summary>Gets the connector type that owns the source asset.</summary>
    public required ConnectorId ConnectorId { get; init; }

    /// <summary>Gets the workspace-scoped connector instance that owns the source asset.</summary>
    public required ConnectorInstanceId InstanceId { get; init; }

    /// <summary>Gets the MIME type of the source content (e.g. "text/markdown").</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the semantic kind of this document. Assigned by the parser — not inferred from MediaType.</summary>
    public required DocumentKind Kind { get; init; }

    /// <summary>Gets the full plain-text representation of the document content.
    /// This is the primary field indexed by the keyword (FTS5) index.</summary>
    public required string PlainText { get; init; }

    /// <summary>Gets the UTC timestamp at which this document was produced by the parser.</summary>
    public required DateTimeOffset ProducedAt { get; init; }

    /// <summary>Gets the fingerprint of the source asset at the time this document was produced.
    /// Used by the indexing pipeline to determine whether re-parsing is needed without re-reading source content.
    /// This is the foundation for incremental indexing in a future sprint.</summary>
    public AssetFingerprint? SourceFingerprint { get; init; }

    /// <summary>Gets the document title extracted by the parser (e.g. first H1 in Markdown). May be null.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the structural sections extracted by the parser.
    /// Sprint 9: H1/H2 Markdown headings. Future parsers may extract any structural element.</summary>
    public IReadOnlyList<DocumentSection> Sections { get; init; } = [];

    /// <summary>Gets parser-assigned metadata as key-value pairs.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
