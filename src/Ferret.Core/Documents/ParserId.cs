namespace Ferret.Core.Documents;

/// <summary>Strongly-typed identifier for a content parser. By convention, use the primary MIME type the parser handles (e.g. "text/plain").</summary>
/// <param name="Value">The raw string value.</param>
public sealed record ParserId(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
