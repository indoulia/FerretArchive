namespace Ferret.Core.Ai.Models;

/// <summary>A single scored document from a reranker.</summary>
public sealed record RerankItem
{
    /// <summary>Gets the document text.</summary>
    public required string Document { get; init; }

    /// <summary>Gets the relevance score assigned by the reranker (higher is more relevant).</summary>
    public required double Score { get; init; }

    /// <summary>Gets the zero-based position of this document in the original input list.</summary>
    public required int Index { get; init; }
}
