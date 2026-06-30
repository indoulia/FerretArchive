namespace Ferret.Core.Search;

/// <summary>
/// The complete output of a search service request — results, status, execution metadata,
/// and diagnostics. The canonical output of the search pipeline; consumed by CLI handlers, MCP, REST, dashboards.
/// </summary>
public sealed record SearchServiceResult
{
    /// <summary>Gets the parsed query that drove this search. Always populated, even on failure.</summary>
    public required SearchQuery Query { get; init; }

    /// <summary>Gets the raw provider results. Null when <see cref="Status"/> is not <see cref="SearchServiceStatus.Success"/>.</summary>
    public SearchResult? Result { get; init; }

    /// <summary>Gets the outcome status of this request.</summary>
    public required SearchServiceStatus Status { get; init; }

    /// <summary>Gets the descriptor of the provider that executed the search. Null on pre-provider failure.</summary>
    public SearchProviderDescriptor? ProviderDescriptor { get; init; }

    /// <summary>Gets execution metadata (session ID, provider, duration, documents scanned, index version). Null on pre-execution failure.</summary>
    public SearchExecutionInfo? ExecutionInfo { get; init; }

    /// <summary>Gets diagnostics from parsing or execution (warnings, errors, recovery hints).</summary>
    public IReadOnlyList<SearchDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>Gets a value indicating whether the search completed successfully.</summary>
    public bool IsSuccess => Status == SearchServiceStatus.Success;

    /// <summary>Gets the ranked hits. Empty when <see cref="IsSuccess"/> is false.</summary>
    public IReadOnlyList<SearchHit> Hits => Result?.Hits ?? [];

    /// <summary>Creates a successful result.</summary>
    /// <param name="query">The parsed query that drove the search.</param>
    /// <param name="result">The provider results.</param>
    /// <param name="executionInfo">Execution metadata.</param>
    /// <param name="providerDescriptor">Optional descriptor of the provider that executed the search.</param>
    /// <returns>A successful <see cref="SearchServiceResult"/>.</returns>
    public static SearchServiceResult Success(
        SearchQuery query,
        SearchResult result,
        SearchExecutionInfo executionInfo,
        SearchProviderDescriptor? providerDescriptor = null) =>
        new()
        {
            Query = query,
            Result = result,
            Status = SearchServiceStatus.Success,
            ProviderDescriptor = providerDescriptor,
            ExecutionInfo = executionInfo,
        };

    /// <summary>Creates a failed result.</summary>
    /// <param name="query">The parsed query (or a stub when parsing failed).</param>
    /// <param name="status">The failure status code.</param>
    /// <param name="diagnostics">Diagnostics describing the failure.</param>
    /// <returns>A failed <see cref="SearchServiceResult"/>.</returns>
    public static SearchServiceResult Failure(
        SearchQuery query,
        SearchServiceStatus status,
        IReadOnlyList<SearchDiagnostic> diagnostics) =>
        new()
        {
            Query = query,
            Status = status,
            Diagnostics = diagnostics,
        };
}
