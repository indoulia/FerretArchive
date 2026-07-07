# Architecture Inventory

> Part of the AEF first-time onboarding package for Ferret. Grounded in the actual code graph via the `tokensave` code-intelligence CLI (already indexed for this repo) cross-checked against `docs/002-Architecture/`, `docs/adr/`, `docs/Reviews/`, and `docs/roadmap/Workspace-Intelligence/`. Discovery only — no drift called out below has been fixed.

## Architecture Overview

The canonical architecture document is **`docs/002-Architecture/ARCH-001.md`** (Status: Draft, Review Status: Pending Architecture Review, v1.0, 2026-06-27). It defines:

- A 5-layer model: Presentation → Application → Domain → Infrastructure → Plugin.
- A "small core + plugin-first" philosophy, with 10 architectural goals (AG-001…AG-010): minimal core, dependency inversion, plugin isolation, deterministic behavior, repository-local state, human-review-cannot-be-bypassed, and others.
- Seven modules as separate .NET projects: `Ferret.Core`, `Ferret.Runtime`, `Ferret.Plugins`, `Ferret.Configuration`, `Ferret.Telemetry`, `Ferret.Mcp`, `Ferret.Cli`.
- Seven domain "engines" described as living **inside** `Ferret.Runtime`: Workspace, Knowledge, Index, Artifact, Memory, Review, Specification.

A second, later architecture track — **"Ferret V2"** (`ARCH-023` through `ARCH-037`) — layers a "mechanism layer" for AI-derived-artifact reuse/validity on top of V1 without altering it. ADR-0021 declared the V2 conceptual+mechanism baseline "complete and frozen" (2026-07-03). ADR-0030 (2026-07-06) declared an "Architecture Conformance Baseline" after a 4-round review that found and fixed two `Ferret.Core` purity violations (`GitHeadResolver`, `SearchHit.SourceWorkspaceId`).

**A correction already recorded in the docs, independently confirmed against code**: ARCH-001/early ARCH-023 versions state platform state lives under `.ai/`. **ARCH-024 (Critical Findings §1)** corrects this — the real root is `.ferret/` (`src/Ferret.Workspace/WorkspaceLayout.cs:7`, `RootDirectoryName = ".ferret"`). Confirmed via `tokensave tool god_class`, which shows `WorkspaceLayout` as a real, populated constants class.

## Major Components / Subsystems

| Area | Real project(s) | Owning doc |
|---|---|---|
| Core contracts | `src/Ferret.Core` (~1,414 public symbols — by far the largest surface) | ARCH-001 §7 |
| Runtime/module host | `src/Ferret.Runtime` (only ~104 public symbols — lifecycle/registry/health infra only) | ARCH-001 §7 |
| CLI | `src/Ferret.Cli` | ARCH-001 §6 |
| MCP | `src/Ferret.Mcp` | ADR-0017 (docs/005-MCP is a stub) |
| Workspace | `src/Ferret.Workspace`, `src/Ferret.Workspace.Graph` | ARCH-020 — **Status: Reserved, not yet authored** (docs/003-Workspace is a stub) |
| Connectors/Discovery | `src/Ferret.ConnectorPlatform`, `src/Ferret.Connectors.Filesystem` | ADR-0013, ARCH-019 (Accepted) |
| Parsing | `src/Ferret.ParserPlatform`, `Ferret.Parsers`, `Ferret.Parsers.Office`, `Ferret.Parsers.Pdf` | ADR-0014, ARCH-018 |
| Indexing / Search | `src/Ferret.Indexing`, `src/Ferret.Search` | ADR-0015 |
| Knowledge federation | `src/Ferret.Knowledge.Federation` | **Not named in ARCH-001 at all** — postdates the ARCH-001 baseline |
| AI / Models / Prompts | `src/Ferret.AI`, `Ferret.Models`, `Ferret.Prompts`, `Ferret.Configuration.AI`, `Ferret.Providers.Ollama`, `Ferret.Providers.OpenAi` | ADR-0019, ADR-0020, ARCH-021 (Draft) |
| Persistence (V2) | `src/Ferret.Persistence` | ADR-0022/0023/0024, ARCH-026/032 |
| Plugin host/SDK | `src/Ferret.Plugins`, `src/Ferret.Plugin.SDK` | ARCH-001 §11, ADR-0011 |
| Distribution | No matching `src/` project (`Ferret.Manual`, `Ferret.VerticalSlice` exist but aren't documented) | ARCH-022 (Draft) |
| Analytics | No matching `src/` project | ARCH-018-Analytics-Architecture.md — "Reserved, not yet implemented" |
| Storage (dedicated abstraction) | None — storage lives inside `Ferret.Persistence`/`Ferret.Indexing` | ARCH-017-Storage-Architecture.md — "Reserved, not yet implemented" |
| Application layer | No `src/` project (intentional) | ADR-0018 "Application Layer Reserved" — consistent, not drift |

## ADR Inventory & the ADR-Location Finding

**Three ADR/decision locations exist in this repo. `docs/adr/` is the authoritative one; the other two are undocumented in its index.**

1. **`docs/adr/`** — 18 numbered ADRs (`0001`, `0005`, `0011`–`0025`, `0030`; `0000` is the template), indexed in `docs/adr/README.md`. This is the location ADR-0001 itself mandates: *"Format: Markdown, stored in `docs/adr/`. Naming: `NNNN-kebab-case-title.md`."* 16 Accepted, 1 Proposed (**ADR-0025**, still Proposed despite already being applied in practice — see `AEF-Onboarding-Validation.md`), 1 Reserved (0018). Numbering gaps at `0002–0004` and `0006–0010` are unexplained in the index; `0026–0029` are explicitly cross-referenced as living separately under `docs/roadmap/Workspace-Intelligence/ADR/`.
2. **`docs/roadmap/Workspace-Intelligence/ADR/`** — contains ADR-0026 (workspace registry model, Accepted), ADR-0027 (federation strategy — live queries, never copies, Accepted, non-negotiable), ADR-0028 (usage-ledger 90-day retention, Accepted), ADR-0029 (v1 sharing/RBAC scope — **Proposed, requires Founder decision**, blocks only out-of-scope Phase 5). This location is at least cross-referenced from both `docs/adr/README.md` and the roadmap README, so it is *documented fragmentation*, not a silent gap — but the ADR sequence visually breaks across two folders with no single combined index.
3. **`docs/002-Architecture/decisions/`** — only 2 files, **neither indexed anywhere in `docs/adr/README.md`**:
   - `ADR-004-runtime-engine-container.md` — a real, well-formed, Accepted decision (Sprint 4, 2026-06-28) using a different ID format (`ADR-004` vs `0004`) than the canonical location, filed **one day after** `docs/adr/0001` made `docs/adr/` mandatory. Its number falls inside the "missing" `0002–0004` gap in the canonical sequence — plausible (unconfirmed) explanation for that gap.
   - `sprint-3-technology-evaluation.md` (2026-06-27, Approved) — a build-vs-buy evaluation that overlaps in scope with `docs/002-Architecture/TECH-001-Technology-Evaluation.md` (2026-06-28) with no cross-reference or supersession note between the two.

   ADR-0030's 2026-07-06 conformance review explicitly closed ADR-index drift in its Round 3, but did not touch or mention this stray folder — it remains live and undiscoverable via the canonical index today.

No **conflicting decisions** were found across the three locations — only a process/discoverability gap.

## Code-vs-Docs Consistency Check

Verified with `tokensave tool module_api`, `files`, `circular`, `dsm`, `god_class`, `hotspots`:

1. **`Ferret.Runtime` does not host ARCH-001's seven engines.** Its ~104 public symbols are generic module-lifecycle/host infrastructure (`Bootstrap/`, `Registry/`, `Lifecycle/`, `Health/`, `Events/`) — matching `ADR-004-runtime-engine-container.md`'s "Runtime = composition root, not a container for domain engines" framing, but contradicting ARCH-001 §7.2's description. In reality, capability that exists lives in independent top-level projects (`Ferret.Workspace`, `Ferret.Indexing`, `Ferret.Search`, `Ferret.ConnectorPlatform`, etc.).
2. **Four of ARCH-001's seven named engines have no implementation at all.** Review Engine, Specification Engine, Artifact Engine, and Memory Engine (as ARCH-001 describes them) correspond to no real engine code anywhere in `src/` — independently confirmed both by `tokensave` file search and by the docs themselves: **`ARCH-024-Artifact-Inventory.md` §Critical Findings #3** states this outright. "Memory" only exists as an AI-context-memory concept (`IConversationMemory`/`ITaskMemory`/`IWorkspaceMemory` in `Ferret.Core.Ai`), not the repository-memory engine ARCH-001 describes.
3. **No AI-derived artifact is live in production.** `ARCH-024 §Critical Findings #2`: `IModelRouter` has no caller anywhere in `src/`, consistent with ADR-0021's note that the current MVP path never invokes `IModelProvider`.
4. **Both architecture indexes are stale relative to their own folders.** `docs/002-Architecture/README.md`'s table omits ARCH-017, 018, 019, 020, 021, 022, and 037, all of which exist as files in the same directory. `ARCH-020-Workspace-Architecture.md` is itself a stub ("Reserved — not yet authored") despite `Ferret.Workspace`/`Ferret.Workspace.Graph` being fully implemented; `docs/003-Workspace/`, `docs/004-Database/`, `docs/005-MCP/` are likewise one-paragraph stubs despite their subsystems being real and populated.
5. **`Ferret.Knowledge.Federation` and `Ferret.Workspace.Graph`** are real, populated projects not mentioned anywhere in ARCH-001's module list — they postdate the ARCH-001 baseline and arrived via the Workspace Intelligence Platform (v2) program; no ARCH document has been updated to formally cover them yet.
6. ADR-0030 states its audit covered "all 41 projects"; the repo today has 62 `.csproj` files (28 `src` + 34 `tests`). Plausibly just growth in the day since the baseline was taken (not necessarily a contradiction) — worth re-verifying before relying on that conformance claim as current.

## Notable Structural Risks

- **Circular dependencies** (`tokensave tool circular`, 3 found): one intra-`Ferret.Cli` cycle (`FerretContext` ↔ `RootCommandFactory` ↔ `FerretConfigLoader`) — a real internal-coupling smell, but confined to one module, not a cross-project violation. One `Ferret.Core.Primitives.SemanticVersion` ↔ `Ferret.Prompts.PromptRegistry` cycle was investigated and appears to be a **tool false positive** — `PromptRegistry` only uses `System.Version`, not `Ferret.Core`'s type, and `Ferret.Core` has zero outgoing project references on manual audit (confirming ARCH001's build-time guarantee holds). One low-significance test-only cycle.
- **DSM** (`tokensave tool dsm --path src`): 586 files, 685 edges, density 0.002, 108 clusters, largest cluster 31 files — sparse and well-partitioned.
- **God classes** (`tokensave tool god_class`, top by member count, none extreme): `SqliteKeywordIndexEngine` (23), `FileDependencyStateStore` (22), `WatchCommandHandler` (21), `FileWorkspaceRegistry` (20), `FederatedKnowledgeStore` (20). The first two are storage/persistence-boundary classes central to the V2 persistence mechanism — worth watching as that subsystem grows.
- **Hotspots** (`tokensave tool hotspots`): dominated by expected utility/DTO chokepoints (`PromptVariables.Contains`, `ModuleDescriptorStore.Add`, `ConsoleFormatter.WriteLine`, `WorkspaceStateDto.Connectors`) rather than any single overloaded business-logic class.

## Gaps / Unknowns

- Whether `docs/002-Architecture/decisions/` is an intentionally-preserved historical artifact or simply forgotten is not stated anywhere.
- The permanent ADR numbering gaps (`0002–0004`, `0006–0010`) are not explained in any doc; the `ADR-004` finding above is a plausible but unconfirmed partial explanation.
- `docs/Reviews/AGR-001`–`AGR-004` (the actual approval mechanism referenced repeatedly by ADR-0021/0030 and ARCH-023–030) were not deep-read in this architecture pass — see `AEF-Onboarding-Validation.md` and the Governance findings folded into `AEF-Onboarding.md` §9.
- `ARCH-037-Dependency-Graph-Mechanism.md` (newest ARCH doc, Draft) is not yet reflected in the V2 roadmap index or `002-Architecture/README.md`; its relationship to ARCH-033 was not fully traced.
- `ARCH-001.md` is 2,148 lines; only §1–14 plus targeted cross-checks were reviewed. A deeper pass on its Security/Telemetry/deployment sections (§15+) would be needed for full verification.
