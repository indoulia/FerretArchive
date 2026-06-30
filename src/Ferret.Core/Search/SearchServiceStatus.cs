namespace Ferret.Core.Search;

/// <summary>
/// Describes the outcome of a search service request.
/// Expected environmental conditions are status codes, not exceptions.
/// Exceptions are reserved for genuine runtime failures (database corruption, unexpected I/O).
/// </summary>
public enum SearchServiceStatus
{
    /// <summary>Search completed successfully. <see cref="SearchServiceResult.Result"/> is populated.</summary>
    Success = 0,

    /// <summary>
    /// No <c>.ferret/</c> workspace was found in the current directory tree.
    /// CLI should print: "No workspace found. Run <c>ferret workspace init</c> first.".
    /// </summary>
    WorkspaceNotFound = 1,

    /// <summary>
    /// The workspace exists but the keyword index file is absent or was never built.
    /// CLI should print: "No index found. Run <c>ferret index</c> first.".
    /// </summary>
    IndexNotFound = 2,

    /// <summary>
    /// The requested <see cref="SearchExecutionMode"/> is not supported by any registered provider.
    /// Example: <see cref="SearchExecutionMode.Semantic"/> requested before Sprint 11 ships.
    /// </summary>
    ProviderUnavailable = 3,

    /// <summary>The raw query string could not be parsed into a valid AST. Diagnostics describe the error.</summary>
    InvalidQuery = 4,
}
