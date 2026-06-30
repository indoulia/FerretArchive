using Ferret.Core.Connectors;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double implementing both IAssetSource and IAssetReader.</summary>
internal sealed class FakeAssetSourceReader : IAssetSource, IAssetReader
{
    private readonly List<AssetDescriptor> _assets;
    private readonly Func<AssetDescriptor, Stream>? _streamFactory;

    internal FakeAssetSourceReader(
        IEnumerable<AssetDescriptor> assets,
        Func<AssetDescriptor, Stream>? streamFactory = null)
    {
        _assets = assets.ToList();
        _streamFactory = streamFactory;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<AssetDescriptor> DiscoverAsync(
        AssetDiscoveryOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var asset in _assets)
        {
            ct.ThrowIfCancellationRequested();
            yield return asset;
            await Task.Yield();
        }
    }

    /// <inheritdoc/>
    public Task<Stream> OpenAsync(AssetDescriptor asset, CancellationToken ct = default)
    {
        var stream = _streamFactory?.Invoke(asset)
            ?? new MemoryStream(System.Text.Encoding.UTF8.GetBytes("sample content"));
        return Task.FromResult(stream);
    }
}
