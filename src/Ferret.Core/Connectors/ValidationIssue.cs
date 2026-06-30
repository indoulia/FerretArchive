namespace Ferret.Core.Connectors;

/// <summary>A single diagnostic issue produced by a validation pass.</summary>
public sealed record ValidationIssue
{
    /// <summary>Gets the human-readable description of the issue.</summary>
    public required string Message { get; init; }

    /// <summary>Gets the severity of this issue.</summary>
    public required ValidationSeverity Severity { get; init; }

    /// <summary>Gets the instance ID this issue relates to, or null if not instance-specific.</summary>
    public string? InstanceId { get; init; }
}
