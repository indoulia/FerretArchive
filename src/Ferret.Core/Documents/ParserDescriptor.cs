namespace Ferret.Core.Documents;

/// <summary>
/// Static descriptor for a registered content parser type. Immutable — no public setters.
/// Mirrors ConnectorDescriptor in the Connector Platform.
/// Priority determines dispatch order when multiple parsers support the same media type —
/// higher priority always wins over a more general parser (e.g. MarkdownParser 200 &gt; PlainTextParser 100).
/// </summary>
public sealed record ParserDescriptor
{
    /// <summary>Gets the parser identifier. By convention, use the primary MIME type handled.</summary>
    public required ParserId Id { get; init; }

    /// <summary>Gets the human-readable parser name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the parser version string.</summary>
    public required string Version { get; init; }

    /// <summary>Gets the MIME types this parser handles.</summary>
    public required IReadOnlyList<string> SupportedMediaTypes { get; init; }

    /// <summary>Gets the capabilities this parser provides.</summary>
    public required IReadOnlyList<ParserCapability> Capabilities { get; init; }

    /// <summary>Gets the dispatch priority. Higher values win when multiple parsers support the same media type.
    /// Convention: 100 = general fallback, 200 = specific format, 500 = user-supplied override.</summary>
    public int Priority { get; init; } = 100;
}
