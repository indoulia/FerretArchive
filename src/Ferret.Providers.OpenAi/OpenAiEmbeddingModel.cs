using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

using Microsoft.Extensions.Logging;

using SdkEmbeddingClient = OpenAI.Embeddings.EmbeddingClient;

namespace Ferret.Providers.OpenAi;

/// <summary>OpenAI-SDK-backed embedding model handle.</summary>
internal sealed class OpenAiEmbeddingModel : IEmbeddingModel
{
    private readonly string _modelName;
    private readonly OpenAiOptions _options;
    private readonly ModelDescriptor _descriptor;

    /// <summary>Initializes a new instance of the <see cref="OpenAiEmbeddingModel"/> class.</summary>
    /// <param name="modelName">The local model name (e.g. "text-embedding-3-small").</param>
    /// <param name="options">OpenAI connection options.</param>
    /// <param name="logger">Logger for this model.</param>
    public OpenAiEmbeddingModel(string modelName, OpenAiOptions options, ILogger<OpenAiEmbeddingModel> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _modelName = modelName;
        _options = options;
        _ = logger;
        _descriptor = new ModelDescriptor
        {
            Id = ModelId.Create($"openai/{modelName}"),
            ProviderId = ProviderId.Create("openai"),
            DisplayName = modelName,
            Capabilities = ModelCapabilities.Embedding,
        };
    }

    /// <inheritdoc/>
    public ModelDescriptor Descriptor => _descriptor;

    /// <inheritdoc/>
    public async Task<EmbeddingResult> EmbedAsync(EmbeddingRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var client = new SdkEmbeddingClient(_modelName, _options.ApiKey ?? string.Empty);
        var result = await client.GenerateEmbeddingAsync(request.Text, cancellationToken: ct).ConfigureAwait(false);
        var vector = result.Value.ToFloats();
        return new EmbeddingResult
        {
            Vector = vector,
            ModelId = _descriptor.Id,
            TokenCount = 0,
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<EmbeddingRequest> requests,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var results = new List<EmbeddingResult>(requests.Count);
        foreach (var request in requests)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(await EmbedAsync(request, ct).ConfigureAwait(false));
        }

        return results.AsReadOnly();
    }
}
