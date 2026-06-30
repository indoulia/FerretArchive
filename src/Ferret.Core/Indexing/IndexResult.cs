namespace Ferret.Core.Indexing;

/// <summary>Represents the outcome of a complete index pipeline run.</summary>
public sealed record IndexResult
{
    /// <summary>Gets the total number of assets discovered by the connector source.</summary>
    public required int AssetsDiscovered { get; init; }

    /// <summary>Gets the total number of assets processed (attempted parse).</summary>
    public required int AssetsProcessed { get; init; }

    /// <summary>Gets the number of documents successfully written to the index.</summary>
    public required int DocumentsIndexed { get; init; }

    /// <summary>Gets the number of documents skipped (unchanged since last run, or filtered).</summary>
    public required int DocumentsSkipped { get; init; }

    /// <summary>Gets the number of assets that failed during parsing or indexing.</summary>
    public required int Failures { get; init; }

    /// <summary>Gets the number of assets that produced warnings during processing.</summary>
    public required int Warnings { get; init; }

    /// <summary>Gets the total wall-clock duration of the pipeline run.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Gets the individual failure messages, one per failed asset.</summary>
    public IReadOnlyList<string> FailureMessages { get; init; } = [];

    /// <summary>Gets the individual warning messages, one per warned asset.</summary>
    public IReadOnlyList<string> WarningMessages { get; init; } = [];
}
