# Ferret Engineering Productivity Benchmark Suite — Spec

**Date:** 2026-06-30
**Status:** Approved for implementation (rev 2)
**Author:** Captured from product session

---

## Problem Statement

After RC1, the question "why Ferret over Copilot Chat, Cursor, or plain Claude Code?" can only be answered architecturally. This benchmark suite converts that architectural argument into evidence — numbers that engineering leaders and developers can evaluate.

The goal is not "performance benchmarking." It is an **Engineering Productivity Benchmark Suite**: evidence that Ferret changes how developers and AI work with a codebase.

---

## Benchmark Categories

### Category 1 — Platform Benchmarks (Performance)

*Is Ferret fast?*

| Metric                       | Target    |
| ---------------------------- | --------- |
| Index 1,000 files            | < 5 s     |
| Index 10,000 files           | < 45 s    |
| Incremental index (1 file)   | < 500 ms  |
| Search latency               | < 100 ms  |
| Context assembly (10 docs)   | < 250 ms  |
| MCP tool response            | < 300 ms  |
| CLI startup                  | < 2 s     |

Tool: BenchmarkDotNet (already wired in `tests/Ferret.Benchmarks`).

---

### Category 2 — Scale Benchmarks

*Does Ferret hold up at real repo sizes?*

**Synthetic corpora** (automated, reproducible):

| Corpus       | Files  | LOC    |
| ------------ | ------ | ------ |
| Small        | 200    | 20K    |
| Medium       | 2,000  | 250K   |
| Enterprise   | 15,000 | 2M     |

**Real corpora** (opt-in via `--real-corpus` flag; not run in CI by default):

```
benchmarks/corpora/
    ferret/         ← Ferret's own repo (always available, default real-corpus)
    aspnet/         ← ASP.NET Runtime (optional clone, ~large)
    orchard/        ← Orchard Core (medium)
    eshop/          ← eShopOnContainers (medium)
    roslyn/         ← Roslyn (large, optional)
```

Real-corpus runs must pin a git commit hash for reproducibility. Results are stored in `docs/benchmarks/<version>/real-corpora/`.

---

### Category 3 — Context Quality Benchmarks

*Does Ferret surface the right documents?*

For a curated Q&A dataset (20–50 question/answer pairs against Ferret's own repo):

| Metric              | Description                                          |
| ------------------- | ---------------------------------------------------- |
| Precision@k         | Fraction of top-k retrieved docs that are relevant   |
| Recall@k            | Fraction of relevant docs found in top-k             |
| Mean Reciprocal Rank | How high does the first relevant doc rank?          |
| nDCG@10             | Normalized Discounted Cumulative Gain (IR standard)  |
| Success@1           | Was the first result relevant?                       |
| Success@5           | Was at least one relevant result in top 5?           |
| Success@10          | Was at least one relevant result in top 10?          |
| Token count         | Tokens in assembled context package                  |

> **Note on nDCG:** current eval dataset uses binary relevance (relevant/not relevant). nDCG@10 will be computed with grade=1 for relevant, grade=0 otherwise. Graded relevance can be added to the dataset in a future iteration.

Automated: the eval runner queries Ferret and scores results against the dataset.

---

### Category 4 — Context Effectiveness Benchmarks

*Does Ferret improve the context supplied to any model?*

The category name is **Context Effectiveness** (not "AI benchmarks") — Ferret is not benchmarking models; it is benchmarking how much Ferret improves the context available to any downstream model.

**Primary metric: Time to First Useful Context (TTFUC)**

```
Question → Search → Dedup → Expand → Filter → Budget → Prompt Package Ready
```

| Repository          | TTFUC target |
| ------------------- | ------------ |
| Ferret (1K files)   | < 250 ms     |
| Medium (2K files)   | < 400 ms     |
| Enterprise (15K)    | < 800 ms     |

**Context assembly stage breakdown:**

Also benchmark each pipeline stage individually to identify optimization targets:

| Stage    | Description                        |
| -------- | ---------------------------------- |
| Search   | BM25 FTS5 query                    |
| Dedup    | Remove repeated DocumentIds        |
| Expand   | Fetch full documents               |
| Filter   | Remove empty/duplicate content     |
| Budget   | Apply MaxTokens / MaxDocuments     |

**Automated metrics (no live LLM calls required):**

| Metric                    | Description                                          |
| ------------------------- | ---------------------------------------------------- |
| Baseline token estimate   | Entire corpus chars / 4                              |
| Ferret token estimate     | Context package chars / 4                            |
| Token reduction %         | `(baseline - ferret) / baseline × 100`               |
| **Context compression ratio** | `ferret_tokens / corpus_tokens` (e.g. 0.003 = 99.7%) |
| Documents surfaced        | Count of docs in context package                     |

**Live-call metrics (optional, requires `ANTHROPIC_API_KEY`):**

| Metric              | Claude Alone | Claude + Ferret |
| ------------------- | ------------ | --------------- |
| Actual tokens used  | measured     | measured        |
| Prompt iterations   | measured     | measured        |
| Answer correctness  | manual score | manual score    |

---

### Category 5 — Engineering Productivity Benchmarks

*Does Ferret actually make developers faster?*

**Automatable proxy** (covered by TTFUC + Category 3):
- Docs retrieved per query
- Time to context assembly
- Retrieval precision (how many surfaced docs are relevant)

**Human evaluation** (manual protocol, not automated):

Tasks:
1. Find where indexing starts
2. Add a parser
3. Explain connector lifecycle
4. Locate BM25 implementation
5. Find all extension points

Measured per task:
- Clock time (developer stopwatch)
- Number of retrieved docs reviewed
- Number of follow-up prompts required
- Subjective difficulty (1–5 scale)

Protocol documented in `docs/benchmarks/PRODUCTIVITY-EVAL-PROTOCOL.md`.

---

### Category 6 — Context Assembly Stage Benchmarks

*Where does time go inside context assembly?*

Each stage timed independently using BenchmarkDotNet:

```
Search → Dedup → Expand → Filter → Budget
```

This reveals optimization targets for each RC.

---

### Reserved Categories (Future)

These categories are reserved for V2+ work. RC1 leaves them empty.

#### Federation Benchmarks
- Distributed search across multiple Knowledge Spaces
- Shared Knowledge context assembly
- Remote context assembly latency

#### Host Startup Benchmarks
- CLI cold/warm startup
- MCP server startup
- REST host startup
- Watch mode initialization

---

## Signature Metrics

### Time to First Useful Context (TTFUC) — Primary

End-to-end time from question to assembled context package ready for LLM consumption.

> This is Ferret's core value proposition. Ferret intentionally stops at assembling high-quality context. The LLM is a downstream consumer.

### Context Compression Ratio

```
Corpus: 4.3M tokens
↓
Ferret context package: 12K tokens
↓
Compression ratio: 0.28% (99.72% compression)
```

This is a flagship publishable number.

---

## Benchmark Report Format

Every benchmark run produces a versioned report in `docs/benchmarks/<release>/`:

```
## Objective
## Environment (CPU, RAM, repo size, model)
## Methodology (repeatable steps, pinned commit hashes for real corpora)
## Raw Measurements
## Derived Metrics (averages, percentiles, speedups)
## Observations
## Future Optimization Opportunities
```

**Historical trend table** (`docs/benchmarks/history.md`):

| Version | Index (1K) | Search P50 | TTFUC | Compression |
| ------- | ---------- | ---------- | ----- | ----------- |
| RC1     |            |            |       |             |
| RC2     |            |            |       |             |
| V1      |            |            |       |             |

---

## Repository Structure

```
tests/Ferret.Benchmarks/
    Platform/
        IndexBenchmarks.cs
        SearchBenchmarks.cs
        ContextAssemblyStageBenchmarks.cs   ← stage-level timing
        TestCorpusGenerator.cs
    Scale/
        ScaleIndexBenchmarks.cs
    Quality/
        ContextQualityRunner.cs
        ContextQualityReport.cs
        EvalDataset/
            eval-dataset.json
    ContextEffectiveness/                   ← renamed from AI/
        ContextEffectivenessRunner.cs
        ContextEffectivenessReport.cs
        Prompts/
            benchmark-prompts.json
    Reports/
        BenchmarkReporter.cs
    BenchmarkSetupBase.cs
docs/
    benchmarks/
        RC1/
            BENCHMARK-001-RC1.md
            quality-YYYY-MM-DD.json
            context-effectiveness-YYYY-MM-DD.json
            real-corpora/
        history.md
        PRODUCTIVITY-EVAL-PROTOCOL.md
benchmarks/
    run-benchmarks.ps1
    corpora/
        ferret/     ← symlink or clone path
        aspnet/     ← optional
```

---

## Published KPIs

### Platform
- Index throughput (files/sec)
- Search P50/P99 latency
- TTFUC P50/P99
- Context assembly stage breakdown
- Incremental index latency

### Quality
- Search Precision@10
- Search Recall@10
- nDCG@10
- Success@1, @5, @10
- MRR
- Duplicate elimination rate

### Context Effectiveness
- Token reduction %
- **Context compression ratio** (flagship)
- Documents surfaced (Ferret vs full-repo)

### Engineering Productivity (human eval)
- Time to first correct answer
- Prompts to completion
- Developer satisfaction (1–5 scale)

---

## Out of Scope (this implementation)

- Cursor vs Cursor+Ferret comparison (requires external tooling)
- Web benchmark dashboard (future)
- Live LLM automation of productivity benchmarks (requires LLM-in-the-loop harness)
- Federation benchmarks (V2+)
- Host startup benchmarks (deferred until hosts stabilize)
