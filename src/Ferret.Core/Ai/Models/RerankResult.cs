namespace Ferret.Core.Ai.Models;

/// <summary>Result from a reranker — items ordered by descending score.</summary>
public sealed record RerankResult
{
    /// <summary>Gets the reranked documents in descending score order.</summary>
    public required IReadOnlyList<RerankItem> Items { get; init; }

    /// <summary>Creates a <see cref="RerankResult"/> with items sorted by descending relevance score.</summary>
    /// <param name="items">The raw reranked items from the provider.</param>
    /// <returns>A new <see cref="RerankResult"/> with <see cref="Items"/> in descending score order.</returns>
    public static RerankResult Create(IEnumerable<RerankItem> items) =>
        new() { Items = [.. items.OrderByDescending(i => i.Score)] };
}
