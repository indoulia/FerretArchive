namespace Ferret.Core.Documents;

/// <summary>
/// Reserved extension point for post-parse document normalization.
/// Pipeline position: Parser → Normalizer → Document.
/// Examples (future sprints): Unicode normalization, line-ending normalization,
/// whitespace cleanup, HTML entity decoding.
/// Not implemented in Sprint 9.
/// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
public interface IContentNormalizer
{
    // Sprint 10+:
    // ValueTask<Document> NormalizeAsync(Document document, CancellationToken ct = default);
}
#pragma warning restore CA1040
