using System.Text;

using Ferret.Benchmarks.Corpus;
using Ferret.Benchmarks.Corpus.Renderers;
using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Office;
using Ferret.Parsers.Pdf;

namespace Ferret.Benchmarks.Tests.Corpus;

public sealed class RendererTests
{
    // Blocks kept < 100 chars so the PDF renderer does not truncate relative to Markdown/DOCX.
    private static CorpusDocument SampleProse() => new(
        "Design Proposal 7",
        new Dictionary<string, string>(StringComparer.Ordinal) { [DocumentMetadata.Author] = "Alice" },
        [
            new CorpusBlock(CorpusBlockKind.Paragraph, "The indexing pipeline stores content."),
            new CorpusBlock(CorpusBlockKind.Paragraph, "Search latency improved after compression."),
        ],
        Tables: []);

    [Theory]
    [InlineData(".md")]
    [InlineData(".html")]
    [InlineData(".cs")]
    [InlineData(".json")]
    public void Text_Renderer_Emits_NonEmpty_File_Containing_Title(string ext)
    {
        IDocumentRenderer renderer = ext switch
        {
            ".md" => new MarkdownRenderer(),
            ".html" => new HtmlRenderer(),
            ".cs" => new CSharpRenderer(),
            _ => new JsonRenderer(),
        };
        using var ms = new MemoryStream();

        renderer.Render(SampleProse(), ms);

        var text = Encoding.UTF8.GetString(ms.ToArray());
        Assert.NotEmpty(text);

        // C# sanitizes the title into a type name; assert a title token survives in that form.
        var expected = ext == ".cs" ? "DesignProposal7" : "Design Proposal 7";
        Assert.Contains(expected, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Cross_Format_Renderers_Preserve_Content_Tokens()
    {
        var doc = SampleProse();
        var expected = new[] { "indexing", "pipeline", "content", "latency", "compression" };

        var mdText = ExtractedFrom(new MarkdownRenderer(), doc, null);
        var docxText = ExtractedFrom(new DocxRenderer(), doc, (s, a) => new WordParser(new ParserOptions()).ParseAsync(s, a));
        var pdfText = ExtractedFrom(new PdfRenderer(), doc, (s, a) => new PdfParser(new ParserOptions()).ParseAsync(s, a));

        foreach (var token in expected)
        {
            Assert.Contains(token, Normalize(mdText), StringComparison.OrdinalIgnoreCase);
            Assert.Contains(token, Normalize(docxText), StringComparison.OrdinalIgnoreCase);
            Assert.Contains(token, Normalize(pdfText), StringComparison.OrdinalIgnoreCase);
        }
    }

    // For MD (no parser), read the rendered bytes directly; for binaries, parse and take PlainText.
    private static string ExtractedFrom(
        IDocumentRenderer renderer, CorpusDocument doc, Func<Stream, ParseContext, ValueTask<Document>>? parse)
    {
        using var ms = new MemoryStream();
        renderer.Render(doc, ms);
        ms.Position = 0;
        if (parse is null)
        {
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        var uri = new Uri("filesystem:///sample" + renderer.Extension);
        var asset = new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("bench"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = "sample" + renderer.Extension,
            LastModified = DateTimeOffset.UnixEpoch,
            MediaType = "application/octet-stream",
        };
        return parse(ms, ParseContext.For(asset)).AsTask().GetAwaiter().GetResult().PlainText;
    }

    // Normalized semantic comparison: collapse runs of whitespace to single spaces (case-insensitive
    // containment is applied by the caller, so no case folding is done here).
    private static string Normalize(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
