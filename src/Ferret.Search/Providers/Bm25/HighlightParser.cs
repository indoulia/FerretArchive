using System.Text;

using Ferret.Core.Search;

namespace Ferret.Search.Providers.Bm25;

/// <summary>
/// Converts an FTS5 <c>snippet()</c> output — containing sentinel characters marking match boundaries —
/// into a <see cref="HighlightedText"/> span list.
/// FTS5 snippet format: text + <c>char(2)</c> (STX) opens a match, <c>char(3)</c> (ETX) closes it.
/// </summary>
internal static class HighlightParser
{
    /// <summary>STX character used as open-match sentinel in FTS5 <c>snippet()</c> calls.</summary>
    internal const char MatchOpen = '\x02';

    /// <summary>ETX character used as close-match sentinel in FTS5 <c>snippet()</c> calls.</summary>
    internal const char MatchClose = '\x03';

    /// <summary>Parses a sentinel-delimited FTS5 snippet string into a <see cref="HighlightedText"/>.</summary>
    internal static HighlightedText Parse(string snippet)
    {
        if (snippet.Length == 0)
        {
            return new HighlightedText { Spans = [] };
        }

        var spans = new List<TextSpan>();
        var buffer = new StringBuilder();
        var inMatch = false;

        foreach (var ch in snippet)
        {
            if (ch == MatchOpen)
            {
                if (buffer.Length > 0)
                {
                    spans.Add(new TextSpan(buffer.ToString(), TextSpanKind.Normal));
                    buffer.Clear();
                }

                inMatch = true;
            }
            else if (ch == MatchClose)
            {
                if (buffer.Length > 0)
                {
                    spans.Add(new TextSpan(buffer.ToString(), TextSpanKind.Match));
                    buffer.Clear();
                }

                inMatch = false;
            }
            else
            {
                buffer.Append(ch);
            }
        }

        if (buffer.Length > 0)
        {
            spans.Add(new TextSpan(buffer.ToString(), inMatch ? TextSpanKind.Match : TextSpanKind.Normal));
        }

        return new HighlightedText { Spans = spans };
    }
}
