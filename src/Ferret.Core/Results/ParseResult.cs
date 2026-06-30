#pragma warning disable SA1402 // non-generic factory and generic<T> companion types share one file by convention
namespace Ferret.Core.Results;

/// <summary>Factory for creating <see cref="ParseResult{T}"/> instances.</summary>
public static class ParseResult
{
    /// <summary>Creates a successful parse result carrying a value.</summary>
    /// <typeparam name="T">The type of the parsed value.</typeparam>
    /// <param name="value">The parsed value.</param>
    /// <returns>A successful <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Success<T>(T value) => new(true, value, null);

    /// <summary>Creates a failed parse result with an error message.</summary>
    /// <typeparam name="T">The type of the parsed value.</typeparam>
    /// <param name="errorMessage">The error message describing the parse failure.</param>
    /// <returns>A failed <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Failure<T>(string errorMessage) => new(false, default, errorMessage);
}

/// <summary>Represents the result of a parse operation that produces a value of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
public sealed class ParseResult<T>
{
    internal ParseResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether parsing succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the parsed value on success, or the default of <typeparamref name="T"/> on failure.</summary>
    public T? Value { get; }

    /// <summary>Gets the error message when parsing failed, or <see langword="null"/> on success.</summary>
    public string? ErrorMessage { get; }
}
