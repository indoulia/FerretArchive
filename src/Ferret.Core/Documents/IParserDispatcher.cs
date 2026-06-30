using Ferret.Core.Connectors;

namespace Ferret.Core.Documents;

/// <summary>
/// Dispatches parse requests to the appropriate <see cref="IContentParser"/> based on
/// <see cref="AssetDescriptor.MediaType"/>. Returns a <see cref="ParseResult{T}"/> —
/// the dispatcher never throws. All failure modes are explicit outcomes.
/// </summary>
public interface IParserDispatcher
{
    /// <summary>Selects the highest-priority compatible parser and parses the content stream.</summary>
    /// <param name="content">The raw content stream, positioned at the beginning.</param>
    /// <param name="asset">The source asset descriptor — MediaType drives parser selection.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="ParseResult{Document}"/> describing the outcome.</returns>
    ValueTask<ParseResult<Document>> DispatchAsync(
        Stream content,
        AssetDescriptor asset,
        CancellationToken ct = default);
}
