namespace Ferret.Core.Documents;

/// <summary>The single shared implementation of the configurable extracted-text limit.
/// Every heavyweight parser (PDF, Word, Excel) calls this — no per-parser truncation logic.</summary>
public static class ExtractionLimiter
{
    /// <summary>Applies <see cref="ParserOptions.MaxExtractedCharacters"/> to <paramref name="text"/>.</summary>
    /// <param name="text">The extracted text.</param>
    /// <param name="options">Parser options carrying the optional limit.</param>
    /// <returns>The (possibly truncated) text and whether truncation occurred.</returns>
    public static (string Text, bool Truncated) ApplyCharacterLimit(string text, ParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxExtractedCharacters is long max && text.Length > max)
        {
            // max < text.Length (an int) here, so it fits int; Math.Min guards against any future reordering.
            var limit = (int)Math.Min(max, text.Length);
            return (text[..limit], true);
        }

        return (text, false);
    }
}
