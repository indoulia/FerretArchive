namespace Ferret.Core.Documents;

/// <summary>Host-configurable options for content parsers.</summary>
public sealed record ParserOptions
{
    /// <summary>Gets the maximum characters of extracted text to keep per document.
    /// Null (default) means unlimited — documents index completely unless an administrator caps them.</summary>
    public long? MaxExtractedCharacters { get; init; }
}
