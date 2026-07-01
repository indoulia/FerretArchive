using System.Diagnostics;

using BenchmarkDotNet.Attributes;

using Ferret.Benchmarks.Corpus;
using Ferret.Benchmarks.Corpus.Renderers;
using Ferret.Core.Documents;
using Ferret.ParserPlatform;
using Ferret.Parsers;
using Ferret.Parsers.Pdf;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Benchmarks.Benchmarks;

/// <summary>Measures parse throughput per document type (PDF, DOCX, XLSX) over a Small corpus,
/// plus dispatcher overhead and a large-workbook peak-working-set reading.</summary>
[MemoryDiagnoser]
public class ParserThroughputBenchmark
{
    private string _root = string.Empty;
    private string _largeXlsxPath = string.Empty;
    private IParserDispatcher _dispatcher = null!;
    private MimeTypeResolver _resolver = null!;
    private PdfParser _pdfDirect = null!;

    /// <summary>Gets the peak working set (bytes) captured around the large-workbook parse.</summary>
    public long PeakWorkingSetBytes { get; private set; }

    /// <summary>Generates the corpus and resolves the composed parser dispatcher.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _root = Path.Join(Path.GetTempPath(), "pp-bench-" + Guid.NewGuid().ToString("N"));
        new SyntheticEnterpriseCorpusGenerator(seed: 99).Generate(CorpusSize.Small, _root);

        var services = new ServiceCollection();
        ParserPackModule.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        _dispatcher = provider.GetRequiredService<IParserDispatcher>();
        _resolver = new MimeTypeResolver();
        _pdfDirect = new PdfParser(new ParserOptions());

        // A ~50k-row single-sheet workbook (realistic enterprise export) that exercises the streaming
        // reader. Written outside _root so the recursive *.xlsx glob below does not sweep it up.
        _largeXlsxPath = Path.Join(Path.GetTempPath(), "pp-large-" + Guid.NewGuid().ToString("N") + ".xlsx");
        var headers = new[] { "Key", "Summary", "Severity", "Resolved", "Assignee", "Sprint" };
        var rows = new List<IReadOnlyList<CorpusCell>>(50_000);
        for (var i = 0; i < 50_000; i++)
        {
            rows.Add(
            [
                CorpusCell.Text($"BUG-{i:D6}"),
                CorpusCell.Text("login export index search auth cache"),
                CorpusCell.Text("High"),
                CorpusCell.Boolean(i % 2 == 0),
                CorpusCell.Text("Alice"),
                CorpusCell.Text("S-14"),
            ]);
        }

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal) { [DocumentMetadata.Author] = "Alice" };
        var largeDoc = new CorpusDocument("Large Bug Export", metadata, [], [new CorpusTable(headers, rows)]);
        using (var fs = File.Create(_largeXlsxPath))
        {
            new XlsxRenderer().Render(largeDoc, fs);
        }
    }

    /// <summary>Deletes the generated corpus and the large workbook.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }

        if (File.Exists(_largeXlsxPath))
        {
            File.Delete(_largeXlsxPath);
        }
    }

    /// <summary>Parses every PDF in the corpus through the dispatcher.</summary>
    /// <returns>A task representing the benchmark run.</returns>
    [Benchmark]
    public async Task ParseAllPdfs()
    {
        foreach (var path in Directory.GetFiles(_root, "*.pdf", SearchOption.AllDirectories))
        {
            await ParseOne(path);
        }
    }

    /// <summary>Parses every DOCX in the corpus through the dispatcher.</summary>
    /// <returns>A task representing the benchmark run.</returns>
    [Benchmark]
    public async Task ParseAllDocx()
    {
        foreach (var path in Directory.GetFiles(_root, "*.docx", SearchOption.AllDirectories))
        {
            await ParseOne(path);
        }
    }

    /// <summary>Parses every XLSX in the corpus through the dispatcher.</summary>
    /// <returns>A task representing the benchmark run.</returns>
    [Benchmark]
    public async Task ParseAllXlsx()
    {
        foreach (var path in Directory.GetFiles(_root, "*.xlsx", SearchOption.AllDirectories))
        {
            await ParseOne(path);
        }
    }

    /// <summary>Baseline: parses PDFs directly, bypassing the dispatcher (no resolve/selection).</summary>
    /// <returns>A task representing the benchmark run.</returns>
    [Benchmark]
    public async Task ParsePdfsDirect()
    {
        foreach (var path in Directory.GetFiles(_root, "*.pdf", SearchOption.AllDirectories))
        {
            var asset = TestAsset.For(path, "application/pdf");
            await using var fs = File.OpenRead(path);
            await _pdfDirect.ParseAsync(fs, ParseContext.For(asset));
        }
    }

    /// <summary>Parses the ~50k-row workbook and records process peak working set.</summary>
    /// <returns>A task representing the benchmark run.</returns>
    [Benchmark]
    public async Task ParseLargeWorkbook()
    {
        using var proc = Process.GetCurrentProcess();
        await ParseOne(_largeXlsxPath); // ~50k-row single-sheet workbook built in [GlobalSetup]
        proc.Refresh();
        PeakWorkingSetBytes = proc.PeakWorkingSet64; // recorded into the report's "Peak WS" column
    }

    private async Task ParseOne(string path)
    {
        var mediaType = _resolver.Resolve(Path.GetFileName(path)).MediaType;
        var asset = TestAsset.For(path, mediaType);
        await using var fs = File.OpenRead(path);
        await _dispatcher.DispatchAsync(fs, asset);
    }
}
