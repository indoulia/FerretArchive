namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a single execution or run.</summary>
public sealed class ExecutionId : IEquatable<ExecutionId>
{
    private ExecutionId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="ExecutionId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="ExecutionId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static ExecutionId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ExecutionId(value);
    }

    /// <inheritdoc/>
    public bool Equals(ExecutionId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ExecutionId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
