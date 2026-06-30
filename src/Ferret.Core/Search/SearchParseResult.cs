namespace Ferret.Core.Search;

/// <summary>
/// The outcome of a query parse attempt. The parser never throws for user input;
/// all failure modes are represented as <see cref="SearchParseResult"/> values.
/// Use the static factory methods to construct instances.
/// </summary>
public sealed class SearchParseResult
{
    private SearchParseResult()
    {
    }

    /// <summary>Gets a value indicating whether parsing produced a valid query.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>Gets the parsed query. Only valid when <see cref="IsSuccess"/> is true.</summary>
    public SearchQuery? Query { get; private init; }

    /// <summary>Gets diagnostics collected during parsing.</summary>
    public IReadOnlyList<SearchDiagnostic> Diagnostics { get; private init; } = [];

    /// <summary>
    /// Parsing succeeded and produced a valid query.
    /// </summary>
    /// <param name="query">The parsed query.</param>
    /// <returns>A successful parse result.</returns>
    public static SearchParseResult Success(SearchQuery query) =>
        new() { IsSuccess = true, Query = query };

    /// <summary>
    /// Parsing failed with multiple diagnostics.
    /// </summary>
    /// <param name="diagnostics">The collection of diagnostics.</param>
    /// <returns>A failed parse result.</returns>
    public static SearchParseResult Failure(IReadOnlyList<SearchDiagnostic> diagnostics) =>
        new() { IsSuccess = false, Diagnostics = diagnostics };

    /// <summary>
    /// Parsing failed with a single error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A failed parse result.</returns>
    public static SearchParseResult Failure(string message) =>
        Failure([new SearchDiagnostic(SearchDiagnosticSeverity.Error, message)]);
}
