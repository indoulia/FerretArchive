# Enterprise Content Pack 1 — Planned Benchmark Evolution

Follow-up to the [performance report](README.md). Two additions give a balanced view of
**ingestion** and **retrieval**, alongside the existing per-type parser throughput.

**Guiding constraint:** the existing `IndexPipelineBenchmark` and `SearchBenchmark` are
**retained and evolved, not replaced**. Reuse the current scaffolding (real `IndexPipeline`,
real `SearchService` + `Bm25SearchProvider`, `SingleConnectorManager`, temp-dir SQLite); change
only the **corpus source**, **parameterization**, and **reporting**. Both already run the real
end-to-end machinery — the gap is the input corpus and the shape of the output.

---

## 1. End-to-end indexing throughput (Corpus → `IndexPipeline` → SQLite → done)

The number users compare against other tools: files/sec through the full pipeline.

**Evolve `IndexPipelineBenchmark`:**
- **Corpus source:** replace the 10,000 homogeneous 200-char `.cs` files in `[GlobalSetup]` with
  `SyntheticEnterpriseCorpusGenerator(seed: 99).Generate(size, tempDir)` — the same multi-format
  corpus (PDF/DOCX/XLSX/code/text) used by the parser-throughput report, so ingestion is measured
  over realistic content.
- **Parser dispatcher:** replace `ParserRegistryBuilder.Build([new PlainTextParser()])` with the
  full pack — resolve `IParserDispatcher` from `ParserPackModule.ConfigureServices(services)` — so
  binary formats actually flow through the pipeline (today they would be skipped).
- **Parameterization:** add `[Params(CorpusSize.Small, CorpusSize.Medium, CorpusSize.Enterprise)]`
  driving the generated size. Keep `ForceRebuild = true`. (Note: the current `<60s` `[IterationCleanup]`
  target is calibrated to 10k `.cs` files — re-baseline per tier or gate it on size.)
- **Reporting:** emit `IndexResult` file count + `Duration` per tier into the report as:

  | Corpus | Files | Time | Files/sec |
  | ------ | ----- | ---- | --------- |
  | Small  |  194  |      |           |
  | Medium |       |      |           |
  | Large  |       |      |           |

## 2. Search latency by query category (mean / P95 / max)

Retrieval performance across representative query shapes now that PDF/DOCX/XLSX content is indexed.

**Evolve `SearchBenchmark`:**
- **Corpus source:** index the `SyntheticEnterpriseCorpusGenerator` corpus through the **full
  `ParserPackModule`** dispatcher in `[GlobalSetup]` (reusing the existing index-then-search wiring),
  so PDF/Word/Excel text is actually searchable — the current fake `.cs` index cannot answer
  content queries against binary formats.
- **Query categories:** replace the flat `Queries[]` with the six representative shapes, each its
  own case so BenchmarkDotNet reports it separately: **class name**, **method name**, **phrase**
  (quoted), **PDF text**, **Word text**, **Excel cell value**. Drive them via `[Params]` or one
  `[Benchmark]` per category.
- **Reporting shape:** report **mean / P95 / max** per category. BenchmarkDotNet percentile columns
  (P95, Max) apply per *op*, so make one op = one query (not the current 10-queries-averaged op);
  enable percentile columns in the config. Alternatively aggregate `ExecutionInfo.Duration` samples
  manually.
- **Target:** keep the existing `<200 ms` mean guard, now applied per category.

  | Query           | Mean | P95 | Max |
  | --------------- | ---- | --- | --- |
  | class name      |      |     |     |
  | method name     |      |     |     |
  | phrase          |      |     |     |
  | PDF text        |      |     |     |
  | Word text       |      |     |     |
  | Excel cell value|      |     |     |

---

## Shared prerequisites
- Both consume the seed-99 `SyntheticEnterpriseCorpusGenerator` corpus and the composed
  `ParserPackModule` dispatcher — the same inputs the parser-throughput benchmark already uses.
- Benchmarks continue to **build in CI, run on demand** (no CI execution).
- `Enterprise` tier (~15,000 files) is I/O- and time-heavy; document expected wall-clock so a full
  run isn't mistaken for a hang.
