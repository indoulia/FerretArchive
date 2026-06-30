namespace Ferret.Core.Connectors;

/// <summary>
/// Provides content retrieval for discovered assets. Separate from <see cref="IAssetSource"/> (discovery).
/// A connector implementing both <see cref="IAssetSource"/> and <see cref="IAssetReader"/> supports
/// the full discover-then-read pipeline. Connectors without <see cref="IAssetReader"/> are skipped
/// during content ingestion.
/// </summary>
public interface IAssetReader
{
    /// <summary>Opens a read-only stream for the asset's content. Caller owns disposal.</summary>
    /// <param name="asset">The asset whose content to open.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only stream positioned at the beginning of the asset's content.</returns>
    Task<Stream> OpenAsync(AssetDescriptor asset, CancellationToken ct = default);
}
