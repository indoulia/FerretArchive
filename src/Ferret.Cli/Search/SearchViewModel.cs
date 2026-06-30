using Ferret.Core.Search;

namespace Ferret.Cli.Search;

/// <summary>
/// View model produced by <c>SearchCommandHandler</c> and consumed by <c>SearchRendererSelector</c>.
/// Presentation models live in the CLI layer per ADR-0015, principle 5.
/// </summary>
public sealed record SearchViewModel
{
    /// <summary>Gets the raw query string as typed by the user.</summary>
    public required string OriginalQuery { get; init; }

    /// <summary>Gets the ranked file hits from <see cref="ISearchService"/>.</summary>
    public required IReadOnlyList<FileSearchHit> Hits { get; init; }

    /// <summary>Gets the provider name, duration, and document count from the search execution.</summary>
    public required SearchExecutionInfo ExecutionInfo { get; init; }
}
