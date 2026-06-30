namespace Ferret.Core.Ai.Models;

/// <summary>A single streamed chunk from a chat model.</summary>
public sealed record ChatResponseChunk
{
    /// <summary>Gets the incremental text content of this chunk.</summary>
    public required string Delta { get; init; }

    /// <summary>Gets the finish reason if this is the final chunk, or <see langword="null"/> for intermediate chunks.</summary>
    public FinishReason? FinishReason { get; init; }
}
