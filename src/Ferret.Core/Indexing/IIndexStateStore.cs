using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Core.Indexing;

/// <summary>Persists per-asset fingerprints for incremental indexing change detection.</summary>
public interface IIndexStateStore
{
    /// <summary>Returns the stored fingerprint for the asset, or null if not recorded.</summary>
    /// <param name="assetId">The asset whose fingerprint to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored <see cref="AssetFingerprint"/>, or null if no entry exists.</returns>
    ValueTask<AssetFingerprint?> GetFingerprintAsync(AssetId assetId, CancellationToken ct = default);

    /// <summary>Records or updates the fingerprint for an asset.</summary>
    /// <param name="assetId">The asset to record.</param>
    /// <param name="fingerprint">The fingerprint to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the fingerprint has been stored.</returns>
    Task SetFingerprintAsync(AssetId assetId, AssetFingerprint fingerprint, CancellationToken ct = default);

    /// <summary>Removes the state entry for an asset (called when the asset is deleted).</summary>
    /// <param name="assetId">The asset whose entry to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the entry has been removed.</returns>
    Task RemoveAsync(AssetId assetId, CancellationToken ct = default);

    /// <summary>Returns all asset IDs currently in the store.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only set of all known <see cref="AssetId"/> values.</returns>
    ValueTask<IReadOnlySet<AssetId>> GetAllKeysAsync(CancellationToken ct = default);

    /// <summary>Clears all stored state (called on ForceRebuild).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when all state has been cleared.</returns>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>Flushes in-memory state to the backing store.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the state has been flushed.</returns>
    Task SaveAsync(CancellationToken ct = default);
}
