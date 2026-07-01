using System.Globalization;
using System.Text.Json;

using Ferret.Benchmarks.Corpus.Renderers;
using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus;

/// <summary>
/// Generates a deterministic, multi-format synthetic enterprise corpus laid out under a realistic
/// enterprise folder tree (Engineering / Operations / Quality / Management), plus a corpus.json
/// manifest. Same seed + size produces identical output (byte-identical text; identical extracted
/// text/metadata for binaries). Reusable beyond benchmarks; lives in the benchmark project.
/// </summary>
public sealed class SyntheticEnterpriseCorpusGenerator
{
    /// <summary>Manifest schema/generator version. Bump when the layout or content changes.</summary>
    public const string GeneratorVersion = "1.0";

    // camelCase so corpus.json keys (seed, size, documentCount, formatCounts, …) match what the
    // determinism/validation tests read via JsonElement.GetProperty.
    private static readonly JsonSerializerOptions ManifestJsonOptions =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // Prose title families per role, keeping generated documents recognizably enterprise-like.
    private static readonly string[] AdrTitles = ["Architecture Decision {0}", "Design Proposal {0}", "RFC {0}"];
    private static readonly string[] DocTitles = ["Design Specification {0}", "Knowledge Base Article {0}", "Configuration Guide {0}"];
    private static readonly string[] RunbookTitles = ["Runbook {0}", "Operations Guide {0}"];
    private static readonly string[] IncidentTitles = ["Incident Report {0}", "Postmortem {0}"];
    private static readonly string[] SpecTitles = ["Technical Specification {0}", "Interface Design {0}"];
    private static readonly string[] PlanningTitles = ["Sprint {0} Planning", "Quarterly Review {0}", "Release Notes {0}"];
    private static readonly string[] SourceTitles = ["Service {0}", "Repository {0}", "Controller {0}"];
    private static readonly string[] MixedTitles = ["Meeting Minutes {0}", "Status Update {0}"];

    private static readonly string[] Names = ["Alice", "Bob", "Chandra", "Dana", "Omar", "Priya"];

    // Deterministic sentence templates — natural prose without unseeded randomness.
    private static readonly string[] SentenceTemplates =
    [
        "The indexing pipeline stores extracted content in the workspace.",
        "The connector periodically synchronizes remote repositories.",
        "Search latency improved after introducing compression.",
        "The deployment failed because authentication tokens expired.",
        "Retrieval quality is measured across code and documents.",
        "The parser extracts text and lightweight metadata from each stream.",
        "Context assembly ranks candidates before returning the top results.",
        "Throughput scales with the number of connector instances.",
    ];

    private readonly int _seed;

    /// <summary>Initializes a new instance of the <see cref="SyntheticEnterpriseCorpusGenerator"/> class.</summary>
    /// <param name="seed">The RNG seed.</param>
    public SyntheticEnterpriseCorpusGenerator(int seed) => _seed = seed;

    /// <summary>Generates the corpus into <paramref name="outputRoot"/> and writes corpus.json.</summary>
    /// <param name="size">The corpus size tier.</param>
    /// <param name="outputRoot">The destination directory (created if missing).</param>
    public void Generate(CorpusSize size, string outputRoot)
    {
        ArgumentNullException.ThrowIfNull(outputRoot);
        var rng = new Random(_seed); // single seeded RNG drives all content => deterministic
        var layout = LayoutFor(size);

        var formatCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var archetypeCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var documentCount = 0;

        foreach (var entry in layout)
        {
            var dir = Path.Join(new[] { outputRoot }.Concat(entry.RelativePath.Split('/')).ToArray());
            Directory.CreateDirectory(dir);

            for (var i = 0; i < entry.Count; i++)
            {
                CorpusDocument doc;
                if (entry.ProseTitles is null)
                {
                    // Tabular role: cycle the archetypes (rebuilt per doc so RNG advances deterministically).
                    var archetypes = EnterpriseArchetypes.Build(rng);
                    doc = archetypes[i % archetypes.Count];
                    archetypeCounts[doc.Title] = archetypeCounts.GetValueOrDefault(doc.Title) + 1;
                }
                else
                {
                    doc = BuildProse(rng, i, entry.ProseTitles);
                }

                var fileName = string.Create(CultureInfo.InvariantCulture, $"doc{i:D5}{entry.Renderer.Extension}");
                using (var fs = File.Create(Path.Join(dir, fileName)))
                {
                    entry.Renderer.Render(doc, fs);
                }

                formatCounts[entry.Renderer.Extension] = formatCounts.GetValueOrDefault(entry.Renderer.Extension) + 1;
                documentCount++;
            }
        }

        WriteManifest(outputRoot, size, documentCount, formatCounts, archetypeCounts);
    }

    private static CorpusDocument BuildProse(Random rng, int index, string[] titleTemplates)
    {
        var blocks = new List<CorpusBlock>();
        var paraCount = 3 + rng.Next(5);
        for (var p = 0; p < paraCount; p++)
        {
            blocks.Add(new CorpusBlock(CorpusBlockKind.Paragraph, SentenceTemplates[rng.Next(SentenceTemplates.Length)]));
        }

        var template = titleTemplates[rng.Next(titleTemplates.Length)];
        var title = string.Format(CultureInfo.InvariantCulture, template, index);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DocumentMetadata.Author] = Names[rng.Next(Names.Length)],
            [DocumentMetadata.Subject] = title,
            [DocumentMetadata.Category] = "Prose",
        };
        return new CorpusDocument(title, metadata, blocks, Tables: []);
    }

    // Realistic enterprise tree; every binary format (.pdf/.docx/.xlsx) and code/text appears.
    private static IReadOnlyList<Entry> LayoutFor(CorpusSize size)
    {
        var c = CountsFor(size);
        return
        [
            new("Engineering/Source", new CSharpRenderer(), c.Code, SourceTitles),
            new("Engineering/Docs", new MarkdownRenderer(), c.Docs, DocTitles),
            new("Engineering/ADR", new MarkdownRenderer(), c.Adr, AdrTitles),
            new("Engineering/Specs", new DocxRenderer(), c.Word, SpecTitles),
            new("Operations/Runbooks", new MarkdownRenderer(), c.Runbooks, RunbookTitles),
            new("Operations/Incidents", new PdfRenderer(), c.Pdf, IncidentTitles),
            new("Quality/Matrices", new XlsxRenderer(), c.Excel, ProseTitles: null), // tabular archetypes
            new("Management/Planning", new PdfRenderer(), c.Planning, PlanningTitles),
            new("Management/Notes", new JsonRenderer(), c.Json, MixedTitles),
            new("Management/Portal", new HtmlRenderer(), c.Html, MixedTitles),
        ];
    }

    private static (int Code, int Docs, int Adr, int Word, int Runbooks, int Pdf, int Excel, int Planning, int Json, int Html) CountsFor(CorpusSize size) => size switch
    {
        CorpusSize.Small => (60, 20, 15, 20, 15, 20, 20, 10, 8, 6),
        CorpusSize.Medium => (700, 200, 150, 200, 120, 200, 200, 100, 80, 50),
        CorpusSize.Enterprise => (6000, 1500, 1000, 1500, 800, 1500, 1500, 600, 400, 200),
        _ => (60, 20, 15, 20, 15, 20, 20, 10, 8, 6),
    };

    private void WriteManifest(
        string outputRoot,
        CorpusSize size,
        int documentCount,
        IReadOnlyDictionary<string, int> formatCounts,
        IReadOnlyDictionary<string, int> archetypeCounts)
    {
        var manifest = new CorpusManifest(
            GeneratorVersion, _seed, size.ToString(), documentCount, formatCounts, archetypeCounts);
        File.WriteAllText(Path.Join(outputRoot, "corpus.json"), JsonSerializer.Serialize(manifest, ManifestJsonOptions));
    }

    private sealed record Entry(string RelativePath, IDocumentRenderer Renderer, int Count, string[]? ProseTitles);
}
