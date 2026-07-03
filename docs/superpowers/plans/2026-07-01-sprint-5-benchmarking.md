# Sprint 5 — Benchmarking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Measure parser throughput and memory across document types — including a large multi-thousand-row XLSX case with peak-working-set capture — over the deterministic synthetic corpus, and produce a versioned performance report plus README "supported file types" documentation.

**Architecture:** A new `ParserThroughputBenchmark` (BenchmarkDotNet, `[MemoryDiagnoser]`) generates a Small corpus via the Sprint-4 `SyntheticEnterpriseCorpusGenerator`, resolves the full `ParserPackModule` dispatcher, and times parsing per document type (PDF / DOCX / XLSX). Because `[MemoryDiagnoser]` reports *allocations*, not resident memory, a separate large-workbook benchmark captures `Process.PeakWorkingSet64` — the "500 MB vs 5 GB" number enterprise users care about, which is where the Excel streaming reader's memory profile matters. Results land in a versioned report under `docs/benchmarks/parser-pack-1/`. Benchmarks build in CI but run on demand (not in CI).

**Tech Stack:** .NET 9, C#, BenchmarkDotNet, Microsoft.Extensions.DependencyInjection.

**Milestone spec:** `docs/superpowers/specs/2026-07-01-parser-pack-1-design.md` (§ Performance report)
**Benchmark Suite Spec:** `docs/superpowers/specs/2026-06-30-benchmark-suite-spec.md` (report format, metric categories)
**Parent plan (source of reused code):** `docs/superpowers/plans/2026-07-01-parser-pack-1.md` (Task 9)
**Predecessors (hard):** Sprint 2 (`ParserPackModule` + PDF), Sprint 3 (Office parsers), **Sprint 4** (`SyntheticEnterpriseCorpusGenerator` + renderers) must all be implemented first — this sprint consumes the generator and the composed dispatcher directly.

## Global Constraints

- **Target framework:** `net9.0`. `Ferret.Benchmarks` pins `net9.0` explicitly — do NOT change it.
- **Central Package Management:** every NuGet version lives in `Directory.Packages.props`; `<PackageReference>` carries **no** `Version` attribute. (No new packages this sprint — BenchmarkDotNet and the corpus/parser references already exist from earlier sprints.)
- **Benchmarks build in CI, run on demand.** CI verifies the benchmark project *compiles*; the full run (`dotnet run -c Release`) is manual.
- **Determinism:** the corpus feeding the benchmark uses a pinned seed so runs are comparable.
- **Report location:** `docs/benchmarks/parser-pack-1/README.md`, following the Benchmark Suite Spec format.
- **`PeakWorkingSet64` is process-wide**, not per-call — the large-workbook benchmark must be run in isolation for a clean reading, and the report must say so.
- **No production code changes.** This sprint touches only the benchmark project and docs; parser packages and the CLI are untouched.
- **StyleCop:** public types/members need XML doc comments.
- **No work, organization, or personal names** in code, comments, or commit messages.

---

## Task map

| Task | Deliverable | Project |
| ---- | ----------- | ------- |
| 1 | Per-type parser throughput benchmark (+ shared `TestAsset` helper) | `Ferret.Benchmarks` |
| 2 | Large-workbook benchmark with peak-working-set capture | `Ferret.Benchmarks` |
| 3 | Performance report skeleton + README supported-file-types | `docs/`, repo root |

Task 1 introduces the benchmark + the `TestAsset` helper both benchmarks share. Task 2 extends it with the large-workbook case. Task 3 documents the methodology and output. All three depend on Sprints 2–4.

---

### Task 1: Per-type parser throughput benchmark

**Files:**
- Create: `tests/Ferret.Benchmarks/Benchmarks/TestAsset.cs` (shared helper building an `AssetDescriptor` from a file path)
- Create: `tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs`
- Modify: `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj` (add `Ferret.Parsers` ProjectReference)

**Interfaces:**
- Consumes: `SyntheticEnterpriseCorpusGenerator`, `CorpusSize` (Sprint 4); `ParserPackModule` (Sprint 2/3); `IParserDispatcher`, `IMimeTypeResolver`, `MimeTypeResolver`, `AssetDescriptor` (`Ferret.Core` / `Ferret.ParserPlatform`).
- Produces: `internal static class TestAsset { static AssetDescriptor For(string path, string mediaType); }`; `public class ParserThroughputBenchmark` with `[Benchmark]` methods `ParseAllPdfs`, `ParseAllDocx`, `ParseAllXlsx`.

- [ ] **Step 1: Add the `Ferret.Parsers` reference to the benchmark project**

In `tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj`, add to the ProjectReference `ItemGroup`:

```xml
<ProjectReference Include="..\..\src\Ferret.Parsers\Ferret.Parsers.csproj" />
```

(`Ferret.Parsers.Office` and the OpenXml/PdfPig packages were added in Sprint 4; this adds the composition project so the benchmark can resolve the full `ParserPackModule` dispatcher.)

- [ ] **Step 2: Add the shared `TestAsset` helper**

Match the property names against a real `AssetDescriptor` construction in the codebase (e.g. `PdfParserTests`/`JsonParserTests`) before finalizing — the fields below mirror those fixtures.

```csharp
// tests/Ferret.Benchmarks/Benchmarks/TestAsset.cs
using Ferret.Core.Connectors;
using Ferret.Core.Primitives;

namespace Ferret.Benchmarks.Benchmarks;

/// <summary>Builds an <see cref="AssetDescriptor"/> from a filesystem path for benchmark/dispatch use.</summary>
internal static class TestAsset
{
    /// <summary>Creates an asset descriptor for the given file path and resolved media type.</summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="mediaType">The resolved media type.</param>
    /// <returns>An <see cref="AssetDescriptor"/>.</returns>
    public static AssetDescriptor For(string path, string mediaType)
    {
        var name = Path.GetFileName(path);
        var uri = new Uri("filesystem:///" + name);
        return new AssetDescriptor
        {
            Id = AssetId.From(uri),
            ConnectorId = new ConnectorId("filesystem"),
            InstanceId = new ConnectorInstanceId("bench"),
            Kind = AssetKind.File,
            CanonicalUri = uri,
            DisplayName = name,
            LastModified = DateTimeOffset.UnixEpoch, // fixed: benchmark inputs are deterministic
            MediaType = mediaType,
        };
    }
}
```

- [ ] **Step 3: Implement the throughput benchmark**

```csharp
// tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs
using BenchmarkDotNet.Attributes;

using Ferret.Benchmarks.Corpus;
using Ferret.Core.Documents;
using Ferret.Parsers;
using Ferret.ParserPlatform;

using Microsoft.Extensions.DependencyInjection;

namespace Ferret.Benchmarks.Benchmarks;

/// <summary>Measures parse throughput per document type (PDF, DOCX, XLSX) over a Small corpus.</summary>
[MemoryDiagnoser]
public class ParserThroughputBenchmark
{
    private string _root = string.Empty;
    private IParserDispatcher _dispatcher = null!;
    private IMimeTypeResolver _resolver = null!;

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
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    // The corpus is laid out under a realistic enterprise tree (Engineering/Operations/Quality/
    // Management), so discovery recurses and filters by extension rather than assuming per-format dirs.
    [Benchmark]
    public async Task ParseAllPdfs()
    {
        foreach (var path in Directory.GetFiles(_root, "*.pdf", SearchOption.AllDirectories))
        {
            await ParseOne(path);
        }
    }

    [Benchmark]
    public async Task ParseAllDocx()
    {
        foreach (var path in Directory.GetFiles(_root, "*.docx", SearchOption.AllDirectories))
        {
            await ParseOne(path);
        }
    }

    [Benchmark]
    public async Task ParseAllXlsx()
    {
        foreach (var path in Directory.GetFiles(_root, "*.xlsx", SearchOption.AllDirectories))
        {
            await ParseOne(path);
        }
    }

    private async Task ParseOne(string path)
    {
        var mediaType = _resolver.Resolve(Path.GetFileName(path)).MediaType;
        var asset = TestAsset.For(path, mediaType);
        await using var fs = File.OpenRead(path);
        await _dispatcher.DispatchAsync(fs, asset);
    }
}
```

- [ ] **Step 3b: Add a dispatcher-vs-direct-parse benchmark**

Rather than a metadata-only benchmark (metadata extraction is not separable from `ParseAsync` without an artificial parser API), measure the more useful architectural number: **dispatcher overhead** — the cost of media-type resolution + parser selection on top of the parse itself. Add a `PdfParser` constructed directly and a paired benchmark. Add `using Ferret.Parsers.Pdf;` to the file, and these members to `ParserThroughputBenchmark`:

```csharp
private PdfParser _pdfDirect = null!;

// ...at the end of Setup():
_pdfDirect = new PdfParser(new ParserOptions());

// Baseline: parse PDFs directly, bypassing the dispatcher (no resolve, no parser selection).
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
```

`ParsePdfsDirect` vs `ParseAllPdfs` (same files, via the dispatcher) is the dispatcher-overhead comparison. Add `using Ferret.Core.Documents;` if `ParseContext`/`ParserOptions` are not already in scope.

- [ ] **Step 4: Build the benchmark project (compile-only verification)**

Run: `dotnet build tests/Ferret.Benchmarks -c Release`
Expected: build succeeds. If `Program.cs` uses BenchmarkDotNet's auto-discovery (`BenchmarkSwitcher.FromAssembly(...).Run(args)`), the new benchmarks are picked up with no wiring; if it hardcodes a benchmark list, add `ParserThroughputBenchmark` to it.

- [ ] **Step 5: Commit**

```bash
git add tests/Ferret.Benchmarks/Ferret.Benchmarks.csproj tests/Ferret.Benchmarks/Benchmarks/TestAsset.cs tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs
git commit -m "feat(bench): add per-type parser throughput benchmark over the synthetic corpus"
```

---

### Task 2: Large-workbook benchmark with peak-working-set capture

**Files:**
- Modify: `tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs` (add the large-workbook setup + benchmark)

**Interfaces:**
- Consumes: `XlsxRenderer`, `CorpusTable`, `CorpusDocument` (Sprint 4); the existing dispatcher/resolver from Task 1.
- Produces: `ParseLargeWorkbook` `[Benchmark]` and a recorded `PeakWorkingSet64` reading.

- [ ] **Step 1: Build a large single-sheet workbook in setup**

Add the fields and extend `[GlobalSetup]` in `ParserThroughputBenchmark.cs` to render one ~50k-row workbook (a realistic enterprise export size that exercises the streaming reader). It is written **outside** `_root` so the recursive `*.xlsx` glob in `ParseAllXlsx` does not sweep it up. Add the required usings: `using Ferret.Benchmarks.Corpus.Renderers;` and `using Ferret.Core.Documents;` (for `DocumentMetadata`). Extend `[GlobalCleanup]` to delete `_largeXlsxPath`.

```csharp
private string _largeXlsxPath = string.Empty;
private long _peakWorkingSetBytes;

// ...inside Setup(), after generating the Small corpus (kept out of _root by design):
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

// ...inside Cleanup():
if (File.Exists(_largeXlsxPath)) File.Delete(_largeXlsxPath);
```

> `CorpusDocument` now carries a metadata dictionary and `CorpusTable` rows are `IReadOnlyList<CorpusCell>` (Sprint 4). The `using Ferret.Benchmarks.Corpus;` from Task 1 already covers `CorpusCell`/`CorpusTable`/`CorpusDocument`.

- [ ] **Step 2: Add the large-workbook benchmark that captures peak working set**

```csharp
[Benchmark]
public async Task ParseLargeWorkbook()
{
    var proc = System.Diagnostics.Process.GetCurrentProcess();
    await ParseOne(_largeXlsxPath); // ~50k-row single-sheet workbook built in [GlobalSetup]
    proc.Refresh();
    _peakWorkingSetBytes = proc.PeakWorkingSet64; // recorded into the report's "Peak WS" column
}
```

- [ ] **Step 3: Build the benchmark project**

Run: `dotnet build tests/Ferret.Benchmarks -c Release`
Expected: build succeeds.

- [ ] **Step 4: (Optional, manual) Run the large-workbook benchmark in isolation**

Run: `dotnet run -c Release --project tests/Ferret.Benchmarks -- --filter *ParseLargeWorkbook*`
Expected: completes; note the `Allocated` column and the captured peak working set. Run in isolation because `PeakWorkingSet64` is process-wide.

- [ ] **Step 5: Commit**

```bash
git add tests/Ferret.Benchmarks/Benchmarks/ParserThroughputBenchmark.cs
git commit -m "feat(bench): add large-workbook XLSX benchmark with peak-working-set capture"
```

---

### Task 3: Performance report skeleton + README supported-file-types

**Files:**
- Create: `docs/benchmarks/parser-pack-1/README.md`
- Modify: `README.md` (repo root — supported file types + parser packages + extraction-limit setting)

**Interfaces:** none (documentation).

- [ ] **Step 1: Create the report skeleton**

```markdown
<!-- docs/benchmarks/parser-pack-1/README.md -->
# Enterprise Content Pack 1 — Performance Report

## Objective
Measure indexing throughput and parse cost for PDF, DOCX, and XLSX vs text/code,
including a large-workbook (multi-thousand-row) XLSX case.

## Environment
(CPU, RAM, .NET version, corpus size)

## Corpus
Recorded from the generated `corpus.json` manifest so results are reproducible:

| Field | Value |
| ----- | ----- |
| Generator version | (corpus.json `generatorVersion`) |
| Seed | (corpus.json `seed`) |
| Size tier | (corpus.json `size`) |
| Document count | (corpus.json `documentCount`) |

## Methodology
Deterministic Small/Medium corpus via SyntheticEnterpriseCorpusGenerator (seed pinned).
Run: `dotnet run -c Release --project tests/Ferret.Benchmarks`

## Raw Measurements
| Type          | Docs/sec | MB/sec | Allocated | Peak WS | Parser time | Index time |
| ------------- | -------- | ------ | --------- | ------- | ----------- | ---------- |
| PDF           |          |        |           |         |             |            |
| DOCX          |          |        |           |         |             |            |
| XLSX          |          |        |           |         |             |            |
| XLSX (large)  |          |        |           |         |             |            |
| Code          |          |        |           |         |             |            |

## Dispatcher Overhead
| Path                 | Docs/sec | Allocated |
| -------------------- | -------- | --------- |
| PDF direct           |          |           |
| PDF via dispatcher   |          |           |

> Overhead = (via dispatcher) − (direct). Isolates media-type resolution + parser
> selection cost from the parse itself.

> **Allocated** = `[MemoryDiagnoser]` managed bytes (allocations, not resident memory).
> **Peak WS** = `Process.PeakWorkingSet64` captured around the large-workbook run (see the
> ParseLargeWorkbook benchmark) — this is resident memory, which is what
> "500 MB vs 5 GB" refers to; the diagnoser does not report it. **Allocated ≠ Working Set.**
> Run the large-workbook benchmark in isolation for a clean peak reading.

## Observations

## Future Optimization Opportunities
```

- [ ] **Step 2: Update the root README supported-file-types section**

In `README.md`, add a "Supported file types" section listing: source code & text/config (via PlainText/Markdown/JSON), structured CSV/TSV (`CsvParser`), **PDF** (`Ferret.Parsers.Pdf`), **Word .docx** and **Excel .xlsx** (`Ferret.Parsers.Office`), composed via `Ferret.Parsers` / `ParserPackModule`. Document the configurable `Ferret:Parsers:MaxExtractedCharacters` setting (default unlimited). Mention `ferret doctor` shows installed parsers and the supported-extension count. Match the README's existing heading style and section placement.

- [ ] **Step 3: Build the benchmark project (final compile-only verification)**

Run: `dotnet build tests/Ferret.Benchmarks -c Release`
Expected: build succeeds. (Full benchmark execution is run on demand, not in CI.)

- [ ] **Step 4: Commit**

```bash
git add docs/benchmarks/parser-pack-1/README.md README.md
git commit -m "docs(bench): add Enterprise Content Pack 1 performance report skeleton and supported file types"
```

---

## Final verification

- [ ] **Full solution build**

Run: `dotnet build src/Ferret.sln -c Release`
Expected: build clean (benchmark project compiles; production projects and tests unchanged).

- [ ] **Benchmark smoke run (manual)**

Run: `dotnet run -c Release --project tests/Ferret.Benchmarks -- --filter *ParserThroughputBenchmark*`
Expected: completes and emits per-type rows; transcribe results into the report skeleton.

- [ ] **Acceptance criteria check**

Confirm each: benchmark compiles in a Release build · per-type throughput measured for PDF/DOCX/XLSX over the deterministic corpus · large-workbook case exercises the streaming reader and captures `PeakWorkingSet64` · report skeleton exists under `docs/benchmarks/parser-pack-1/` in the Benchmark Suite Spec format · README lists all supported file types + parser packages + extraction-limit setting · no production code changed · no new NuGet packages.
