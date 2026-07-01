using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Ferret.Core.Documents;

// Bare `Document` is ambiguous (Wordprocessing.Document vs Ferret.Core.Documents.Document);
// this renderer builds a Word document, so alias the simple name to the Wordprocessing type.
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a real .docx using OpenXml.</summary>
public sealed class DocxRenderer : IDocumentRenderer
{
    private static readonly DateTime FixedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc/>
    public string Extension => ".docx";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        using var word = WordprocessingDocument.Create(output, WordprocessingDocumentType.Document, autoSave: true);
        var main = word.AddMainDocumentPart();
        var body = new Body();
        body.Append(new Paragraph(new Run(new Text(doc.Title))));
        foreach (var block in doc.Blocks)
        {
            body.Append(new Paragraph(new Run(new Text(block.Text) { Space = SpaceProcessingModeValues.Preserve })));
        }

        foreach (var t in doc.Tables)
        {
            var table = new Table();
            table.Append(RowOf(t.Headers));
            foreach (var row in t.Rows)
            {
                table.Append(RowOf(row.Select(c => c.Value).ToList()));
            }

            body.Append(table);
        }

        main.Document = new Document(body);

        var props = word.PackageProperties;
        props.Title = doc.Title;
        props.Creator = Meta(doc, DocumentMetadata.Author) ?? "Synthetic Corpus Generator";
        props.Subject = Meta(doc, DocumentMetadata.Subject);
        props.Keywords = Meta(doc, DocumentMetadata.Keywords);
        props.Category = Meta(doc, DocumentMetadata.Category);
        props.Created = FixedTimestamp;   // pinned for determinism
        props.Modified = FixedTimestamp;
    }

    private static string? Meta(CorpusDocument doc, string key) =>
        doc.Metadata.TryGetValue(key, out var v) ? v : null;

    private static TableRow RowOf(IReadOnlyList<string> cells)
    {
        var row = new TableRow();
        foreach (var cell in cells)
        {
            row.Append(new TableCell(new Paragraph(new Run(new Text(cell) { Space = SpaceProcessingModeValues.Preserve }))));
        }

        return row;
    }
}
