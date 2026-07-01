using Ferret.Core.Documents;

using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a real PDF using PdfPig's writer (benchmark-only use).</summary>
public sealed class PdfRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".pdf";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        // PdfDocumentBuilder is IDisposable in PdfPig 1.7.0-custom-5 (was not in the 0.1.x API the plan targeted).
        using var builder = new PdfDocumentBuilder { ArchiveStandard = PdfAStandard.None };
        builder.DocumentInformation.Title = doc.Title;
        if (doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author))
        {
            builder.DocumentInformation.Author = author;
        }

        if (doc.Metadata.TryGetValue(DocumentMetadata.Subject, out var subject))
        {
            builder.DocumentInformation.Subject = subject;
        }

        if (doc.Metadata.TryGetValue(DocumentMetadata.Keywords, out var keywords))
        {
            builder.DocumentInformation.Keywords = keywords;
        }

        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(595, 842);

        var y = 800;
        page.AddText(doc.Title, 14, new PdfPoint(25, y), font);
        foreach (var block in doc.Blocks)
        {
            y -= 16;
            if (y < 40)
            {
                page = builder.AddPage(595, 842);
                y = 800;
            }

            page.AddText(Truncate(block.Text), 10, new PdfPoint(25, y), font);
        }

        var bytes = builder.Build();
        output.Write(bytes, 0, bytes.Length);
    }

    // 100-char per-line cap keeps generated text on one line. Cross-format equivalence tests must
    // therefore use blocks < 100 chars so PDF text is not truncated relative to MD/DOCX.
    private static string Truncate(string text) => text.Length <= 100 ? text : text[..100];
}
