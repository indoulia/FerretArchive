using Ferret.Core.Search;
using Ferret.Search.Parsing;

namespace Ferret.Search;

/// <summary>
/// Parses a raw user query string into a canonical <see cref="SearchQuery"/> AST.
/// Implements <see cref="IQueryParser"/> — register via DI; do not construct directly in application code.
/// Sprint 10 constructs: whitespace-separated keywords (implicit AND), quoted phrases, trailing <c>*</c> prefix.
/// All failure modes are <see cref="SearchParseResult"/> values — the parser never throws for user input.
/// </summary>
public sealed class QueryParser : IQueryParser
{
    /// <inheritdoc/>
    public SearchParseResult Parse(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return SearchParseResult.Failure("Query must contain at least one search term.");
        }

        var tokens = new Lexer(rawQuery).Tokenize();

        if (tokens.Count == 0)
        {
            return SearchParseResult.Failure("Query must contain at least one search term.");
        }

        var expressions = BuildExpressions(tokens);
        var root = expressions.Count == 1 ? expressions[0] : new AndExpression(expressions);

        return SearchParseResult.Success(new SearchQuery
        {
            OriginalText = rawQuery,
            Root = root,
        });
    }

    private static List<SearchExpression> BuildExpressions(IReadOnlyList<Token> tokens)
    {
        var expressions = new List<SearchExpression>(tokens.Count);

        foreach (var token in tokens)
        {
            expressions.Add(token.Kind switch
            {
                TokenKind.Word => new KeywordExpression(token.Value),
                TokenKind.Phrase => new PhraseExpression(token.Value),
                TokenKind.Prefix => new PrefixExpression(token.Value),
                _ => throw new InvalidOperationException(
                    $"Unexpected token kind '{token.Kind}' at position {token.Position}."),
            });
        }

        return expressions;
    }
}
