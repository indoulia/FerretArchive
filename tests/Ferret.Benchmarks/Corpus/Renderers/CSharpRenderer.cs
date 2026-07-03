using System.Text;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a plausible C# source file.</summary>
public sealed class CSharpRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".cs";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        var sb = new StringBuilder();
        sb.AppendLine("namespace Generated;").AppendLine();
        if (doc.Metadata.TryGetValue(DocumentMetadata.Author, out var author))
        {
            sb.Append("// Author: ").AppendLine(author);
        }

        sb.Append("/// <summary>").Append(doc.Title).AppendLine("</summary>");
        sb.Append("public sealed class ").Append(Sanitize(doc.Title)).AppendLine();
        sb.AppendLine("{");
        foreach (var block in doc.Blocks)
        {
            sb.Append("    // ").AppendLine(block.Text);
        }

        sb.AppendLine("}");
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        output.Write(bytes, 0, bytes.Length);
    }

    private static string Sanitize(string title)
    {
        var chars = title.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "Doc" : new string(chars);
    }
}
