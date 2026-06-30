namespace Ferret.Core.Ai.Models;

/// <summary>Complete response from a chat model.</summary>
public sealed record ChatResponse
{
    /// <summary>Gets the generated text content.</summary>
    public required string Content { get; init; }

    /// <summary>Gets the reason the model stopped generating tokens.</summary>
    public required FinishReason FinishReason { get; init; }

    /// <summary>Gets the token usage for this call.</summary>
    public required TokenUsage Usage { get; init; }
}
