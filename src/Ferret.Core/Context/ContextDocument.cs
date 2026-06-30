using Ferret.Core.Primitives;

namespace Ferret.Core.Context;

/// <summary>A single assembled document in a context assembly response.</summary>
public sealed record ContextDocument
{
    /// <summary>Gets the document identifier from the index.</summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>Gets the canonical URI for this document (e.g. filesystem:///src/auth.cs).</summary>
    public required Uri CanonicalUri { get; init; }

    /// <summary>Gets the human-readable label (e.g. relative file path).</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the document title extracted by the parser, if available.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the assembled content — full document text or section text.</summary>
    public required string Content { get; init; }

    /// <summary>Gets the relevance score from the search provider.</summary>
    public required float Score { get; init; }

    /// <summary>Gets the estimated token count for <see cref="Content"/> using the 4-chars-per-token approximation.</summary>
    public required int TokenEstimate { get; init; }

    /// <summary>Gets the document source, indicating whether this contains full text or a single section.</summary>
    public required ContextDocumentSource Source { get; init; }
}
