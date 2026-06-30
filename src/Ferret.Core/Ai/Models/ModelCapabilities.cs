namespace Ferret.Core.Ai.Models;

/// <summary>Capability flags describing what a model can do.</summary>
[Flags]
public enum ModelCapabilities
{
    /// <summary>No capabilities declared.</summary>
    None = 0,

    /// <summary>The model supports text-to-text chat (request/response and streaming).</summary>
    Chat = 1,

    /// <summary>The model can produce dense vector embeddings from text.</summary>
    Embedding = 2,

    /// <summary>The model can rerank a list of documents by relevance to a query.</summary>
    Reranking = 4,

    /// <summary>The model can accept image inputs in addition to text.</summary>
    Vision = 8,
}
