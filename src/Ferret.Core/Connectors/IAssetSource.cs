namespace Ferret.Core.Connectors;

/// <summary>
/// A connector capability that discovers assets from a source.
/// Implementors MUST stream — never buffer into List before yielding.
/// </summary>
public interface IAssetSource
{
    /// <summary>Discovers assets, streaming results as they are found.</summary>
    /// <param name="options">Options controlling discovery behaviour (ignore policy, etc.).</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async stream of discovered assets. Memory usage is O(batch), not O(corpus).</returns>
    IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        AssetDiscoveryOptions options,
        CancellationToken ct = default);
}
