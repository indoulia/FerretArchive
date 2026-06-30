using Ferret.Core.Documents;

namespace Ferret.ParserPlatform.Tests.Fakes;

/// <summary>Test double for IContentParser. Used in registry and dispatcher tests.</summary>
internal sealed class FakeContentParser : IContentParser
{
    internal FakeContentParser(string mediaType, int priority = 100)
    {
        Descriptor = new ParserDescriptor
        {
            Id = new ParserId(mediaType),
            Name = $"{mediaType} parser",
            Version = "1.0",
            SupportedMediaTypes = [mediaType],
            Capabilities = [ParserCapabilities.PlainTextExtraction],
            Priority = priority,
        };
    }

    /// <summary>Gets the parser descriptor.</summary>
    public ParserDescriptor Descriptor { get; }

    /// <inheritdoc/>
    public bool CanParse(string mediaType) =>
        mediaType.Equals(Descriptor.SupportedMediaTypes[0], StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct) =>
        throw new NotImplementedException("FakeContentParser.ParseAsync not used in registry tests");
}
