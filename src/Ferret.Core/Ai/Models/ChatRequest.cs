namespace Ferret.Core.Ai.Models;

/// <summary>Input to a chat model call.</summary>
public sealed record ChatRequest
{
    /// <summary>Gets the ordered list of messages forming the conversation history.</summary>
    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    /// <summary>Gets the fully-qualified model ID to use, or <see langword="null"/> to use the platform default.</summary>
    public string? ModelId { get; init; }

    /// <summary>Gets the sampling temperature. Higher values produce more random outputs. Defaults to 0.7.</summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>Gets the maximum number of tokens to generate, or <see langword="null"/> for the model default.</summary>
    public int? MaxTokens { get; init; }
}
