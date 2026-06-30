namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for correlating operations across module boundaries.</summary>
public sealed class CorrelationId : IEquatable<CorrelationId>
{
    private CorrelationId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="CorrelationId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="CorrelationId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static CorrelationId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new CorrelationId(value);
    }

    /// <inheritdoc/>
    public bool Equals(CorrelationId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CorrelationId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
