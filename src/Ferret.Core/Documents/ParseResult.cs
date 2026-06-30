#pragma warning disable CA1000 // Do not declare static members on generic types — factory pattern requires static members on ParseResult<T>
namespace Ferret.Core.Documents;

/// <summary>
/// Represents the outcome of a parse dispatch operation.
/// All failure modes are explicit outcomes — the dispatcher never throws.
/// Use the static factory methods to construct instances.
/// </summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
public sealed class ParseResult<T>
{
    private ParseResult()
    {
    }

    /// <summary>Gets a value indicating whether parsing produced a valid result.</summary>
    public bool IsSuccess => Kind == ParseResultKind.Success;

    /// <summary>Gets the parsed value. Only valid when <see cref="IsSuccess"/> is true.</summary>
    public T? Value { get; private init; }

    /// <summary>Gets the outcome kind.</summary>
    public ParseResultKind Kind { get; private init; }

    /// <summary>Gets diagnostics collected during parsing (warnings, errors, info notes).</summary>
    public IReadOnlyList<ParseDiagnostic> Diagnostics { get; private init; } = [];

    /// <summary>Parsing succeeded and produced a valid result.</summary>
    /// <param name="value">The parsed value.</param>
    /// <returns>A successful <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Success(T value) =>
        new() { Kind = ParseResultKind.Success, Value = value };

    /// <summary>No parser is registered for the given media type.</summary>
    /// <param name="mediaType">The media type for which no parser is registered.</param>
    /// <returns>An unsupported <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Unsupported(string mediaType) =>
        new()
        {
            Kind = ParseResultKind.Unsupported,
            Diagnostics =
            [
                new ParseDiagnostic(
                    ParseDiagnosticSeverity.Warning,
                    $"No parser registered for media type '{mediaType}'"),
            ],
        };

    /// <summary>The content stream was empty or whitespace-only.</summary>
    /// <returns>An empty <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Empty() =>
        new() { Kind = ParseResultKind.Empty };

    /// <summary>The parser failed with an error message.</summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <returns>A failed <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Failed(string message) =>
        new()
        {
            Kind = ParseResultKind.Failed,
            Diagnostics = [new ParseDiagnostic(ParseDiagnosticSeverity.Error, message)],
        };

    /// <summary>The parser failed and collected multiple diagnostics.</summary>
    /// <param name="diagnostics">The collected diagnostics.</param>
    /// <returns>A failed <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Failed(IReadOnlyList<ParseDiagnostic> diagnostics) =>
        new() { Kind = ParseResultKind.Failed, Diagnostics = diagnostics };
}
