using Ferret.Core.Connectors;
using Ferret.Core.Documents;

namespace Ferret.Indexing.Tests.Fakes;

/// <summary>Test double for IParserDispatcher.</summary>
internal sealed class FakeParserDispatcher : IParserDispatcher
{
    private Func<AssetDescriptor, ParseResult<Document>>? _resultFactory;

    /// <summary>Configures the result factory.</summary>
    internal void SetResult(Func<AssetDescriptor, ParseResult<Document>> factory)
    {
        _resultFactory = factory;
    }

    /// <inheritdoc/>
    public ValueTask<ParseResult<Document>> DispatchAsync(
        Stream content,
        AssetDescriptor asset,
        CancellationToken ct = default)
    {
        var result = _resultFactory?.Invoke(asset)
            ?? ParseResult<Document>.Unsupported(asset.MediaType ?? "application/octet-stream");
        return ValueTask.FromResult(result);
    }
}
