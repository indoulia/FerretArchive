using System.Runtime.CompilerServices;
using System.Text;

using Ferret.Configuration.Ai;
using Ferret.Core.Ai.Interfaces;
using Ferret.Core.Ai.Models;

using Microsoft.Extensions.Logging;

using SdkAssistantMessage = OpenAI.Chat.AssistantChatMessage;
using SdkChatClient = OpenAI.Chat.ChatClient;
using SdkChatOptions = OpenAI.Chat.ChatCompletionOptions;
using SdkFinishReason = OpenAI.Chat.ChatFinishReason;
using SdkMessage = OpenAI.Chat.ChatMessage;
using SdkSystemMessage = OpenAI.Chat.SystemChatMessage;
using SdkUserMessage = OpenAI.Chat.UserChatMessage;

namespace Ferret.Providers.OpenAi;

/// <summary>OpenAI-SDK-backed chat model handle.</summary>
internal sealed class OpenAiChatModel : IChatModel
{
    private readonly string _modelName;
    private readonly OpenAiOptions _options;
    private readonly ModelDescriptor _descriptor;

    /// <summary>Initializes a new instance of the <see cref="OpenAiChatModel"/> class.</summary>
    /// <param name="modelName">The local model name (e.g. "gpt-4o").</param>
    /// <param name="options">OpenAI connection options.</param>
    /// <param name="logger">Logger for this model.</param>
    public OpenAiChatModel(string modelName, OpenAiOptions options, ILogger<OpenAiChatModel> logger)
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
        var client = new SdkChatClient(_modelName, _options.ApiKey ?? string.Empty);
        var messages = MapMessages(request.Messages);
        var options = new SdkChatOptions
        {
            MaxOutputTokenCount = request.MaxTokens,
            Temperature = (float?)request.Temperature,
        };
        await foreach (var update in client.CompleteChatStreamingAsync(messages, options, ct).ConfigureAwait(false))
        {
            if (update is null)
            {
                continue;
            }

            var delta = string.Concat(update.ContentUpdate.Select(p => p.Text));
            FinishReason? finish = update.FinishReason.HasValue
                ? MapFinishReason(update.FinishReason.Value)
                : null;
            yield return new ChatResponseChunk
            {
                Delta = delta,
                FinishReason = finish,
            };
        }
    }

    private static IEnumerable<SdkMessage> MapMessages(IReadOnlyList<ChatMessage> messages) =>
        messages.Select<ChatMessage, SdkMessage>(m => m.Role switch
        {
            ChatRole.System => new SdkSystemMessage(m.Content),
            ChatRole.Assistant => new SdkAssistantMessage(m.Content),
            _ => new SdkUserMessage(m.Content),
        });

    private static FinishReason MapFinishReason(SdkFinishReason reason)
    {
        if (reason == SdkFinishReason.Stop)
        {
            return FinishReason.Stop;
        }

        if (reason == SdkFinishReason.Length)
        {
            return FinishReason.Length;
        }

        if (reason == SdkFinishReason.ToolCalls)
        {
            return FinishReason.ToolCalls;
        }

        if (reason == SdkFinishReason.ContentFilter)
        {
            return FinishReason.ContentFilter;
        }

        return FinishReason.Error;
    }
}
