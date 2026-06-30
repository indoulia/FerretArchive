namespace Ferret.Core.Connectors;

/// <summary>Strongly-typed identifier for an asset, derived from its CanonicalUri.</summary>
/// <param name="Value">The canonical URI string.</param>
public sealed record AssetId(string Value)
{
    /// <summary>Derives an <see cref="AssetId"/> from a canonical URI.</summary>
    /// <param name="canonicalUri">The asset's canonical URI.</param>
    /// <returns>A deterministic <see cref="AssetId"/>.</returns>
    public static AssetId From(Uri canonicalUri)
    {
        ArgumentNullException.ThrowIfNull(canonicalUri);
        return new(canonicalUri.ToString());
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
