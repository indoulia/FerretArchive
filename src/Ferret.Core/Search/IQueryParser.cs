namespace Ferret.Core.Search;

/// <summary>
/// Parses a raw user query string into a canonical <see cref="SearchQuery"/> AST.
/// Sprint 10 supports: whitespace-separated keywords (implicit AND), quoted phrases, trailing * prefix.
/// Never throws for syntactically invalid input — all outcomes are <see cref="SearchParseResult"/> values.
/// Implementation lives in <c>Ferret.Search</c>; interface lives in <c>Ferret.Core</c>.
/// </summary>
public interface IQueryParser
{
    /// <summary>
    /// Parses <paramref name="rawQuery"/> into a <see cref="SearchParseResult"/>.
    /// </summary>
    /// <param name="rawQuery">The raw query string as entered by the user.</param>
    /// <returns>The parse result containing the query or diagnostics.</returns>
    SearchParseResult Parse(string rawQuery);
}
