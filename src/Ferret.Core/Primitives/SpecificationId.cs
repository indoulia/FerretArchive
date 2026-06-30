namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a specification.</summary>
public sealed class SpecificationId : IEquatable<SpecificationId>
{
    private SpecificationId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="SpecificationId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="SpecificationId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static SpecificationId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new SpecificationId(value);
    }

    /// <inheritdoc/>
    public bool Equals(SpecificationId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SpecificationId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
