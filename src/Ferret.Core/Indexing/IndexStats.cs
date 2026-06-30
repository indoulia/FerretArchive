namespace Ferret.Core.Indexing;

/// <summary>Represents the current statistics of a keyword index.</summary>
public sealed record IndexStats
{
    /// <summary>Gets the total number of documents in the index.</summary>
    public required long DocumentCount { get; init; }

    /// <summary>Gets the total number of characters across all indexed plain-text fields.</summary>
    public required long TotalChars { get; init; }

    /// <summary>Gets the UTC timestamp of the most recent indexing write.</summary>
    public required DateTimeOffset LastIndexedAt { get; init; }

    /// <summary>Gets the total size of the index storage in bytes.</summary>
    public required long IndexSizeBytes { get; init; }
}
