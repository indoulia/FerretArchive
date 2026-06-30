namespace Ferret.Core.Connectors;

/// <summary>
/// The aggregated result of a validation pass over one or more connector instances.
/// <see cref="IsValid"/> is true when no <see cref="ValidationSeverity.Error"/> issues are present.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>Gets all validation issues. May be empty.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];

    /// <summary>Gets a value indicating whether no error-severity issues are present.</summary>
    public bool IsValid => !Issues.Any(i => i.Severity == ValidationSeverity.Error);

    /// <summary>Creates a valid result with no issues.</summary>
    /// <returns>A new <see cref="ValidationResult"/> with no issues.</returns>
    public static ValidationResult Ok() => new();

    /// <summary>Creates a result with a single error-severity issue.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="instanceId">Optional instance ID this error relates to.</param>
    /// <returns>A new <see cref="ValidationResult"/> with one error issue.</returns>
    public static ValidationResult WithError(string message, string? instanceId = null)
        => new()
        {
            Issues =
            [
                new ValidationIssue
                {
                    Message = message,
                    Severity = ValidationSeverity.Error,
                    InstanceId = instanceId,
                },
            ],
        };

    /// <summary>Merges multiple <see cref="ValidationResult"/> instances into one.</summary>
    /// <param name="results">The results to merge.</param>
    /// <returns>A new <see cref="ValidationResult"/> with all issues from the input results.</returns>
    public static ValidationResult Combine(IEnumerable<ValidationResult> results)
        => new() { Issues = results.SelectMany(r => r.Issues).ToList() };
}
