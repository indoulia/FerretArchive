namespace Ferret.Benchmarks.Corpus;

/// <summary>The semantic role of a block within a logical corpus document.</summary>
public enum CorpusBlockKind
{
    /// <summary>A section heading.</summary>
    Heading,

    /// <summary>A prose paragraph.</summary>
    Paragraph,

    /// <summary>A line of source code.</summary>
    CodeLine,

    /// <summary>A key/value pair.</summary>
    KeyValue,
}

/// <summary>A single format-agnostic content block.</summary>
/// <param name="Kind">The block role.</param>
/// <param name="Text">The block text.</param>
public sealed record CorpusBlock(CorpusBlockKind Kind, string Text);

/// <summary>A format-agnostic table: a header row plus typed data rows. Rendered as a Markdown pipe
/// table, an HTML table, a Word table, or an Excel sheet by the respective renderer.</summary>
/// <param name="Headers">The header row.</param>
/// <param name="Rows">The typed data rows.</param>
public sealed record CorpusTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<CorpusCell>> Rows);

/// <summary>A logical, format-agnostic document. Renderers turn it into concrete file bytes.
/// <see cref="Metadata"/> uses <c>Ferret.Core.Documents.DocumentMetadata</c> keys so every renderer
/// and parser exercises the same metadata schema. A document may carry prose blocks, tables, or both.</summary>
/// <param name="Title">The document title.</param>
/// <param name="Metadata">Metadata keyed by <c>DocumentMetadata</c> constants.</param>
/// <param name="Blocks">The prose content blocks.</param>
/// <param name="Tables">The tabular content.</param>
public sealed record CorpusDocument(
    string Title,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<CorpusBlock> Blocks,
    IReadOnlyList<CorpusTable> Tables);
