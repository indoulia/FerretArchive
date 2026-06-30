namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a workspace.</summary>
public sealed class WorkspaceId : IEquatable<WorkspaceId>
{
    private WorkspaceId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="WorkspaceId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="WorkspaceId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static WorkspaceId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new WorkspaceId(value);
    }

    /// <inheritdoc/>
    public bool Equals(WorkspaceId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is WorkspaceId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
