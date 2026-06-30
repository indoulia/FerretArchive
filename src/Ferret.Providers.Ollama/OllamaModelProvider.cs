using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OllamaSharp;

namespace Ferret.Providers.Ollama;

/// <summary>OllamaSharp-backed implementation of <see cref="IModelProvider"/>.</summary>
public sealed class OllamaModelProvider : IModelProvider, IDisposable
{
    private static readonly Action<ILogger, string, Exception?> LogListFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, "OllamaListFailed"),
            "Ollama ListLocalModelsAsync failed for '{BaseUrl}' — provider excluded from registry.");

    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaModelProvider> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly ProviderDescriptor _descriptor;

    /// <summary>Initializes a new instance of the <see cref="OllamaModelProvider"/> class.</summary>
    /// <param name="options">Ollama connection options.</param>
    /// <param name="logger">Logger for this provider.</param>
    /// <param name="httpClient">Optional HTTP client; one is created from <paramref name="options"/> when null.</param>
    /// <param name="loggerFactory">Optional logger factory for child model loggers; falls back to null factory.</param>
    public OllamaModelProvider(
        OllamaOptions options,
        ILogger<OllamaModelProvider> logger,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        if (httpClient is null)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            };
            _ownsHttpClient = true;
        }
        else
        {
            if (httpClient.BaseAddress is null)
            {
                httpClient.BaseAddress = new Uri(options.BaseUrl);
            }

            _httpClient = httpClient;
            _ownsHttpClient = false;
        }

        _descriptor = new ProviderDescriptor
        {
            Id = ProviderId.Create("ollama"),
            DisplayName = "Ollama",
            Capabilities = ModelCapabilities.Chat | ModelCapabilities.Embedding,
            Version = "1.0",
        };
    }

    /// <inheritdoc/>
    public ProviderDescriptor Descriptor => _descriptor;

    /// <inheritdoc/>
    public IChatModel? GetChatModel(ModelId modelId)
    {
        if (modelId.ProviderPrefix != "ollama")
        {
            return null;
        }

        return new OllamaChatModel(
            modelId.LocalName,
            _options,
            _loggerFactory.CreateLogger<OllamaChatModel>(),
            _httpClient);
    }

    /// <inheritdoc/>
    public IEmbeddingModel? GetEmbeddingModel(ModelId modelId)
    {
        if (modelId.ProviderPrefix != "ollama")
        {
            return null;
        }

        return new OllamaEmbeddingModel(
            modelId.LocalName,
            _options,
            _loggerFactory.CreateLogger<OllamaEmbeddingModel>(),
            _httpClient);
    }

    /// <inheritdoc/>
    public IReranker? GetReranker(ModelId modelId) => null;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct)
    {
        try
        {
            using var client = new OllamaApiClient(_httpClient, string.Empty);
            var models = await client.ListLocalModelsAsync(ct).ConfigureAwait(false);

            return models
                .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                .Select(m => new ModelDescriptor
                {
                    Id = ModelId.Create($"ollama/{m.Name}"),
                    ProviderId = _descriptor.Id,
                    DisplayName = m.Name!,
                    Capabilities = ModelCapabilities.Chat | ModelCapabilities.Embedding,
                })
                .ToList()
                .AsReadOnly();
        }
#pragma warning disable CA1031 // Intentional broad catch — any provider failure must be isolated
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogListFailed(_logger, _options.BaseUrl, ex);
            return Array.Empty<ModelDescriptor>();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
