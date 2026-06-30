using System.Runtime.CompilerServices;
using System.Text;

using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;
using Microsoft.Extensions.Logging;
using OllamaSharp;

using OllamaApiChatRequest = OllamaSharp.Models.Chat.ChatRequest;
using OllamaMessage = OllamaSharp.Models.Chat.Message;
using OllamaRole = OllamaSharp.Models.Chat.ChatRole;

namespace Ferret.Providers.Ollama;

/// <summary>OllamaSharp-backed chat model handle.</summary>
internal sealed class OllamaChatModel : IChatModel
{
    private static readonly OllamaRole SystemRole = OllamaRole.System;
    private static readonly OllamaRole UserRole = OllamaRole.User;
    private static readonly OllamaRole AssistantRole = OllamaRole.Assistant;

    private readonly string _modelName;
    private readonly HttpClient _httpClient;
    private readonly ModelDescriptor _descriptor;

    /// <summary>Initializes a new instance of the <see cref="OllamaChatModel"/> class.</summary>
    /// <param name="modelName">The local model name (e.g. "llama3.2").</param>
    /// <param name="options">Ollama connection options.</param>
    /// <param name="logger">Logger for this model.</param>
    /// <param name="httpClient">HTTP client with BaseAddress already set.</param>
    public OllamaChatModel(
        string modelName,
        OllamaOptions options,
        ILogger<OllamaChatModel> logger,
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
            Capabilities = ModelCapabilities.Chat,
        };
    }

    /// <inheritdoc/>
    public ModelDescriptor Descriptor => _descriptor;

    /// <inheritdoc/>
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sb = new StringBuilder();
        var finishReason = FinishReason.Stop;

        await foreach (var chunk in ChatStreamAsync(request, ct).ConfigureAwait(false))
        {
            sb.Append(chunk.Delta);
            if (chunk.FinishReason.HasValue)
            {
                finishReason = chunk.FinishReason.Value;
            }
        }

        return new ChatResponse
        {
            Content = sb.ToString(),
            FinishReason = finishReason,
            Usage = new TokenUsage { InputTokens = 0, OutputTokens = 0, TotalTokens = 0 },
        };
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseChunk> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var client = new OllamaApiClient(_httpClient, _modelName);
        var ollamaRequest = new OllamaApiChatRequest
        {
            Model = _modelName,
            Messages = MapMessages(request.Messages),
            Stream = true,
        };

        await foreach (var chunk in client.ChatAsync(ollamaRequest, ct).ConfigureAwait(false))
        {
            if (chunk is null)
            {
                continue;
            }

            yield return new ChatResponseChunk
            {
                Delta = chunk.Message?.Content ?? string.Empty,
                FinishReason = chunk.Done ? FinishReason.Stop : null,
            };
        }
    }

    private static IEnumerable<OllamaMessage> MapMessages(IReadOnlyList<ChatMessage> messages) =>
        messages.Select(m => new OllamaMessage(MapRole(m.Role), m.Content));

    private static OllamaRole MapRole(ChatRole role) => role switch
    {
        ChatRole.System => SystemRole,
        ChatRole.Assistant => AssistantRole,
        _ => UserRole,
    };
}
