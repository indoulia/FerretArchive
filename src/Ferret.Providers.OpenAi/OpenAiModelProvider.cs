using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ferret.Providers.OpenAi;

/// <summary>OpenAI-SDK-backed implementation of <see cref="IModelProvider"/>. Uses a fixed model catalog — no network call at startup.</summary>
public sealed class OpenAiModelProvider : IModelProvider
{
    private static readonly Action<ILogger, string, Exception?> LogModelNotInCatalog =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, "OpenAiModelNotInCatalog"),
            "Model '{ModelId}' has openai prefix but is not in the static catalog — returning null.");

    private static readonly HashSet<string> ChatModelNames =
        new(StringComparer.Ordinal) { "gpt-4o", "gpt-4o-mini" };

    private static readonly HashSet<string> EmbeddingModelNames =
        new(StringComparer.Ordinal) { "text-embedding-3-small", "text-embedding-3-large" };

    private static readonly System.Collections.ObjectModel.ReadOnlyCollection<ModelDescriptor> Catalog = BuildCatalog();

    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiModelProvider> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ProviderDescriptor _descriptor;

    /// <summary>Initializes a new instance of the <see cref="OpenAiModelProvider"/> class.</summary>
    /// <param name="options">OpenAI connection options.</param>
    /// <param name="logger">Logger for this provider.</param>
    /// <param name="loggerFactory">Optional logger factory for child model loggers.</param>
    public OpenAiModelProvider(
        OpenAiOptions options,
        ILogger<OpenAiModelProvider> logger,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _logger = logger;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _descriptor = new ProviderDescriptor
        {
            Id = ProviderId.Create("openai"),
            DisplayName = "OpenAI",
            Capabilities = ModelCapabilities.Chat | ModelCapabilities.Embedding,
            Version = "1.0.0",
        };
    }

    /// <inheritdoc/>
    public ProviderDescriptor Descriptor => _descriptor;

    /// <inheritdoc/>
    public Task<IReadOnlyList<ModelDescriptor>> ListModelsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ModelDescriptor>>(Catalog);

    /// <inheritdoc/>
    public IChatModel? GetChatModel(ModelId modelId)
    {
        if (modelId.ProviderPrefix != "openai")
        {
            return null;
        }

        if (!ChatModelNames.Contains(modelId.LocalName))
        {
            LogModelNotInCatalog(_logger, modelId.Value, null);
            return null;
        }

        return new OpenAiChatModel(
            modelId.LocalName,
            _options,
            _loggerFactory.CreateLogger<OpenAiChatModel>());
    }

    /// <inheritdoc/>
    public IEmbeddingModel? GetEmbeddingModel(ModelId modelId)
    {
        if (modelId.ProviderPrefix != "openai")
        {
            return null;
        }

        if (!EmbeddingModelNames.Contains(modelId.LocalName))
        {
            LogModelNotInCatalog(_logger, modelId.Value, null);
            return null;
        }

        return new OpenAiEmbeddingModel(
            modelId.LocalName,
            _options,
            _loggerFactory.CreateLogger<OpenAiEmbeddingModel>());
    }

    /// <inheritdoc/>
    public IReranker? GetReranker(ModelId modelId) => null;

    private static System.Collections.ObjectModel.ReadOnlyCollection<ModelDescriptor> BuildCatalog()
    {
        var providerId = ProviderId.Create("openai");
        var list = new List<ModelDescriptor>
        {
            new()
            {
                Id = ModelId.Create("openai/gpt-4o"),
                ProviderId = providerId,
                DisplayName = "GPT-4o",
                Capabilities = ModelCapabilities.Chat,
            },
            new()
            {
                Id = ModelId.Create("openai/gpt-4o-mini"),
                ProviderId = providerId,
                DisplayName = "GPT-4o Mini",
                Capabilities = ModelCapabilities.Chat,
            },
            new()
            {
                Id = ModelId.Create("openai/text-embedding-3-small"),
                ProviderId = providerId,
                DisplayName = "text-embedding-3-small",
                Capabilities = ModelCapabilities.Embedding,
            },
            new()
            {
                Id = ModelId.Create("openai/text-embedding-3-large"),
                ProviderId = providerId,
                DisplayName = "text-embedding-3-large",
                Capabilities = ModelCapabilities.Embedding,
            },
        };
        return list.AsReadOnly();
    }
}
