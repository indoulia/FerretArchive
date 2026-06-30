using Ferret.Core.Documents;

namespace Ferret.AI.Context;

/// <summary>
/// Removes low-quality documents from the expanded set before token budget is applied.
/// Three exclusion rules (all applied in order):
///   1. Empty or whitespace-only content
///   2. Content length under 50 characters after trimming
///   3. Content duplicate — same (length, first-200-chars) fingerprint already seen in this pass
/// Pure static function — no DI, no I/O, no state between calls.
/// </summary>
public static class ContentFilter
{
    private const int MinContentLength = 50;
    private const int FingerprintPrefixLength = 200;

    /// <summary>
    /// Filters <paramref name="documents"/>, returning only those that pass all quality rules.
    /// First occurrence of a content fingerprint wins; subsequent documents with the same fingerprint are dropped.
    /// </summary>
    /// <param name="documents">The expanded documents to filter.</param>
    /// <returns>A new list containing only the documents that passed all rules, in input order.</returns>
    public static IReadOnlyList<Document> Filter(IReadOnlyList<Document> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        if (documents.Count == 0)
        {
            return [];
        }

        var seenFingerprints = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<Document>(documents.Count);

        foreach (var doc in documents)
        {
            // Rule 1: empty or whitespace
            if (string.IsNullOrWhiteSpace(doc.PlainText))
            {
                continue;
            }

            var trimmed = doc.PlainText.Trim();

            // Rule 2: too small
            if (trimmed.Length <= MinContentLength)
            {
                continue;
            }

            // Rule 3: content duplicate
            var prefix = trimmed[..Math.Min(FingerprintPrefixLength, trimmed.Length)];
            var fingerprint = $"{trimmed.Length}:{prefix}";
            if (!seenFingerprints.Add(fingerprint))
            {
                continue;
            }

            result.Add(doc);
        }

        return result;
    }
}
