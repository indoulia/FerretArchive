using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Ferret.Core.Connectors;
using Ferret.Core.Documents;
using Ferret.Core.Primitives;
using Ferret.Parsers.Office;

// Bare `Document` is ambiguous (Wordprocessing.Document vs Ferret.Core.Documents.Document); the
// builders below construct Word documents, so alias the simple name to the Wordprocessing type.
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace Ferret.Parsers.Office.Tests;

public sealed class WordParserTests
{
    private static AssetDescriptor Asset() => new()
    {
        Id = AssetId.From(new Uri("filesystem:///doc.docx")),
        ConnectorId = new ConnectorId("filesystem"),
        InstanceId = new ConnectorInstanceId("test"),
        Kind = AssetKind.File,
        CanonicalUri = new Uri("filesystem:///doc.docx"),
        DisplayName = "doc.docx",
        LastModified = DateTimeOffset.UtcNow,
        MediaType = OfficeMediaTypes.Docx,
    };

    // Builds a minimal .docx with a body paragraph and a one-cell table.
    private static MemoryStream MakeDocx(string paragraphText, string cellText)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, autoSave: true))
        {
            var main = doc.AddMainDocumentPart();
            var body = new Body();
            body.Append(new Paragraph(new Run(new Text(paragraphText))));
            var table = new Table(new TableRow(new TableCell(new Paragraph(new Run(new Text(cellText))))));
            body.Append(table);
            main.Document = new Document(body);
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void CanParse_True_For_Docx_Only()
    {
        var parser = new WordParser(new ParserOptions());
        Assert.True(parser.CanParse(OfficeMediaTypes.Docx));
        Assert.False(parser.CanParse("application/pdf"));
        Assert.False(parser.CanParse("application/msword")); // legacy .doc unsupported
    }

    [Fact]
    public async Task ParseAsync_Extracts_Paragraph_And_Table_Text()
    {
        var parser = new WordParser(new ParserOptions());
        using var stream = MakeDocx("Quarterly objectives", "Revenue target");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Contains("Quarterly objectives", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("Revenue target", doc.PlainText, StringComparison.Ordinal);
        Assert.Equal(DocumentKind.Prose, doc.Kind);
        Assert.Equal(OfficeMediaTypes.Docx, doc.MediaType);
    }

    // Builds a .docx whose package properties carry known metadata.
    private static MemoryStream MakeDocxWithProps(string author, string subject)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, autoSave: true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text("body")))));
            doc.PackageProperties.Creator = author;
            doc.PackageProperties.Subject = subject;
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task ParseAsync_Extracts_Package_Metadata()
    {
        var parser = new WordParser(new ParserOptions());
        using var stream = MakeDocxWithProps(author: "Bob", subject: "Design Proposal");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Equal("Bob", doc.Metadata[DocumentMetadata.Author]);
        Assert.Equal("Design Proposal", doc.Metadata[DocumentMetadata.Subject]);
    }

    // Builds a .docx with a header, a body paragraph, and a footer, wired via section properties.
    private static MemoryStream MakeDocxWithHeaderFooter(string headerText, string bodyText, string footerText)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, autoSave: true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());

            var headerPart = main.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(new Paragraph(new Run(new Text(headerText))));
            var headerId = main.GetIdOfPart(headerPart);

            var footerPart = main.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(new Paragraph(new Run(new Text(footerText))));
            var footerId = main.GetIdOfPart(footerPart);

            var body = main.Document.Body!;
            body.Append(new Paragraph(new Run(new Text(bodyText))));
            body.Append(new SectionProperties(
                new HeaderReference { Type = HeaderFooterValues.Default, Id = headerId },
                new FooterReference { Type = HeaderFooterValues.Default, Id = footerId }));
        }

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task ParseAsync_Extracts_Header_And_Footer_Text()
    {
        var parser = new WordParser(new ParserOptions());
        using var stream = MakeDocxWithHeaderFooter("Confidential header", "Body content", "Page footer note");

        var doc = await parser.ParseAsync(stream, ParseContext.For(Asset()));

        Assert.Contains("Confidential header", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("Body content", doc.PlainText, StringComparison.Ordinal);
        Assert.Contains("Page footer note", doc.PlainText, StringComparison.Ordinal);
    }
}
