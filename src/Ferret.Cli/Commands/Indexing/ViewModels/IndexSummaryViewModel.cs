using Ferret.Core.Indexing;

namespace Ferret.Cli.Commands.Indexing.ViewModels;

/// <summary>Presentation model for the result of 'ferret index' pipeline run.</summary>
internal sealed record IndexSummaryViewModel
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

    /// <summary>Gets the total wall-clock duration of the pipeline run.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Gets the absolute path to the keyword index database.</summary>
    public required string DatabasePath { get; init; }

    /// <summary>Gets the individual failure messages, one per failed asset.</summary>
    public IReadOnlyList<string> FailureMessages { get; init; } = [];

    /// <summary>Creates an <see cref="IndexSummaryViewModel"/> from an <see cref="IndexResult"/> and database path.</summary>
    /// <param name="result">The pipeline result to map from.</param>
    /// <param name="databasePath">The absolute path to the keyword index database.</param>
    /// <returns>A new <see cref="IndexSummaryViewModel"/> populated from the result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static IndexSummaryViewModel From(IndexResult result, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new IndexSummaryViewModel
        {
            AssetsDiscovered = result.AssetsDiscovered,
            AssetsProcessed = result.AssetsProcessed,
            DocumentsIndexed = result.DocumentsIndexed,
            DocumentsSkipped = result.DocumentsSkipped,
            Failures = result.Failures,
            Duration = result.Duration,
            DatabasePath = databasePath,
            FailureMessages = result.FailureMessages,
        };
    }
}
