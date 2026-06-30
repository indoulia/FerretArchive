using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>Query-document reranking contract for a single model handle.</summary>
public interface IReranker
{
    /// <summary>Gets the model's identity and capabilities.</summary>
    ModelDescriptor Descriptor { get; }

    /// <summary>Reranks the documents in the request by relevance to the query.</summary>
    /// <param name="request">The rerank request containing the query and documents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="RerankResult"/> with items ordered by descending relevance score.</returns>
    Task<RerankResult> RerankAsync(RerankRequest request, CancellationToken ct);
}
