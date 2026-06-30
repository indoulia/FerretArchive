using Ferret.Core.Connectors;
using Ferret.Core.Indexing;
using Ferret.Core.Primitives;

namespace Ferret.Indexing;

/// <summary>No-op state store — always reports no stored fingerprints.
/// Used in tests and when incremental indexing is disabled.</summary>
public sealed class NullIndexStateStore : IIndexStateStore
{
    /// <inheritdoc/>
    public ValueTask<AssetFingerprint?> GetFingerprintAsync(AssetId assetId, CancellationToken ct = default) =>
        ValueTask.FromResult<AssetFingerprint?>(null);

    /// <inheritdoc/>
    public Task SetFingerprintAsync(AssetId assetId, AssetFingerprint fingerprint, CancellationToken ct = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task RemoveAsync(AssetId assetId, CancellationToken ct = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public ValueTask<IReadOnlySet<AssetId>> GetAllKeysAsync(CancellationToken ct = default) =>
        ValueTask.FromResult<IReadOnlySet<AssetId>>(new HashSet<AssetId>());

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken ct = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task SaveAsync(CancellationToken ct = default) =>
        Task.CompletedTask;
}
