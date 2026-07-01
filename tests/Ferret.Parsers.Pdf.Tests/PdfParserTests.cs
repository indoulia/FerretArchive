using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Pdf;

using UglyToad.PdfPig.Writer;

namespace Ferret.Parsers.Pdf.Tests;

public sealed class PdfParserTests
{
    private static AssetDescriptor Asset(string name) => new()
    {
        Id = AssetId.From(new Uri($"filesystem:///{name}")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri($"filesystem:///{name}"),
        DisplayName = name,
        LastModified = DateTimeOffset.UtcNow,
        MediaType = "application/pdf",
    };

    // Builds a one-page PDF containing the given text using PdfPig's writer.
    // PdfDocumentBuilder is IDisposable in PdfPig 1.7.0-custom-5 (was not in the 0.1.x API the plan targeted).
    private static MemoryStream MakePdf(string text)
    {
        using var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        page.AddText(text, 12, new UglyToad.PdfPig.Core.PdfPoint(25, 800), font);
        return new MemoryStream(builder.Build());
    }

    [Fact]
    public void CanParse_True_For_ApplicationPdf_Only()
    {
        var parser = new PdfParser(new ParserOptions());
        Assert.True(parser.CanParse("application/pdf"));
        Assert.False(parser.CanParse("text/plain"));
        Assert.False(parser.CanParse("application/octet-stream"));
    }

    [Fact]
    public async Task ParseAsync_Extracts_Text()
    {
        var parser = new PdfParser(new ParserOptions());
        using var stream = MakePdf("Hello enterprise document");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.Contains("Hello", doc.PlainText, StringComparison.Ordinal);
        Assert.Equal(DocumentKind.Prose, doc.Kind);
        Assert.Equal("application/pdf", doc.MediaType);
    }

    [Fact]
    public async Task ParseAsync_Sets_PageCount_Metadata()
    {
        var parser = new PdfParser(new ParserOptions());
        using var stream = MakePdf("page one text");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.Equal("1", doc.Metadata[DocumentMetadata.PageCount]);
    }

    [Fact]
    public async Task ParseAsync_Does_Not_Dispose_Stream()
    {
        var parser = new PdfParser(new ParserOptions());
        using var stream = MakePdf("x");

        await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.True(stream.CanRead); // not disposed
    }

    [Fact]
    public async Task ParseAsync_Honors_Configured_Extraction_Limit()
    {
        var parser = new PdfParser(new ParserOptions { MaxExtractedCharacters = 10 });
        using var stream = MakePdf("This is a fairly long line of extracted PDF text well beyond ten characters.");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")));

        Assert.True(doc.PlainText.Length <= 10);
        Assert.Equal("true", doc.Metadata[DocumentMetadata.Truncated]);
    }

    [Fact]
    public async Task ParseAsync_Throws_When_Token_Already_Cancelled()
    {
        var parser = new PdfParser(new ParserOptions());
        using var stream = MakePdf("content");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await parser.ParseAsync(stream, ParseContext.For(Asset("a.pdf")), cts.Token));
    }
}
