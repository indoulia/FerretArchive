using Ferret.Core.Connectors;

namespace Ferret.Core.Primitives;

/// <summary>Strongly-typed identifier for a document.</summary>
public sealed class DocumentId : IEquatable<DocumentId>
{
    private DocumentId(string value) => Value = value;

    /// <summary>Gets the raw string value of this identifier.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="DocumentId"/> from a non-empty string.</summary>
    /// <param name="value">The raw identifier value.</param>
    /// <returns>A new <see cref="DocumentId"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null or whitespace.</exception>
    public static DocumentId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new DocumentId(value);
    }

    /// <summary>Derives a deterministic <see cref="DocumentId"/> from the source <see cref="AssetId"/>.
    /// The resulting DocumentId equals the AssetId value — one asset produces one document.</summary>
    /// <param name="assetId">The source asset identifier.</param>
    /// <returns>A deterministic <see cref="DocumentId"/>.</returns>
    public static DocumentId From(AssetId assetId)
    {
        ArgumentNullException.ThrowIfNull(assetId);
        return Create(assetId.Value);
    }

    /// <inheritdoc/>
    public bool Equals(DocumentId? other) => other is not null && Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DocumentId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
