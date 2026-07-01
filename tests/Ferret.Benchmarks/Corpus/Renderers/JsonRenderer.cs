using System.Text.Json;

namespace Ferret.Benchmarks.Corpus.Renderers;

/// <summary>Renders a CorpusDocument as a JSON object with a metadata map.</summary>
public sealed class JsonRenderer : IDocumentRenderer
{
    /// <inheritdoc/>
    public string Extension => ".json";

    /// <inheritdoc/>
    public void Render(CorpusDocument doc, Stream output)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(output);

        // Deterministic: fixed property order; metadata keys emitted in ordinal-sorted order.
        using var writer = new Utf8JsonWriter(output);
        writer.WriteStartObject();
        writer.WriteString("title", doc.Title);

        writer.WriteStartObject("metadata");
        foreach (var kv in doc.Metadata.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            writer.WriteString(kv.Key, kv.Value);
        }

        writer.WriteEndObject();

        writer.WriteStartArray("blocks");
        foreach (var block in doc.Blocks)
        {
            writer.WriteStringValue(block.Text);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
    }
}
