using Ferret.Core.Documents;
using Ferret.ParserPlatform.Tests.Fakes;

namespace Ferret.ParserPlatform.Tests;

public sealed class ParserRegistryTests
{
    [Fact]
    public void GetAll_Returns_Descriptors_In_Priority_Descending_Order()
    {
        var low = new FakeContentParser("text/low", priority: 100);
        var high = new FakeContentParser("text/high", priority: 200);
        var registry = ParserRegistryBuilder.Build([low, high]);

        var all = registry.GetAll();

        Assert.Equal(200, all[0].Priority);
        Assert.Equal(100, all[1].Priority);
    }

    [Fact]
    public void GetById_Returns_Correct_Descriptor()
    {
        var parser = new FakeContentParser("text/plain");
        var registry = ParserRegistryBuilder.Build([parser]);

        var result = registry.GetById(new ParserId("text/plain"));

        Assert.NotNull(result);
        Assert.Equal("text/plain", result.Id.Value);
    }

    [Fact]
    public void GetById_Returns_Null_For_Unknown_Id()
    {
        var registry = ParserRegistryBuilder.Build([new FakeContentParser("text/plain")]);

        Assert.Null(registry.GetById(new ParserId("text/markdown")));
    }

    [Fact]
    public void GetParserFor_Returns_Highest_Priority_Compatible_Parser()
    {
        // Two parsers that both CanParse "text/plain" but have different ParserIds
        var low = new FakeContentParser("text/plain", priority: 100);
        var high = new HighPriorityTextPlainParser();
        var registry = ParserRegistryBuilder.Build([low, high]);

        var result = registry.GetParserFor("text/plain");

        Assert.NotNull(result);
        Assert.Equal(200, result.Descriptor.Priority);
    }

    [Fact]
    public void GetParserFor_Returns_Null_When_No_Parser_Matches()
    {
        var registry = ParserRegistryBuilder.Build([new FakeContentParser("text/plain")]);

        Assert.Null(registry.GetParserFor("application/json"));
    }

    [Fact]
    public void GetParserFor_Is_Case_Insensitive()
    {
        var registry = ParserRegistryBuilder.Build([new FakeContentParser("text/plain")]);

        Assert.NotNull(registry.GetParserFor("TEXT/PLAIN"));
    }

    [Fact]
    public void Empty_Registry_Returns_Null_From_GetParserFor()
    {
        var registry = ParserRegistryBuilder.Build([]);

        Assert.Null(registry.GetParserFor("text/plain"));
    }

    [Fact]
    public void Empty_Registry_Returns_Empty_From_GetAll()
    {
        var registry = ParserRegistryBuilder.Build([]);

        Assert.Empty(registry.GetAll());
    }

    private sealed class HighPriorityTextPlainParser : IContentParser
    {
        public ParserDescriptor Descriptor { get; } = new()
        {
            Id = new ParserId("text/plain-high"),
            Name = "High priority text/plain",
            Version = "1.0",
            SupportedMediaTypes = ["text/plain"],
            Capabilities = [ParserCapabilities.PlainTextExtraction],
            Priority = 200,
        };

        public bool CanParse(string mediaType) =>
            mediaType.Equals("text/plain", StringComparison.OrdinalIgnoreCase);

        public ValueTask<Document> ParseAsync(Stream content, ParseContext context, CancellationToken ct) =>
            throw new NotImplementedException();
    }
}
