namespace Ferret.Core.Search;

/// <summary>
/// Orchestrates the full search pipeline: parse → validate → select provider → execute → post-process.
/// Exposes two overloads: a high-level string overload for CLI/MCP/REST callers, and a typed overload
/// for unit tests, benchmarks, AI agents, and future programmatic consumers.
/// The string overload parses and delegates to the typed overload — one implementation, no duplication.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Parses <paramref name="rawQuery"/> and executes a full search pipeline.
    /// Suitable for CLI, MCP, and REST callers that receive raw user input.
    /// </summary>
    /// <param name="rawQuery">The raw query string as entered by the user.</param>
    /// <param name="options">Execution options.</param>
    /// <returns>A <see cref="SearchServiceResult"/> with ranked hits and execution metadata.</returns>
    Task<SearchServiceResult> SearchAsync(string rawQuery, SearchOptions options);

    /// <summary>
    /// Executes a full search pipeline against a pre-parsed query.
    /// Suitable for unit tests, benchmarks, AI agents, saved searches, and programmatic consumers.
    /// </summary>
    /// <param name="query">The pre-parsed query AST.</param>
    /// <param name="options">Execution options.</param>
    /// <returns>A <see cref="SearchServiceResult"/> with ranked hits and execution metadata.</returns>
    Task<SearchServiceResult> SearchAsync(SearchQuery query, SearchOptions options);
}
