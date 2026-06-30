using Ferret.Core.Connectors;
using Ferret.Core.Documents;

namespace Ferret.ParserPlatform;

/// <summary>
/// Routes parse requests to the highest-priority compatible <see cref="IContentParser"/>
/// based on <see cref="AssetDescriptor.MediaType"/>. Never throws — all failure modes
/// are expressed as <see cref="ParseResultKind"/> values.
/// OperationCanceledException is the only exception that propagates.
/// </summary>
public sealed class ParserDispatcher : IParserDispatcher
{
    private readonly IParserRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParserDispatcher"/> class.
    /// </summary>
    /// <param name="registry">The parser registry to dispatch against.</param>
    public ParserDispatcher(IParserRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc/>
    public async ValueTask<ParseResult<Document>> DispatchAsync(
        Stream content,
        AssetDescriptor asset,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(asset);

        var mediaType = asset.MediaType ?? "application/octet-stream";

        var parser = _registry.GetParserFor(mediaType);
        if (parser is null)
        {
            return ParseResult<Document>.Unsupported(mediaType);
        }

        if (content.CanSeek && content.Length == 0)
        {
            return ParseResult<Document>.Empty();
        }

        try
        {
            var document = await parser.ParseAsync(content, ParseContext.For(asset), ct)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(document.PlainText))
            {
                return ParseResult<Document>.Empty();
            }

            return ParseResult<Document>.Success(document);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // catch general exception — dispatcher contract requires it
        catch (Exception ex)
        {
            return ParseResult<Document>.Failed(ex.Message);
        }
#pragma warning restore CA1031
    }
}
