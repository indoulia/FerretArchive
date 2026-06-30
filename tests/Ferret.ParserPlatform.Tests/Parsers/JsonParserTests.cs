using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Parsers;

namespace Ferret.ParserPlatform.Tests.Parsers;

public sealed class JsonParserTests
{
    private static readonly AssetDescriptor JsonAsset = new()
    {
        Id = AssetId.From(new Uri("filesystem:///src/config.json")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///src/config.json"),
        DisplayName = "config.json",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = "application/json",
    };

    [Fact]
    public void CanParse_Returns_True_For_ApplicationJson()
    {
        var parser = new JsonParser();

        Assert.True(parser.CanParse("application/json"));
        Assert.True(parser.CanParse("APPLICATION/JSON"));
    }

    [Fact]
    public void CanParse_Returns_False_For_Other_Types()
    {
        var parser = new JsonParser();

        Assert.False(parser.CanParse("text/plain"));
        Assert.False(parser.CanParse("text/json"));
    }

    [Fact]
    public void Priority_Is_200()
    {
        var parser = new JsonParser();

        Assert.Equal(200, parser.Descriptor.Priority);
    }

    [Fact]
    public async Task ParseAsync_Flattens_Simple_Object()
    {
        var parser = new JsonParser();
        var json = """{"name":"Alice","age":30}""";
        using var stream = MakeStream(json);
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Contains("age", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("30", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("name", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("Alice", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_Extracts_Name_As_Title()
    {
        var parser = new JsonParser();
        var json = """{"name":"My Package","version":"1.0"}""";
        using var stream = MakeStream(json);
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal("My Package", doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Extracts_Title_Field_When_No_Name()
    {
        var parser = new JsonParser();
        var json = """{"title":"My Title","description":"desc"}""";
        using var stream = MakeStream(json);
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal("My Title", doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Returns_Null_Title_When_No_Name_Or_Title()
    {
        var parser = new JsonParser();
        var json = """{"key":"value"}""";
        using var stream = MakeStream(json);
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Null(doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Flattens_Nested_Object()
    {
        var parser = new JsonParser();
        var json = """{"outer":{"inner":"value"}}""";
        using var stream = MakeStream(json);
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Contains("outer.inner", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("value", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_Flattens_Array()
    {
        var parser = new JsonParser();
        var json = """{"items":["a","b"]}""";
        using var stream = MakeStream(json);
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Contains("items[0]", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("items[1]", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_Sets_Kind_Data()
    {
        var parser = new JsonParser();
        using var stream = MakeStream("""{"key":"value"}""");
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Data, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Sets_MediaType_ApplicationJson()
    {
        var parser = new JsonParser();
        using var stream = MakeStream("""{"key":"value"}""");
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal("application/json", doc.MediaType);
    }

    [Fact]
    public async Task ParseAsync_Property_Order_Is_Lexicographic()
    {
        var parser = new JsonParser();
        var json = """{"z":"last","a":"first"}""";
        using var stream = MakeStream(json);
        var context = ParseContext.For(JsonAsset);

        var doc = await parser.ParseAsync(stream, context);

        var aIndex = doc.PlainText.IndexOf("a:", StringComparison.Ordinal);
        var zIndex = doc.PlainText.IndexOf("z:", StringComparison.Ordinal);

        // 'a' must appear before 'z' in the flattened output
        Assert.True(aIndex < zIndex);
    }

    [Fact]
    public async Task ParseAsync_Invalid_Json_Propagates_Exception()
    {
        var parser = new JsonParser();
        using var stream = MakeStream("not valid json {{{");
        var context = ParseContext.For(JsonAsset);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            parser.ParseAsync(stream, context).AsTask());
    }

    private static MemoryStream MakeStream(string content) =>
        new(System.Text.Encoding.UTF8.GetBytes(content));
}
