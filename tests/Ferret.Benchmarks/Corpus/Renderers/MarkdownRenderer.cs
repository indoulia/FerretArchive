using System.Text;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as Markdown, including an author line and pipe tables.</summary>
public sealed class MarkdownRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".md";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        var sb = new StringBuilder();
        sb.Append("# ").AppendLine(doc.Title);
        if (doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author))
        {
            sb.Append("> Author: ").AppendLine(author);
        }

        foreach (var block in doc.Blocks)
        {
            switch (block.Kind)
            {
                case CorpusBlockKind.Heading: sb.Append("## ").AppendLine(block.Text); break;
                case CorpusBlockKind.CodeLine: sb.Append("    ").AppendLine(block.Text); break;
                case CorpusBlockKind.KeyValue: sb.Append("- ").AppendLine(block.Text); break;
                default: sb.AppendLine(block.Text); break;
            }
        }

        foreach (var t in doc.Tables)
        {
            sb.Append("| ").Append(string.Join(" | ", t.Headers)).AppendLine(" |");
            sb.Append("| ").Append(string.Join(" | ", t.Headers.Select(_ => "---"))).AppendLine(" |");
            foreach (var row in t.Rows)
            {
                sb.Append("| ").Append(string.Join(" | ", row.Select(c => c.Value))).AppendLine(" |");
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        output.Write(bytes, 0, bytes.Length);
    }
}
