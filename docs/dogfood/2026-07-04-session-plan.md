# Ferret Dogfooding Session Plan — 2026-07-04

| Field | Value |
|---|---|
| **Governs** | DOGFOOD-001 Phase 1 (Personal Workflow) + Phase 6 (Benchmark Collection) |
| **Governance basis** | `docs/DOGFOOD-001.md` (Active), `docs/adr/0025-uncommitted-work-during-active-governance-gate.md` |
| **Rule in force** | Evidence before architecture: fix behavior first; any architecture change must be justified by evidence gathered here, not the other way around |
| **Install source** | Published npm only — `@indoulia/ferret@0.16.0` (verified installed; source builds are not valid dogfooding evidence) |
| **Scope** | Observation and bug-finding only. No new feature, no architecture change, no fix beyond what a filed bug strictly requires. |

## Known blocker — surfaced honestly, not worked around

The request to "copy PDFs from App1 and App2 in the POC folder" cannot be completed as given: `C:/POC` does not contain folders named `App1` or `App2`. Its actual contents are: `AI`, `Certs`, `FITrust`, `Ferret`, `FerretTest`, `NetCore`, `SecureIntent`, `garuda-product-strategy`, `indoulia-foundation`, plus `FinT.zip`. None is an obvious match. This workstream is **blocked pending the correct path** — it will not be guessed at.

## Workstreams

1. **Test suite baseline** — `dotnet test src/Ferret.sln`. Status: done, recorded below.
2. **Component/unit test coverage gap survey** — identify under-tested modules (unit/component level, not just E2E) using TokenSave's coverage tooling. Output is a punch list of gaps, not fixes.
3. **Manual CLI dogfooding on this repo** — `ferret --version`, `ferret doctor`, `ferret index`, `ferret search`, `ferret watch`, `ferret ask`/`serve` if available. Record real latency, correctness, and friction — not summarized impressions.
4. **Sample-data dogfooding** — **blocked**, see above.
5. **Benchmark capture** (DOGFOOD-001 Phase 6 table) — index throughput, full-index time, incremental re-index time, search latency (median/p95), cold-start time, index size on disk. Peak memory / context-assembly time captured only if measurable without adding new tooling.
6. **Issue logging** — every genuine defect found → GitHub Issue on `indoulia/Ferret`, `dogfood` label, DOGFOOD-001 severity rubric (Critical/High/Medium/Low), linked back to this log. No batching cosmetic noise into false-severity issues.
7. **Daily log** — `docs/dogfood/2026-07-04-daily-log.md`, DOGFOOD-001 template, plus an explicit **Went Well / Didn't Go Well / Needs Improvement / Missing** section — no glossing over failures.

## Cost discipline

Cheapest capable model for mechanical/bulk steps (running commands, formatting logs). No large open-ended multi-agent fan-outs for this pass — it is observational, not a full audit. Escalate only for real root-cause triage of a confirmed bug.

## Execution order

1. Component test-coverage gap survey
2. Manual CLI dogfooding on this repo, with real timings
3. Benchmark table population
4. Daily log write-up (including Went Well / Didn't / Improve / Missing)
5. File GitHub issues for confirmed defects
6. App1/App2 sample-data pass — once the correct path is confirmed by the user

## Test Suite Baseline — 2026-07-04

`dotnet test src/Ferret.sln --nologo -v minimal` — **all green, 0 failures.**

| Test project | Passed | Skipped | Total |
|---|---|---|---|
| Ferret.Plugins.Tests | 1 | 0 | 1 |
| Ferret.Telemetry.Tests | 1 | 0 | 1 |
| Ferret.Models.Tests | 15 | 0 | 15 |
| Ferret.Providers.Ollama.Tests | 31 | 0 | 31 |
| Ferret.Indexing.Tests | 50 | 1 (perf benchmark) | 51 |
| Ferret.Search.Tests | 70 | 0 | 70 |
| Ferret.Providers.OpenAi.Tests | 45 | 4 (live-API tests) | 49 |
| Ferret.E2E.Tests | 31 | 0 | 31 |
| Ferret.Parsers.Tests | 2 | 0 | 2 |
| Ferret.Parsers.Pdf.Tests | 6 | 0 | 6 |
| Ferret.Mcp.Tests | 51 | 0 | 51 |
| Ferret.ConnectorPlatform.Tests | 42 | 0 | 42 |
| Ferret.Architecture.Tests | 31 | 0 | 31 |
| Ferret.Integration.Tests | 20 | 0 | 20 |
| Ferret.Cli.Tests | 195 | 0 | 195 |
| Ferret.Benchmarks.Tests | 8 | 0 | 8 |

Skips are pre-existing (performance benchmark opt-out, OpenAI live-API tests requiring credentials) — not new gaps introduced by anything in this session.
