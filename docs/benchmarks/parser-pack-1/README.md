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
