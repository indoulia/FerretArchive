namespace Ferret.Core.Ai.Models;

/// <summary>Input to a reranker call.</summary>
public sealed record RerankRequest
{
    /// <summary>Gets the query used to score documents by relevance.</summary>
    public required string Query { get; init; }

    /// <summary>Gets the list of documents to rerank.</summary>
    public required IReadOnlyList<string> Documents { get; init; }

    /// <summary>Gets the fully-qualified model ID to use, or <see langword="null"/> to use the platform default.</summary>
    public string? ModelId { get; init; }
}
