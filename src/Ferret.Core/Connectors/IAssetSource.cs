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

    /// <summary>Resolves a single asset by its canonical Id without a full discovery walk.
    /// Used by incremental callers (e.g. watch-mode reindexing) that already know which
    /// asset changed and must not pay an O(corpus) <see cref="DiscoverAsync"/> cost per change.
    /// The default implementation returns null (equivalent to "this source has nothing at that Id"),
    /// so adding this member is a non-breaking addition per ADR-0012 rule 2 — existing implementors
    /// do not need to change.</summary>
    /// <param name="assetId">The canonical Id of the asset to resolve.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The asset's current <see cref="AssetDescriptor"/>, or null if it does not exist,
    /// is excluded by this source's ignore policy, or this source does not support targeted lookup.</returns>
    Task<AssetDescriptor?> TryGetAsync(AssetId assetId, CancellationToken ct = default) =>
        Task.FromResult<AssetDescriptor?>(null);
}
