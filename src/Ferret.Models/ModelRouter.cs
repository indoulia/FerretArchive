using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Ferret.Models.Exceptions;
using Microsoft.Extensions.Options;

namespace Ferret.Models;

/// <summary>Configuration-driven router that resolves chat and embedding models from the registry.</summary>
public sealed class ModelRouter : IModelRouter
{
    private readonly IModelRegistry _registry;
    private readonly ModelId _defaultChatModelId;
    private readonly ModelId _defaultEmbeddingModelId;

    /// <summary>Initializes a new instance of the <see cref="ModelRouter"/> class.</summary>
    /// <param name="registry">The model registry to resolve providers from.</param>
    /// <param name="options">AI options supplying the default model IDs.</param>
    public ModelRouter(IModelRegistry registry, IOptions<AiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(options);

        _registry = registry;
        _defaultChatModelId = ModelId.Create(options.Value.DefaultChatModel);
        _defaultEmbeddingModelId = ModelId.Create(options.Value.DefaultEmbeddingModel);
    }

    /// <inheritdoc/>
    public IChatModel GetDefaultChatModel()
    {
        return GetChatModel(_defaultChatModelId)
            ?? throw new ModelNotFoundException(_defaultChatModelId);
    }

    /// <inheritdoc/>
    public IChatModel? GetChatModel(ModelId id)
    {
        var slash = id.Value.IndexOf('/', StringComparison.Ordinal);
        if (slash < 0)
        {
            return null;
        }

        var providerPrefix = ProviderId.Create(id.Value[..slash]);
        return _registry.GetProvider(providerPrefix)?.GetChatModel(id);
    }

    /// <inheritdoc/>
    public IEmbeddingModel GetDefaultEmbeddingModel()
    {
        return GetEmbeddingModel(_defaultEmbeddingModelId)
            ?? throw new ModelNotFoundException(_defaultEmbeddingModelId);
    }

    /// <inheritdoc/>
    public IEmbeddingModel? GetEmbeddingModel(ModelId id)
    {
        var slash = id.Value.IndexOf('/', StringComparison.Ordinal);
        if (slash < 0)
        {
            return null;
        }

        var providerPrefix = ProviderId.Create(id.Value[..slash]);
        return _registry.GetProvider(providerPrefix)?.GetEmbeddingModel(id);
    }
}
