using Ferret.Core.Enumerations;

namespace Ferret.Core.Results;

/// <summary>Describes a single validation failure for a field or constraint.</summary>
public sealed class ValidationFailure
{
    /// <summary>Initializes a new instance of the <see cref="ValidationFailure"/> class.</summary>
    /// <param name="field">The name of the field that failed validation.</param>
    /// <param name="constraint">The constraint that was violated.</param>
    /// <param name="guidance">Human-readable guidance for resolving the failure.</param>
    /// <param name="severity">The severity of the failure.</param>
    public ValidationFailure(string field, string constraint, string guidance, ValidationSeverity severity)
    {
        Field = field;
        Constraint = constraint;
        Guidance = guidance;
        Severity = severity;
    }

    /// <summary>Gets the name of the field that failed validation.</summary>
    public string Field { get; }

    /// <summary>Gets the constraint that was violated.</summary>
    public string Constraint { get; }

    /// <summary>Gets human-readable guidance for resolving the failure.</summary>
    public string Guidance { get; }

    /// <summary>Gets the severity of the validation failure.</summary>
    public ValidationSeverity Severity { get; }
}
