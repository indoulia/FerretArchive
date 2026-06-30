using System.Globalization;
using System.Text;
using System.Text.Json;

using Ferret.Core.Search;

namespace Ferret.Cli.Search;

/// <summary>
/// Routes <see cref="SearchViewModel"/> rendering to the appropriate format.
/// Text format: ANSI-highlighted hit list with snippet and footer.
/// JSON format: machine-readable JSON for scripting and tool integration.
/// </summary>
public sealed class SearchRendererSelector
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = true };

    private readonly ITextStyler _styler;

    /// <summary>Initializes a new instance of the <see cref="SearchRendererSelector"/> class.</summary>
    /// <param name="styler">The text styler to apply to terminal output.</param>
    public SearchRendererSelector(ITextStyler styler)
    {
        _styler = styler;
    }

    /// <summary>Renders the view model to a string in the requested format.</summary>
    /// <param name="viewModel">The search view model to render.</param>
    /// <param name="format">The output format.</param>
    /// <returns>A formatted string ready for terminal output.</returns>
    public string Render(SearchViewModel viewModel, SearchOutputFormat format)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        return format switch
        {
            SearchOutputFormat.Json => RenderJson(viewModel),
            _ => RenderText(viewModel),
        };
    }

    private static string RenderJson(SearchViewModel viewModel)
    {
        var hits = viewModel.Hits.Select(h => new
        {
            documentId = h.DocumentId.ToString(),
            displayName = h.DisplayName,
            canonicalUri = h.CanonicalUri.ToString(),
            score = h.Score,
            snippet = string.Concat(h.Snippet.Spans.Select(s => s.Text)),
        }).ToList();

        return JsonSerializer.Serialize(
            new
            {
                query = viewModel.OriginalQuery,
                total = viewModel.Hits.Count,
                hits,
            },
            JsonOptions);
    }

    private string RenderText(SearchViewModel viewModel)
    {
        var sb = new StringBuilder();

        if (viewModel.Hits.Count == 0)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "No results for \"{0}\".", viewModel.OriginalQuery));
            return sb.ToString();
        }

        foreach (var hit in viewModel.Hits)
        {
            sb.AppendLine(_styler.Muted(hit.DisplayName));

            foreach (var span in hit.Snippet.Spans)
            {
                sb.Append(span.Kind == TextSpanKind.Match
                    ? _styler.Match(span.Text)
                    : _styler.Normal(span.Text));
            }

            sb.AppendLine();
            sb.AppendLine();
        }

        var info = viewModel.ExecutionInfo;
        sb.Append(_styler.Muted(string.Format(
            CultureInfo.InvariantCulture,
            "{0} result(s) · {1} · {2:F0}ms",
            viewModel.Hits.Count,
            info.ProviderName,
            info.Duration.TotalMilliseconds)));

        return sb.ToString();
    }
}
