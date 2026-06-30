using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Models.Exceptions;

namespace Ferret.Models;

/// <summary>Resolves default and named AI models from the registry.</summary>
public interface IModelRouter
{
    /// <summary>Returns the default chat model configured in <c>Ferret:Ai:DefaultChatModel</c>.</summary>
    /// <returns>The default <see cref="IChatModel"/>.</returns>
    /// <exception cref="ModelNotFoundException">If the configured default is not available.</exception>
    IChatModel GetDefaultChatModel();

    /// <summary>Returns the chat model for <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The fully-qualified model identifier.</param>
    /// <returns>The matching <see cref="IChatModel"/>, or <see langword="null"/>.</returns>
    IChatModel? GetChatModel(ModelId id);

    /// <summary>Returns the default embedding model configured in <c>Ferret:Ai:DefaultEmbeddingModel</c>.</summary>
    /// <returns>The default <see cref="IEmbeddingModel"/>.</returns>
    /// <exception cref="ModelNotFoundException">If the configured default is not available.</exception>
    IEmbeddingModel GetDefaultEmbeddingModel();

    /// <summary>Returns the embedding model for <paramref name="id"/>, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The fully-qualified model identifier.</param>
    /// <returns>The matching <see cref="IEmbeddingModel"/>, or <see langword="null"/>.</returns>
    IEmbeddingModel? GetEmbeddingModel(ModelId id);
}
