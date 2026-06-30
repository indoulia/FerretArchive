namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a review.</summary>
public sealed class ReviewId : IEquatable<ReviewId>
{
    private ReviewId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="ReviewId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="ReviewId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static ReviewId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ReviewId(value);
    }

    /// <inheritdoc/>
    public bool Equals(ReviewId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ReviewId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
