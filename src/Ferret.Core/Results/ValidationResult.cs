namespace Ferret.Core.Results;

/// <summary>Represents the outcome of a validation operation, including all failures.</summary>
public sealed class ValidationResult
{
    private ValidationResult(bool isValid, IReadOnlyList<ValidationFailure> failures)
    {
        IsValid = isValid;
        Failures = failures;
    }

    /// <summary>Gets a value indicating whether validation passed with no errors.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the collection of validation failures, empty when valid.</summary>
    public IReadOnlyList<ValidationFailure> Failures { get; }

    /// <summary>Creates a valid result with no failures.</summary>
    /// <returns>A valid <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Valid() => new(true, []);

    /// <summary>Creates an invalid result from a list of failures.</summary>
    /// <param name="failures">The validation failures.</param>
    /// <returns>An invalid <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Invalid(IReadOnlyList<ValidationFailure> failures) =>
        new(false, failures);
}
