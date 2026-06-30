namespace Ferret.Core.Results;

/// <summary>Represents the outcome of an indexing operation.</summary>
public sealed class IndexResult
{
    /// <summary>Initializes a new instance of the <see cref="IndexResult"/> class.</summary>
    /// <param name="indexedCount">The number of items successfully indexed.</param>
    /// <param name="failedCount">The number of items that failed to index.</param>
    /// <param name="isComplete">Indicates whether indexing completed without truncation.</param>
    public IndexResult(int indexedCount, int failedCount, bool isComplete)
    {
        IndexedCount = indexedCount;
        FailedCount = failedCount;
        IsComplete = isComplete;
    }

    /// <summary>Gets the number of items successfully indexed.</summary>
    public int IndexedCount { get; }

    /// <summary>Gets the number of items that failed to index.</summary>
    public int FailedCount { get; }

    /// <summary>Gets a value indicating whether indexing completed without truncation.</summary>
    public bool IsComplete { get; }
}
