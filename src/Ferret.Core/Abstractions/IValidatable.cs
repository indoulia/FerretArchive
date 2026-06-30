using Ferret.Core.Results;

namespace Ferret.Core.Abstractions;

/// <summary>Allows a type to validate its own state and return structured failures.</summary>
public interface IValidatable
{
    /// <summary>Validates the current state of this instance.</summary>
    /// <returns>A <see cref="ValidationResult"/> describing any validation failures.</returns>
    ValidationResult Validate();
}
