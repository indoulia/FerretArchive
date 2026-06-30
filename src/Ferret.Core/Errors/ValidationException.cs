namespace Ferret.Core.Errors;

/// <summary>Thrown when input validation fails for a specific field or constraint.</summary>
public sealed class ValidationException : FerretException
{
    /// <summary>Initializes a new instance of the <see cref="ValidationException"/> class.</summary>
    public ValidationException()
        : base("Validation failed.")
    {
        Field = string.Empty;
        Constraint = string.Empty;
        Guidance = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ValidationException"/> class with a message.</summary>
    /// <param name="message">A message describing the validation failure.</param>
    public ValidationException(string message)
        : base(message)
    {
        Field = string.Empty;
        Constraint = string.Empty;
        Guidance = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ValidationException"/> class with a message and inner exception.</summary>
    /// <param name="message">A message describing the validation failure.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Field = string.Empty;
        Constraint = string.Empty;
        Guidance = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ValidationException"/> class with field, constraint, and guidance.</summary>
    /// <param name="field">The name of the field that failed validation.</param>
    /// <param name="constraint">The constraint that was violated.</param>
    /// <param name="guidance">Human-readable guidance for resolving the validation failure.</param>
    public ValidationException(string field, string constraint, string guidance)
        : base($"Validation failed for field '{field}': {constraint}. {guidance}")
    {
        Field = field;
        Constraint = constraint;
        Guidance = guidance;
    }

    /// <summary>Gets the name of the field that failed validation.</summary>
    public string Field { get; }

    /// <summary>Gets the constraint that was violated.</summary>
    public string Constraint { get; }

    /// <summary>Gets human-readable guidance for resolving the failure.</summary>
    public string Guidance { get; }
}
