using System.Text;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a minimal HTML document with meta tags and tables.</summary>
public sealed class HtmlRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".html";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        var sb = new StringBuilder();
        sb.Append("<html><head><title>").Append(doc.Title).Append("</title>");
        if (doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author))
        {
            sb.Append("<meta name=\"author\" content=\"").Append(author).Append("\">");
        }

        sb.Append("</head><body>");
        sb.Append("<h1>").Append(doc.Title).Append("</h1>");
        foreach (var block in doc.Blocks)
        {
            sb.Append("<p>").Append(block.Text).Append("</p>");
        }

        foreach (var t in doc.Tables)
        {
            sb.Append("<table><tr>");
            foreach (var h in t.Headers)
            {
                sb.Append("<th>").Append(h).Append("</th>");
            }

            sb.Append("</tr>");
            foreach (var row in t.Rows)
            {
                sb.Append("<tr>");
                foreach (var c in row)
                {
                    sb.Append("<td>").Append(c.Value).Append("</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</table>");
        }

        sb.Append("</body></html>");
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        output.Write(bytes, 0, bytes.Length);
    }
}
