using Ferret.Core.Search;

namespace Ferret.Search.Providers.Bm25;

/// <summary>
/// Translates a <see cref="SearchExpression"/> AST into an FTS5 query string.
/// This is the ONLY place in <c>Ferret.Search</c> that produces SQLite/FTS5 syntax (ADR-0015, principle 3).
/// </summary>
internal static class QueryTranslator
{
    private static readonly HashSet<string> Fts5ReservedWords =
        new(StringComparer.OrdinalIgnoreCase) { "AND", "OR", "NOT", "NEAR" };

    /// <summary>Translates a <see cref="SearchExpression"/> to an FTS5 MATCH argument string.</summary>
    internal static string Translate(SearchExpression expression) =>
        expression switch
        {
            KeywordExpression { Value: var v } => EscapeKeyword(v),
            PhraseExpression { Value: var v } => $"\"{v.Replace("\"", "\"\"", StringComparison.Ordinal)}\"",
            PrefixExpression { Prefix: var p } when p.Length == 0 => "*",
            PrefixExpression { Prefix: var p } => $"{EscapeKeyword(p)}*",
            AndExpression { Operands: var operands } =>
                string.Join(" ", operands.Select(Translate)),
            _ => throw new InvalidOperationException(
                $"Unsupported expression type '{expression.GetType().Name}' — not supported in Sprint 10."),
        };

    private static string EscapeKeyword(string value)
    {
        // FTS5 reserved words must be double-quoted to search for their literal text.
        if (Fts5ReservedWords.Contains(value))
        {
            return $"\"{value}\"";
        }

        // Keywords containing non-alphanumeric characters (other than _) are also quoted.
        // A bare '-' is FTS5's NOT operator, not a literal character, so it cannot be left unquoted.
        return value.Any(c => !char.IsLetterOrDigit(c) && c != '_')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}
