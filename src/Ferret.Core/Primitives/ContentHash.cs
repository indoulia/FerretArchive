namespace Ferret.Core.Primitives;

/// <summary>Represents the cryptographic hash of content, identified by algorithm and hex digest.</summary>
public sealed class ContentHash : IEquatable<ContentHash>
{
    private ContentHash(string algorithm, string hex)
    {
        Algorithm = algorithm;
        Hex = hex;
    }

    /// <summary>Gets the name of the hashing algorithm (e.g. "sha256").</summary>
    public string Algorithm { get; }

    /// <summary>Gets the hexadecimal digest string.</summary>
    public string Hex { get; }

    /// <summary>Creates a new <see cref="ContentHash"/> instance.</summary>
    /// <param name="algorithm">The hashing algorithm name.</param>
    /// <param name="hex">The hexadecimal digest string.</param>
    /// <returns>A new <see cref="ContentHash"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="algorithm"/> or <paramref name="hex"/> is null or whitespace.</exception>
    public static ContentHash Create(string algorithm, string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithm);
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        return new ContentHash(algorithm, hex);
    }

    /// <inheritdoc/>
    public bool Equals(ContentHash? other) =>
        other is not null &&
        string.Equals(Algorithm, other.Algorithm, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(Hex, other.Hex, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ContentHash other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(
            Algorithm.ToUpperInvariant().GetHashCode(StringComparison.Ordinal),
            Hex.GetHashCode(StringComparison.Ordinal));

    /// <summary>Returns the hash in <c>algorithm:hex</c> format.</summary>
    /// <returns>A string representation of the content hash.</returns>
    public override string ToString() => $"{Algorithm}:{Hex}";
}
