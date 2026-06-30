namespace Ferret.Core.Search;

/// <summary>
/// Describes the search capabilities of a provider.
/// Sprint 10: BM25 providers set <see cref="SupportsKeyword"/>, <see cref="SupportsPhrase"/>,
/// and <see cref="SupportsPrefix"/>. Semantic and hybrid capabilities are reserved.
/// </summary>
public sealed record SearchCapabilities
{
    /// <summary>Gets a value indicating whether this provider supports keyword (BM25) search.</summary>
    public required bool SupportsKeyword { get; init; }

    /// <summary>Gets a value indicating whether this provider supports exact phrase matching.</summary>
    public required bool SupportsPhrase { get; init; }

    /// <summary>Gets a value indicating whether this provider supports prefix wildcard matching.</summary>
    public required bool SupportsPrefix { get; init; }

    /// <summary>Gets a value indicating whether this provider supports embedding-based semantic similarity search. Reserved for Sprint 11+.</summary>
    public bool SupportsSemantic { get; init; }

    /// <summary>Gets a value indicating whether this provider supports hybrid (keyword + semantic) fusion search. Reserved for Sprint 12+.</summary>
    public bool SupportsHybrid { get; init; }
}
