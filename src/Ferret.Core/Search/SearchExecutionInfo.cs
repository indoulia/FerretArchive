namespace Ferret.Core.Search;

/// <summary>
/// Execution metadata for a single search request. Carried in <see cref="SearchServiceResult"/>
/// for telemetry, distributed tracing, dashboard history, and diagnostics.
/// </summary>
public sealed record SearchExecutionInfo
{
    /// <summary>Gets a unique identifier for this search execution.
    /// Generated per-request by <c>SearchService</c>. Used for telemetry and distributed tracing.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Gets the display name of the provider that executed the search.</summary>
    public required string ProviderName { get; init; }

    /// <summary>Gets the wall-clock duration of the search execution (parse + provider + post-process).</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Gets the number of index documents scanned by the provider.</summary>
    public required int DocumentsScanned { get; init; }

    /// <summary>Gets the version string of the index that was queried.</summary>
    public required string IndexVersion { get; init; }
}
