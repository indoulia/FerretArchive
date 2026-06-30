using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models;

namespace Ferret.Providers.Ollama;

/// <summary>OllamaSharp-backed embedding model handle.</summary>
internal sealed class OllamaEmbeddingModel : IEmbeddingModel
{
    private readonly string _modelName;
    private readonly HttpClient _httpClient;
    private readonly ModelDescriptor _descriptor;

    /// <summary>Initializes a new instance of the <see cref="OllamaEmbeddingModel"/> class.</summary>
    /// <param name="modelName">The local model name (e.g. "nomic-embed-text").</param>
    /// <param name="options">Ollama connection options.</param>
    /// <param name="logger">Logger for this model.</param>
    /// <param name="httpClient">HTTP client with BaseAddress already set.</param>
    public OllamaEmbeddingModel(
        string modelName,
        OllamaOptions options,
        ILogger<OllamaEmbeddingModel> logger,
        HttpClient httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(httpClient);

        _modelName = modelName;
        _httpClient = httpClient;
        _ = logger;
        _ = options;

        _descriptor = new ModelDescriptor
        {
            Id = ModelId.Create($"ollama/{modelName}"),
            ProviderId = ProviderId.Create("ollama"),
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

        using var client = new OllamaApiClient(_httpClient, _modelName);
        var ollamaRequest = new EmbedRequest
        {
            Model = _modelName,
            Input = [request.Text],
        };

        var response = await client.EmbedAsync(ollamaRequest, ct).ConfigureAwait(false);
        var vector = response.Embeddings?.FirstOrDefault() ?? [];

        return new EmbeddingResult
        {
            Vector = new ReadOnlyMemory<float>(vector),
            ModelId = _descriptor.Id,
            TokenCount = response.PromptEvalCount ?? 0,
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
            results.Add(await EmbedAsync(request, ct).ConfigureAwait(false));
        }

        return results.AsReadOnly();
    }
}
