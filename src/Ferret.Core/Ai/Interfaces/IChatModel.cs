using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>Chat and streaming chat contract for a single model handle.</summary>
public interface IChatModel
{
    /// <summary>Gets the model's identity and capabilities.</summary>
    ModelDescriptor Descriptor { get; }

    /// <summary>Sends a chat request and returns the complete response.</summary>
    /// <param name="request">The chat request containing messages and options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The complete <see cref="ChatResponse"/> from the model.</returns>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct);

    /// <summary>
    /// Sends a chat request and streams response chunks as they are produced.
    /// Providers that do not support native streaming must yield the complete
    /// response as a single <see cref="ChatResponseChunk"/>.
    /// </summary>
    /// <param name="request">The chat request containing messages and options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async sequence of <see cref="ChatResponseChunk"/> values.</returns>
    IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(ChatRequest request, CancellationToken ct);
}
