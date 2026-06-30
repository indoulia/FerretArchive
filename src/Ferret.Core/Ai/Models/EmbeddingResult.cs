namespace Ferret.Core.Ai.Models;

/// <summary>Result from an embedding model call.</summary>
public sealed record EmbeddingResult
{
    /// <summary>Gets the dense vector representation of the input text.</summary>
    public required ReadOnlyMemory<float> Vector { get; init; }

    /// <summary>Gets the model that produced this embedding.</summary>
    public required ModelId ModelId { get; init; }

    /// <summary>Gets the number of tokens consumed by the embedding call.</summary>
    public required int TokenCount { get; init; }
}
