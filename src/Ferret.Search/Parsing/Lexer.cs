namespace Ferret.Search.Parsing;

/// <summary>
/// Converts a raw query string into a flat list of <see cref="Token"/> values.
/// Recognises three token forms: plain words (keyword), quoted phrases, and words ending with <c>*</c> (prefix).
/// Whitespace is consumed as a delimiter and produces no tokens.
/// Unclosed quotes are treated leniently — the remaining input becomes the phrase value.
/// </summary>
internal sealed class Lexer
{
    private readonly string _input;
    private int _pos;

    /// <summary>Initializes a new instance of the <see cref="Lexer"/> class for the given raw query string.</summary>
    /// <param name="input">The raw query string.</param>
    internal Lexer(string input)
    {
        _input = input;
        _pos = 0;
    }

    /// <summary>
    /// Scans the input and returns all tokens in source order.
    /// Never throws. Returns an empty list for empty or whitespace-only input.
    /// </summary>
    internal IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();
        SkipWhitespace();

        while (_pos < _input.Length)
        {
            var start = _pos;
            tokens.Add(_input[_pos] == '"' ? ReadPhrase(start) : ReadWordOrPrefix(start));
            SkipWhitespace();
        }

        return tokens;
    }

    private Token ReadPhrase(int start)
    {
        _pos++; // consume opening "
        var valueStart = _pos;

        while (_pos < _input.Length && _input[_pos] != '"')
        {
            _pos++;
        }

        var value = _input[valueStart.._pos];

        if (_pos < _input.Length)
        {
            _pos++; // consume closing "
        }

        // else: unclosed quote — treat remaining input as phrase value (lenient)
        return new Token(TokenKind.Phrase, value, start);
    }

    private Token ReadWordOrPrefix(int start)
    {
        while (_pos < _input.Length && !char.IsWhiteSpace(_input[_pos]))
        {
            _pos++;
        }

        var raw = _input[start.._pos];

        return raw.EndsWith('*')
            ? new Token(TokenKind.Prefix, raw[..^1], start)
            : new Token(TokenKind.Word, raw, start);
    }

    private void SkipWhitespace()
    {
        while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
        {
            _pos++;
        }
    }
}
