using Ferret.Cli.Diagnostics;
using Ferret.Cli.Tests.Infrastructure;
using Ferret.Core.Documents;
using Ferret.ParserPlatform.Parsers;
using Ferret.Parsers.Office;
using Ferret.Parsers.Pdf;

namespace Ferret.Cli.Tests.Diagnostics;

public sealed class ParserPlatformReportTests
{
    // Registration order: platform text parsers, then PDF, then Office (as ParserPackModule composes them).
    private static IReadOnlyList<IContentParser> AllParsers() =>
    [
        new PlainTextParser(),
        new MarkdownParser(),
        new JsonParser(),
        new CsvParser(new ParserOptions()),
        new PdfParser(new ParserOptions()),
        new WordParser(new ParserOptions()),
        new ExcelParser(new ParserOptions()),
    ];

    private static string Render(IReadOnlyList<IContentParser> parsers, bool verbose)
    {
        var formatter = new CapturingOutputFormatter();
        new ParserPlatformReport(parsers).Render(formatter, verbose);
        return formatter.Text;
    }

    [Fact]
    public void Default_HasAllSectionsAndCoverageTotals()
    {
        var text = Render(AllParsers(), verbose: false);

        Assert.Contains("Parser Platform", text, StringComparison.Ordinal);
        Assert.Contains("Installed Parsers (7)", text, StringComparison.Ordinal);
        Assert.Contains("Extension Coverage", text, StringComparison.Ordinal);
        Assert.Contains("Parseable Binary: 3", text, StringComparison.Ordinal);
        Assert.Contains("Known Extensions:", text, StringComparison.Ordinal);
        Assert.Contains(".docx", text, StringComparison.Ordinal);
        Assert.Contains("currently treated as opaque binary", text, StringComparison.Ordinal);
        Assert.Contains("Parser Packages", text, StringComparison.Ordinal);
        Assert.Contains("Ferret.Parsers.Pdf", text, StringComparison.Ordinal);
        Assert.Contains("Ferret.Parsers.Office", text, StringComparison.Ordinal);
        Assert.Contains("Ferret.ParserPlatform", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistrationOrder_IsStable_NotSorted()
    {
        var text = Render(AllParsers(), verbose: false);
        int[] positions =
        [
            text.IndexOf("Plain Text Parser", StringComparison.Ordinal),
            text.IndexOf("Markdown Parser", StringComparison.Ordinal),
            text.IndexOf("JSON Parser", StringComparison.Ordinal),
            text.IndexOf("CSV Parser", StringComparison.Ordinal),
            text.IndexOf("PDF Parser", StringComparison.Ordinal),
            text.IndexOf("Word (DOCX) Parser", StringComparison.Ordinal),
            text.IndexOf("Excel (XLSX) Parser", StringComparison.Ordinal),
        ];
        Assert.All(positions, p => Assert.True(p >= 0));
        var sorted = positions.OrderBy(x => x).ToArray();
        Assert.Equal(sorted, positions); // appear in registration order, not alphabetical
    }

    [Fact]
    public void Default_SummarizesOpaque_WithHint()
    {
        var text = Render(AllParsers(), verbose: false);
        Assert.Contains("run `ferret doctor --verbose` for the full list", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".zip", text, StringComparison.Ordinal); // sorts late, beyond the sample
    }

    [Fact]
    public void Verbose_ShowsAllOpaque_PriorityMediaType_AndParseableMime()
    {
        var text = Render(AllParsers(), verbose: true);
        Assert.DoesNotContain("run `ferret doctor --verbose`", text, StringComparison.Ordinal);
        Assert.Contains(".zip", text, StringComparison.Ordinal);           // full opaque list
        Assert.Contains("Priority: 200", text, StringComparison.Ordinal);  // per-parser detail
        Assert.Contains("Media Type: application/pdf", text, StringComparison.Ordinal);
        Assert.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document", text, StringComparison.Ordinal); // parseable MIME
    }

    [Fact]
    public void EmptyParsers_RendersSafeguard_NoException()
    {
        var text = Render([], verbose: false);
        Assert.Contains("Installed Parsers (0)", text, StringComparison.Ordinal);
        Assert.Contains("No parsers are registered.", text, StringComparison.Ordinal);
    }
}
