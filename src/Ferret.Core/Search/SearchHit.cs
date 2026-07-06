using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Search;

/// <summary>
/// Base type for a single search result. Identity is always canonical — renderers derive display labels.
/// Concrete subtypes add kind-specific fields without nullable properties on the base.
/// </summary>
public abstract record SearchHit
{
    /// <summary>Gets the durable document identifier, derived from the source asset.</summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>Gets the connector instance that owns the source asset.
    /// Disambiguates two connectors indexing different roots (e.g. two filesystem connectors).</summary>
    public required ConnectorInstanceId ConnectorInstanceId { get; init; }

    /// <summary>Gets the universal locator for this document.
    /// Examples: <c>filesystem:///src/Program.cs</c>, <c>jira://ENG-1234</c>, <c>git://main/abc123</c>.
    /// Renderers derive human-friendly display labels from this value.</summary>
    public required Uri CanonicalUri { get; init; }

    /// <summary>Gets the renderer-derived human-friendly label for display.
    /// For filesystem hits, this is the relative file path; for JIRA hits, the issue key; etc.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the granularity of this hit.</summary>
    public required SearchHitKind Kind { get; init; }

    /// <summary>Gets the relevance score assigned by the provider.
    /// BM25 score in Sprint 10; vector similarity, hybrid score, or knowledge confidence in future sprints.</summary>
    public required float Score { get; init; }

    /// <summary>Gets the highlighted snippet for this hit.</summary>
    public required HighlightedText Snippet { get; init; }

    /// <summary>Gets the per-provider score breakdown. Null in Sprint 10; populated by Sprint 11+ providers.
    /// Example: "BM25: 0.91 | Semantic: 0.84 | Hybrid: 0.89".</summary>
    public string? Explanation { get; init; }

    /// <summary>Gets an opaque identifier for the upstream source that produced this hit, when a query
    /// fans out across multiple sources. Null for a single-source query. Core assigns no meaning to this
    /// value -- callers that fan out queries (e.g. <c>IFederatedKnowledgeStore</c>) define and populate it.</summary>
    public Guid? SourceId { get; init; }
}
