namespace Ferret.Search.Parsing;

/// <summary>The kind of a lexed token.</summary>
internal enum TokenKind
{
    /// <summary>A plain keyword (e.g. <c>authentication</c>).</summary>
    Word,

    /// <summary>A quoted phrase with quotes stripped (e.g. input <c>"runtime builder"</c> → value <c>runtime builder</c>).</summary>
    Phrase,

    /// <summary>A prefix match with trailing <c>*</c> stripped (e.g. input <c>auth*</c> → value <c>auth</c>).</summary>
    Prefix,
}

/// <summary>A single lexed token produced by <see cref="Lexer"/>.</summary>
/// <param name="Kind">The token classification.</param>
/// <param name="Value">The token value (quotes and asterisks already stripped).</param>
/// <param name="Position">The zero-based character offset of this token in the original input.</param>
internal sealed record Token(TokenKind Kind, string Value, int Position);
