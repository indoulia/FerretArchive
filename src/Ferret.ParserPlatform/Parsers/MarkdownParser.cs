using System.Text.RegularExpressions;

using Ferret.Core.Documents;
using Ferret.Core.Primitives;

namespace Ferret.ParserPlatform.Parsers;

/// <summary>
/// Content parser for <c>text/markdown</c>. Priority 200 — higher than PlainTextParser (100).
/// Strips Markdown syntax using Regex to produce a plain-text approximation for FTS5 indexing.
/// Section extraction: H2 headings become DocumentSection entries.
/// </summary>
public sealed class MarkdownParser : IContentParser
{
    private static readonly Regex Images =
        new(@"!\[.*?\]\(.*?\)", RegexOptions.Compiled);

    private static readonly Regex Links =
        new(@"\[([^\]]+)\]\([^\)]*\)", RegexOptions.Compiled);

    private static readonly Regex CodeFenceOpen =
        new(@"^```[^\n]*\n?", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex CodeFenceClose =
        new(@"^```\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex InlineCode =
        new(@"`([^`]*)`", RegexOptions.Compiled);

    private static readonly Regex Bold =
        new(@"\*\*([^*]+)\*\*|__([^_]+)__", RegexOptions.Compiled);

    private static readonly Regex Italic =
        new(@"\*([^*]+)\*|_([^_]+)_", RegexOptions.Compiled);

    private static readonly Regex Headings =
        new(@"^#+\s*(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex HtmlTags =
        new(@"<[^>]+>", RegexOptions.Compiled);

    private static readonly Regex HRules =
        new(@"^[-*_]{3,}\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex H1 =
        new(@"^#\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex H2 =
        new(@"^##\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly ParserDescriptor MarkdownDescriptor = new()
    {
        Id = new ParserId("text/markdown"),
        Name = "Markdown Parser",
        Version = "1.0",
        SupportedMediaTypes = ["text/markdown"],
        Capabilities = [ParserCapabilities.PlainTextExtraction, ParserCapabilities.SectionExtraction],
        Priority = 200,
    };

    /// <inheritdoc/>
    public ParserDescriptor Descriptor => MarkdownDescriptor;

    /// <inheritdoc/>
    public bool CanParse(string mediaType)
    {
        ArgumentNullException.ThrowIfNull(mediaType);
        return mediaType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async ValueTask<Document> ParseAsync(
        Stream content,
        ParseContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(context);

        using var reader = new StreamReader(
            content,
            System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var raw = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var title = ExtractTitle(raw);
        var sections = ExtractSections(raw);
        var plainText = StripMarkdown(raw);

        return new Document
        {
            Id = DocumentId.From(context.Asset.Id),
            SourceAssetId = context.Asset.Id,
            ConnectorId = context.Asset.ConnectorId,
            InstanceId = context.Asset.InstanceId,
            MediaType = "text/markdown",
            Kind = DocumentKind.Prose,
            PlainText = plainText,
            Title = title,
            Sections = sections,
            ProducedAt = DateTimeOffset.UtcNow,
            SourceFingerprint = context.Asset.Fingerprint,
        };
    }

    private static string? ExtractTitle(string raw)
    {
        var m = H1.Match(raw);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static List<DocumentSection> ExtractSections(string raw)
    {
        var lines = raw.Split('\n');
        var sections = new List<DocumentSection>();

        for (var i = 0; i < lines.Length; i++)
        {
            var m = H2.Match(lines[i]);
            if (m.Success)
            {
                sections.Add(new DocumentSection(m.Groups[1].Value.Trim(), string.Empty, i + 1, i + 1));
            }
        }

        return sections;
    }

    private static string StripMarkdown(string raw)
    {
        var text = raw;
        text = Images.Replace(text, string.Empty);
        text = Links.Replace(text, "$1");
        text = CodeFenceOpen.Replace(text, string.Empty);
        text = CodeFenceClose.Replace(text, string.Empty);
        text = InlineCode.Replace(text, "$1");
        text = Bold.Replace(text, m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
        text = Italic.Replace(text, m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
        text = Headings.Replace(text, "$1");
        text = HtmlTags.Replace(text, string.Empty);
        text = HRules.Replace(text, string.Empty);
        return text.Trim();
    }
}
