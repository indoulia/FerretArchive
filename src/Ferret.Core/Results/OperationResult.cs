#pragma warning disable SA1402 // non-generic and generic<T> companion types share one file by convention
namespace Ferret.Core.Results;

/// <summary>Represents the outcome of an operation that produces no value.</summary>
public sealed class OperationResult
{
    private OperationResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the error message when the operation failed, or <see langword="null"/> on success.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Creates a successful operation result.</summary>
    /// <returns>A successful <see cref="OperationResult"/>.</returns>
    public static OperationResult Success() => new(true, null);

    /// <summary>Creates a failed operation result with a message.</summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult"/>.</returns>
    public static OperationResult Failure(string errorMessage) => new(false, errorMessage);

    /// <summary>Creates a successful operation result carrying a value.</summary>
    /// <typeparam name="T">The type of the produced value.</typeparam>
    /// <param name="value">The produced value.</param>
    /// <returns>A successful <see cref="OperationResult{T}"/>.</returns>
    public static OperationResult<T> Success<T>(T value) => new(true, value, null);

    /// <summary>Creates a failed operation result with an error message.</summary>
    /// <typeparam name="T">The type of the produced value.</typeparam>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult{T}"/>.</returns>
    public static OperationResult<T> Failure<T>(string errorMessage) => new(false, default, errorMessage);
}

/// <summary>Represents the outcome of an operation that produces a value of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The type of the produced value.</typeparam>
public sealed class OperationResult<T>
{
    internal OperationResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the produced value on success, or the default of <typeparamref name="T"/> on failure.</summary>
    public T? Value { get; }

    /// <summary>Gets the error message when the operation failed, or <see langword="null"/> on success.</summary>
    public string? ErrorMessage { get; }
}
