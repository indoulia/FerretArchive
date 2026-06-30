namespace Ferret.Core.Search;

/// <summary>
/// An immutable, parsed search query. Carries the original text alongside the canonical AST.
/// <see cref="OriginalText"/> is for telemetry, logging, query history, and "did you mean?" suggestions.
/// <see cref="Root"/> is the machine-readable form consumed by providers.
/// </summary>
public sealed record SearchQuery
{
    /// <summary>Gets the raw query text as entered by the user. Preserved verbatim.</summary>
    public required string OriginalText { get; init; }

    /// <summary>Gets the root of the parsed query AST.</summary>
    public required SearchExpression Root { get; init; }
}
