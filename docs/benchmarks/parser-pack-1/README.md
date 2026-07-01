# Enterprise Content Pack 1 — Performance Report

## Summary
- **PDF parser:** ~2,900 docs/sec
- **DOCX parser:** ~600 docs/sec
- **XLSX parser:** ~122 docs/sec (primary optimization target)
- **Dispatcher overhead:** negligible
- **50k-row workbook:** processed with ~326 MB peak working set

## Objective
Measure indexing throughput and parse cost for PDF, DOCX, and XLSX vs text/code,
including a large-workbook (multi-thousand-row) XLSX case.

## Environment
- **Runner:** BenchmarkDotNet v0.14.0, `DefaultJob`
- **OS:** Windows 11 (10.0.26200.8737)
- **CPU:** 11th Gen Intel Core i7-11850H @ 2.50 GHz — 1 socket, 8 physical / 16 logical cores
- **Runtime:** .NET SDK 9.0.313; host .NET 9.0.15, X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
- **Date:** 2026-07-01

> Single-machine, laptop-class results — treat as indicative, not absolute. Re-run
> `dotnet run -c Release --project tests/Ferret.Benchmarks` on the target hardware.

## Corpus
Recorded from the generated `corpus.json` manifest (deterministic, so results are reproducible):

- **Generator version:** 1.0
- **Seed:** 99
- **Size tier:** Small (~200 files)
- **Document count:** 194
- **Format counts:** `.cs` 60, `.md` 50, `.pdf` 30, `.docx` 20, `.xlsx` 20, `.json` 8, `.html` 6

## Methodology
Deterministic Small corpus via `SyntheticEnterpriseCorpusGenerator` (seed 99), laid out
under a realistic enterprise tree (Engineering / Operations / Quality / Management). Each
per-type `[Benchmark]` parses **all** files of that type through the composed
`ParserPackModule` dispatcher; the reported Mean is the whole-batch time.

```
dotnet run -c Release --project tests/Ferret.Benchmarks -- --filter *ParserThroughputBenchmark*
dotnet run -c Release --project tests/Ferret.Benchmarks -- --filter *ParseLargeWorkbook*   # in isolation
```

`Docs/sec` and `MB/sec` are derived from the batch Mean and the corpus format/byte totals
(PDF batch = 30 docs / 38.6 KB, DOCX = 20 / 48.5 KB, XLSX = 20 / 174.6 KB). Byte totals were
captured by generating the same seed-99 corpus and summing per-extension file sizes.

## Raw Measurements

| Type         | Docs/sec | MB/sec | Allocated | Peak WS  | Parser time (ms/doc) | Batch Mean |
| ------------ | -------- | ------ | --------- | -------- | -------------------- | ---------- |
| PDF          | ~2,900   | 3.65   | 6.81 MB   | —        | 0.34                 | 10.33 ms   |
| DOCX         | ~598     | 1.42   | 3.16 MB   | —        | 1.67                 | 33.42 ms   |
| XLSX         | ~122     | 1.04   | 41.87 MB  | —        | 8.17                 | 163.47 ms  |
| XLSX (large) | ~1.4     | 0.37   | 206.79 MB | 326 MB   | 708                  | 708.0 ms   |
| Code / text  | n/m      | n/m    | n/m       | —        | n/m                  | n/m        |

- **Allocated** is `[MemoryDiagnoser]` managed bytes per batch operation (allocations, not resident memory).
- **XLSX (large)** = one ~50k-row × 6-column single-sheet workbook (264 KB on disk) exercising the streaming reader.
- **Code / text** (`n/m` = not measured): the throughput suite covers only the binary formats
  (PDF/DOCX/XLSX). Plain text / Markdown / JSON / CSV go through the in-process text parsers and
  are not in this suite; the `IndexPipelineBenchmark.RunPipeline_10kFiles` benchmark covers the
  end-to-end text/code path separately.

## Dispatcher Overhead

| Path                | Docs/sec | Allocated | Batch Mean |
| ------------------- | -------- | --------- | ---------- |
| PDF direct          | ~2,515   | 6.80 MB   | 11.93 ms   |
| PDF via dispatcher  | ~2,900   | 6.81 MB   | 10.33 ms   |

> Overhead = (via dispatcher) − (direct). The two runs are within run-to-run noise (both PDF
> distributions were flagged bimodal by BenchmarkDotNet, mValue > 3), and allocations are
> identical to within 0.01 MB. **Takeaway:** media-type resolution + parser selection add no
> measurable cost on top of the parse itself — the dispatcher is effectively free.

> **Allocated** = `[MemoryDiagnoser]` managed bytes (allocations, not resident memory).
> **Peak WS** = `Process.PeakWorkingSet64` — resident memory, which is what "500 MB vs 5 GB"
> refers to; the diagnoser does not report it. **Allocated ≠ Working Set.** The 326 MB figure
> was captured from a standalone single-parse run of the 50k-row workbook (process-wide, so it
> includes the .NET runtime baseline and JIT); the in-benchmark `ParseLargeWorkbook` records the
> same value into its public `PeakWorkingSetBytes` property. Run the large-workbook benchmark in
> isolation for a clean peak reading.

## Observations
- **XLSX dominates parse cost.** At ~122 docs/sec it is ~24× slower per document than PDF and
  ~13× slower than DOCX, and allocates 41.87 MB for 20 small workbooks — OpenXML cell/shared-string
  handling is the hot path. PDF (page text via PdfPig) is the cheapest binary format here.
- **The streaming reader holds.** A 50k-row workbook parses with ~326 MB peak resident and
  ~207 MB managed allocations — linear in row count, no runaway. This is the "500 MB, not 5 GB"
  guarantee for realistic enterprise exports; the reader does not buffer the whole sheet.
- **Dispatcher overhead is negligible** (see above) — the isolation split between parser packages
  costs nothing at dispatch time.
- **MB/sec is fixed-cost-bound, not bandwidth-bound.** The Small-corpus files are KB-scale, so
  per-document setup (stream open, media-type resolve, parser construction) dominates and MB/sec
  understates real streaming throughput. The large-workbook row shows the streaming regime.

## Future Optimization Opportunities
- **XLSX shared-string / cell allocations.** 41.87 MB for 20 small workbooks and 207 MB for one
  50k-row sheet suggest pooling or a struct-based cell path could cut Gen1/Gen2 pressure.
- **Reduce per-document fixed cost** (parser/option object reuse) to lift MB/sec on the many-small-files
  case that dominates real code/doc repos.
- **Add a text/code throughput row** so the report compares binary formats against the plain-text
  baseline directly, and wire `IndexPipelineBenchmark` output into the Index-time column.
- **Track Peak WS in-suite.** `PeakWorkingSetBytes` is currently a public property read out-of-band;
  a custom BenchmarkDotNet column or exporter would fold it into the summary table automatically.

See [future-benchmarks.md](future-benchmarks.md) for the planned evolution of `IndexPipelineBenchmark`
(end-to-end files/sec by corpus tier) and `SearchBenchmark` (search latency by query category,
mean/P95/max) — both evolve the existing benchmarks onto the shared multi-format corpus.
