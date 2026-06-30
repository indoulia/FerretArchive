using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.ParserPlatform.Parsers;

namespace Ferret.ParserPlatform.Tests.Parsers;

public sealed class MarkdownParserTests
{
    private static readonly AssetDescriptor MarkdownAsset = new()
    {
        Id = AssetId.From(new Uri("filesystem:///docs/readme.md")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///docs/readme.md"),
        DisplayName = "readme.md",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = "text/markdown",
    };

    [Fact]
    public void CanParse_Returns_True_For_TextMarkdown()
    {
        var parser = new MarkdownParser();

        Assert.True(parser.CanParse("text/markdown"));
        Assert.True(parser.CanParse("TEXT/MARKDOWN"));
    }

    [Fact]
    public void CanParse_Returns_False_For_Other_Types()
    {
        var parser = new MarkdownParser();

        Assert.False(parser.CanParse("text/plain"));
        Assert.False(parser.CanParse("application/json"));
    }

    [Fact]
    public void Priority_Is_Higher_Than_PlainTextParser()
    {
        var parser = new MarkdownParser();

        Assert.True(parser.Descriptor.Priority > PlainTextParser.PlainTextDescriptor.Priority);
    }

    [Fact]
    public void Priority_Is_200()
    {
        var parser = new MarkdownParser();

        Assert.Equal(200, parser.Descriptor.Priority);
    }

    [Fact]
    public async Task ParseAsync_Extracts_Title_From_H1()
    {
        var parser = new MarkdownParser();
        var content = "# My Document\n\nSome content here.";
        using var stream = MakeStream(content);
        var context = ParseContext.For(MarkdownAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal("My Document", doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Returns_Null_Title_When_No_H1()
    {
        var parser = new MarkdownParser();
        var content = "## Section One\n\nSome content.";
        using var stream = MakeStream(content);
        var context = ParseContext.For(MarkdownAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Null(doc.Title);
    }

    [Fact]
    public async Task ParseAsync_Extracts_H2_Sections()
    {
        var parser = new MarkdownParser();
        var content = "# Title\n\n## Section One\n\nContent.\n\n## Section Two\n\nMore.";
        using var stream = MakeStream(content);
        var context = ParseContext.For(MarkdownAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(2, doc.Sections.Count);
        Assert.Equal("Section One", doc.Sections[0].Title);
        Assert.Equal("Section Two", doc.Sections[1].Title);
    }

    [Fact]
    public async Task ParseAsync_Strips_Bold_From_PlainText()
    {
        var parser = new MarkdownParser();
        var content = "This is **bold** text.";
        using var stream = MakeStream(content);
        var context = ParseContext.For(MarkdownAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Contains("bold", doc.PlainText, StringComparison.Ordinal);
        Assert.DoesNotContain("**", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_Strips_Links_Keeping_Text()
    {
        var parser = new MarkdownParser();
        var content = "See [the docs](https://example.com) for more.";
        using var stream = MakeStream(content);
        var context = ParseContext.For(MarkdownAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Contains("the docs", doc.PlainText, StringComparison.Ordinal);
        Assert.DoesNotContain("https://example.com", doc.PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseAsync_Sets_Kind_Prose()
    {
        var parser = new MarkdownParser();
        using var stream = MakeStream("# Hello\n\nWorld.");
        var context = ParseContext.For(MarkdownAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(DocumentKind.Prose, doc.Kind);
    }

    [Fact]
    public async Task ParseAsync_Sets_MediaType_TextMarkdown()
    {
        var parser = new MarkdownParser();
        using var stream = MakeStream("# Hello");
        var context = ParseContext.For(MarkdownAsset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal("text/markdown", doc.MediaType);
    }

    [Fact]
    public async Task ParseAsync_Sets_SourceFingerprint()
    {
        var parser = new MarkdownParser();
        using var stream = MakeStream("# Hello");
        var fingerprint = AssetFingerprint.CreateLightweight(DateTimeOffset.UtcNow, 50);
        var asset = new AssetDescriptor
        {
            Id = AssetId.From(new Uri("filesystem:///docs/fp.md")),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("test"),
            Kind = AssetKind.File,
            CanonicalUri = new Uri("filesystem:///docs/fp.md"),
            DisplayName = "fp.md",
            LastModified = DateTimeOffset.UtcNow,
            MediaType = "text/markdown",
            Fingerprint = fingerprint,
        };
        var context = ParseContext.For(asset);

        var doc = await parser.ParseAsync(stream, context);

        Assert.Equal(fingerprint, doc.SourceFingerprint);
    }

    private static MemoryStream MakeStream(string content) =>
        new(System.Text.Encoding.UTF8.GetBytes(content));
}
