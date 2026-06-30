using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>Text embedding contract for a single model handle.</summary>
public interface IEmbeddingModel
{
    /// <summary>Gets the model's identity and capabilities.</summary>
    ModelDescriptor Descriptor { get; }

    /// <summary>Embeds a single text input.</summary>
    /// <param name="request">The embedding request containing the text to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="EmbeddingResult"/> containing the vector and token count.</returns>
    Task<EmbeddingResult> EmbedAsync(EmbeddingRequest request, CancellationToken ct);

    /// <summary>
    /// Embeds a batch of text inputs.
    /// Implementations may submit inputs in a single provider call or loop internally;
    /// callers must not assume any specific batching strategy.
    /// </summary>
    /// <param name="requests">The list of embedding requests to process.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="EmbeddingResult"/> values in the same order as <paramref name="requests"/>.</returns>
    Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<EmbeddingRequest> requests, CancellationToken ct);
}
