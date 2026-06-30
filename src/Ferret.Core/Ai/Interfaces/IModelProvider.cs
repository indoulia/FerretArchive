using Ferret.Core.Ai.Models;

namespace Ferret.Core.Ai.Interfaces;

/// <summary>
/// Contract for an AI model provider. Vends typed model handles and lists available models.
/// Implementations live in <c>Ferret.Providers.*</c> packages; this interface is Ferret-owned.
/// </summary>
public interface IModelProvider
{
    /// <summary>Gets the provider's identity and aggregate capabilities.</summary>
    ProviderDescriptor Descriptor { get; }

    /// <summary>
    /// Returns a chat model handle for the given model ID,
    /// or <see langword="null"/> if the model is not available from this provider.
    /// </summary>
    /// <param name="modelId">The fully-qualified model identifier.</param>
    /// <returns>An <see cref="IChatModel"/> handle, or <see langword="null"/>.</returns>
    IChatModel? GetChatModel(ModelId modelId);

    /// <summary>
    /// Returns an embedding model handle for the given model ID,
    /// or <see langword="null"/> if the model is not available from this provider.
    /// </summary>
    /// <param name="modelId">The fully-qualified model identifier.</param>
    /// <returns>An <see cref="IEmbeddingModel"/> handle, or <see langword="null"/>.</returns>
    IEmbeddingModel? GetEmbeddingModel(ModelId modelId);

    /// <summary>
    /// Returns a reranker handle for the given model ID,
    /// or <see langword="null"/> if the model is not available from this provider.
    /// </summary>
    /// <param name="modelId">The fully-qualified model identifier.</param>
    /// <returns>An <see cref="IReranker"/> handle, or <see langword="null"/>.</returns>
    IReranker? GetReranker(ModelId modelId);

    /// <summary>
    /// Lists all models available from this provider.
    /// Unreachable or unconfigured providers return an empty list rather than throwing.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="ModelDescriptor"/> instances, one per available model.</returns>
    Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct);
}
