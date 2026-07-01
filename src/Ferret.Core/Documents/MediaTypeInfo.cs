namespace Ferret.Core.Documents;

/// <summary>
/// Richer MIME type resolution result. Returned by IMimeTypeResolver in place of a raw string
/// so callers have enough context to make decisions (binary skip, kind suggestion, confidence)
/// without re-examining the file name. Immutable.
/// </summary>
public sealed record MediaTypeInfo
{
    /// <summary>Gets the resolved MIME type string (e.g. "text/markdown").</summary>
    public required string MediaType { get; init; }

    /// <summary>Gets the content category for this media type.</summary>
    public required MediaCategory Category { get; init; }

    /// <summary>Gets a value indicating whether the content is human-readable text. Derived from <see cref="Category"/>.</summary>
    public bool IsText => Category == MediaCategory.Text;

    /// <summary>Gets a value indicating whether the content is binary. Derived from <see cref="Category"/>.</summary>
    public bool IsBinary => Category != MediaCategory.Text;

    /// <summary>Gets an optional suggested DocumentKind hint for the parser.</summary>
    public DocumentKind? SuggestedKind { get; init; }

    /// <summary>Gets the resolver's confidence in this classification (0.0–1.0).</summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>Gets a <see cref="MediaTypeInfo"/> representing an unrecognized binary file.</summary>
    public static MediaTypeInfo Unknown => new()
    {
        MediaType = "application/octet-stream",
        Category = MediaCategory.BinaryOpaque,
        Confidence = 0.5,
    };
}
