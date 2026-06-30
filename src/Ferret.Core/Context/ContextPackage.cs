using System.Globalization;
using System.Text;

namespace Ferret.Core.Context;

/// <summary>
/// The assembled context package — the output of <see cref="IContextAssembler.AssembleAsync"/>.
/// Contains deduplicated, token-budgeted documents and provides <see cref="ToPromptString"/> for prompt injection.
/// </summary>
public sealed record ContextPackage
{
    /// <summary>Gets the original query used to assemble this package.</summary>
    public required string Query { get; init; }

    /// <summary>Gets the included documents, ordered by descending relevance score.</summary>
    public required IReadOnlyList<ContextDocument> Documents { get; init; }

    /// <summary>Gets the approximate total token count across all included documents.</summary>
    public required int TotalTokenEstimate { get; init; }

    /// <summary>Gets the total search hits considered before token budget was applied.</summary>
    public required int DocumentsConsidered { get; init; }

    /// <summary>Gets the number of documents included after deduplication and token budget.</summary>
    public required int DocumentsIncluded { get; init; }

    /// <summary>Gets the UTC timestamp when this package was assembled.</summary>
    public required DateTimeOffset AssembledAt { get; init; }

    /// <summary>
    /// Renders the context package as a formatted string ready for injection into an AI prompt.
    /// Format:
    ///   # Context for: "{query}"
    ///   (empty line)
    ///   ## [N] {display_name} (score: {score:F3})
    ///   {content}
    ///   (empty line between documents).
    /// </summary>
    /// <returns>A formatted string suitable for prompt injection.</returns>
    public string ToPromptString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            CultureInfo.InvariantCulture,
            $"# Context for: \"{Query}\"");

        if (Documents.Count == 0)
        {
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine();

        for (var i = 0; i < Documents.Count; i++)
        {
            var doc = Documents[i];
            var label = doc.Title is not null
                ? $"{doc.DisplayName} — {doc.Title}"
                : doc.DisplayName;

            sb.AppendLine(
                CultureInfo.InvariantCulture,
                $"## [{i + 1}] {label} (score: {doc.Score:F3})");
            sb.AppendLine();
            sb.AppendLine(doc.Content);

            if (i < Documents.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }
}
