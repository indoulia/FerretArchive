using Ferret.Core.Documents;
using Ferret.ParserPlatform.Tests.Fakes;

namespace Ferret.ParserPlatform.Tests;

public sealed class ParserRegistryBuilderTests
{
    [Fact]
    public void Build_Throws_On_Duplicate_ParserId()
    {
        var a = new FakeContentParser("text/plain", priority: 100);
        var b = new FakeContentParser("text/plain", priority: 200);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ParserRegistryBuilder.Build([a, b]));

        Assert.Contains("text/plain", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_Throws_On_Duplicate_MediaType_Priority_Combination()
    {
        var colliding = new CollidingFakeParser("text/md-a", "text/shared", 200);
        var colliding2 = new CollidingFakeParser("text/md-b", "text/shared", 200);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ParserRegistryBuilder.Build([colliding, colliding2]));

        Assert.Contains("text/shared", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_Succeeds_With_Same_MediaType_Different_Priority()
    {
        var low = new FakeContentParser("text/plain", priority: 100);
        var highOverride = new CollidingFakeParser("text/plain-override", "text/plain", 200);

        var registry = ParserRegistryBuilder.Build([low, highOverride]);

        Assert.Equal(2, registry.GetAll().Count);
    }

    [Fact]
    public void Build_With_Empty_Collection_Succeeds()
    {
        var registry = ParserRegistryBuilder.Build([]);

        Assert.Empty(registry.GetAll());
    }

    /// <summary>Parser with a distinct ParserId but a configurable CanParse media type and priority.</summary>
    private sealed class CollidingFakeParser : IContentParser
    {
        private readonly string _canParseMediaType;

        internal CollidingFakeParser(string parserId, string canParseMediaType, int priority)
        {
            _canParseMediaType = canParseMediaType;
            Descriptor = new ParserDescriptor
            {
                Id = new ParserId(parserId),
                Name = $"{parserId} parser",
                Version = "1.0",
                SupportedMediaTypes = [canParseMediaType],
                Capabilities = [ParserCapabilities.PlainTextExtraction],
                Priority = priority,
            };
        }

        public ParserDescriptor Descriptor { get; }

        public bool CanParse(string mediaType) =>
            mediaType.Equals(_canParseMediaType, StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct) =>
            throw new NotImplementedException();
    }
}
