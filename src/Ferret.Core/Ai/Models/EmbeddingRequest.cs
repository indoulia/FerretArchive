namespace Ferret.Core.Ai.Models;

/// <summary>Input to an embedding model call.</summary>
public sealed record EmbeddingRequest
{
    /// <summary>Gets the text to embed.</summary>
    public required string Text { get; init; }

    /// <summary>Gets the fully-qualified model ID to use, or <see langword="null"/> to use the platform default.</summary>
    public string? ModelId { get; init; }
}
