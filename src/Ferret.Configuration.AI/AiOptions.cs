using System.ComponentModel.DataAnnotations;

namespace Ferret.Configuration.Ai;

/// <summary>Top-level AI platform configuration bound from <c>Ferret:Ai</c>.</summary>
public sealed class AiOptions
{
    /// <summary>Gets or sets the fully-qualified default chat model ID. Format: {provider}/{model}.</summary>
    [Required]
    public string DefaultChatModel { get; set; } = "ollama/llama3.2";

    /// <summary>Gets or sets the fully-qualified default embedding model ID. Format: {provider}/{model}.</summary>
    [Required]
    public string DefaultEmbeddingModel { get; set; } = "ollama/nomic-embed-text";

    /// <summary>Gets or sets the fully-qualified default reranker model ID. Null means no reranker configured.</summary>
    public string? DefaultReranker { get; set; }

    /// <summary>Gets or sets per-provider configuration keyed by provider name (e.g. "OpenAi", "Ollama").</summary>
#pragma warning disable CA2227 // POCO options class — config binder requires a setter to populate the collection
    public Dictionary<string, ProviderOptions> Providers { get; set; } = [];
#pragma warning restore CA2227
}
