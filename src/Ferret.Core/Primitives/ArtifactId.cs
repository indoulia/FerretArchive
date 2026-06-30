namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for an artifact.</summary>
public sealed class ArtifactId : IEquatable<ArtifactId>
{
    private ArtifactId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="ArtifactId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="ArtifactId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static ArtifactId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ArtifactId(value);
    }

    /// <inheritdoc/>
    public bool Equals(ArtifactId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ArtifactId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
