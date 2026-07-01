namespace Ferret.Benchmarks.Corpus;

/// <summary>Renders a logical <see cref="CorpusDocument"/> into a concrete file format.
/// This interface is the sole extension seam for new formats (XML, YAML, PPTX, CSV, logs, …):
/// add a renderer, no change to the model or generator core.</summary>
public interface IDocumentRenderer
{
    /// <summary>Gets the file extension this renderer produces, including the leading dot.</summary>
    string Extension { get; }

    /// <summary>Renders the document to the output stream. Must be deterministic for a given input.</summary>
    /// <param name="doc">The logical document.</param>
    /// <param name="output">The destination stream.</param>
    void Render(CorpusDocument doc, Stream output);
}
