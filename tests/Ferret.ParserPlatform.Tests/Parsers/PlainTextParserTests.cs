using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Parsers;

namespace Ferret.ParserPlatform.Tests.Parsers;

public sealed class PlainTextParserTests
{
    private static readonly AssetDescriptor TextAsset = new()
    {
        Id = AssetId.From(new Uri("filesystem:///src/hello.txt")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///src/hello.txt"),
        DisplayName = "hello.txt",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = "text/plain",
    };

    [Fact]
    public void CanParse_Returns_True_For_Text_MediaTypes()
    {
        var parser = new PlainTextParser();

        Assert.True(parser.CanParse("text/plain"));
        Assert.True(parser.CanParse("text/markdown"));
        Assert.True(parser.CanParse("text/x-csharp"));
        Assert.True(parser.CanParse("TEXT/PLAIN"));
    }

    [Fact]
    public void CanParse_Returns_False_For_Non_Text_MediaTypes()
    {
        var parser = new PlainTextParser();

        Assert.False(parser.CanParse("application/json"));
        Assert.False(parser.CanParse("application/octet-stream"));
        Assert.False(parser.CanParse("image/png"));
    }

    [Fact]
    public void Priority_Is_100()
    {
        Assert.Equal(100, PlainTextParser.PlainTextDescriptor.Priority);
    }

    [Fact]
    public async Task ParseAsync_Returns_Document_With_Full_Content()
    {
        var parser = new PlainTextParser();
        using var stream = MakeStream("Hello, world!");
        var context = ParseContext.For(TextAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal("Hello, world!", doc.PlainText);
        Assert.Equal("text/plain", doc.MediaType);
    }

    [Fact]
    public async Task ParseAsync_Sets_Kind_Code_For_CSharp()
    {
        var parser = new PlainTextParser();
        using var stream = MakeStream("public class Foo {}");
        var asset = AssetWith("text/x-csharp");
        var context = ParseContext.For(asset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Code, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Sets_Kind_Config_For_Yaml()
    {
        var parser = new PlainTextParser();
        using var stream = MakeStream("key: value");
        var asset = AssetWith("text/yaml");
        var context = ParseContext.For(asset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Config, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Sets_Kind_Data_For_Csv()
    {
        var parser = new PlainTextParser();
        using var stream = MakeStream("a,b,c");
        var asset = AssetWith("text/csv");
        var context = ParseContext.For(asset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Data, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Sets_Kind_Prose_For_Markdown()
    {
        var parser = new PlainTextParser();
        using var stream = MakeStream("# Title\nContent");
        var asset = AssetWith("text/markdown");
        var context = ParseContext.For(asset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Prose, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Sets_Kind_Unknown_For_Plain_Text()
    {
        var parser = new PlainTextParser();
        using var stream = MakeStream("some text");
        var context = ParseContext.For(TextAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Unknown, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Sets_SourceFingerprint_From_Asset()
    {
        var parser = new PlainTextParser();
        using var stream = MakeStream("content");
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 100);
        var asset = new AssetDescriptor
        {
            Id = AssetId.From(new Uri("filesystem:///src/fp.txt")),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///src/fp.txt"),
            DisplayName = "fp.txt",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = "text/plain",
            Fingerprint = fingerprint,
        };
        var context = ParseContext.For(asset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(fingerprint, doc.SourceFingerprint);
    }

    [Fact]
    public async Task ParseAsync_Derives_DocumentId_From_AssetId()
    {
        var parser = new PlainTextParser();
        using var stream = MakeStream("content");
        var context = ParseContext.For(TextAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(TextAsset.Id.Value, doc.Id.Value);
    }

    private static MemoryStream MakeStream(string content) =>
        new(System.Text.Encoding.UTF8.GetBytes(content));

    private static AssetDescriptor AssetWith(string mediaType) => new()
    {
        Id = AssetId.From(new Uri("filesystem:///src/file.txt")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///src/file.txt"),
        DisplayName = "file.txt",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = mediaType,
    };
}
