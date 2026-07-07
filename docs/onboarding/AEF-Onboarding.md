# Ferret — AEF Onboarding Guide

> Produced by AEF's first-time onboarding process (2026-07-07), reading this repository with no prior context and no human guidance beyond the repo itself. This is a discovery document, not a design document: it describes Ferret **as it actually is today**, including its rough edges. Companion documents: `Repository-Inventory.md`, `Architecture-Inventory.md`, `Technology-Inventory.md`, `Documentation-Inventory.md`, and `AEF-Onboarding-Validation.md` (confidence levels, open questions, and what a human needs to resolve).

## 1. Executive Summary

Ferret is an open-source, MIT-licensed .NET 9 CLI platform — self-described as "the AI Workspace Operating System" — that gives AI coding assistants persistent, structured, repository-local knowledge of a codebase, indexed locally and queried via a CLI or an MCP server. It ships as a single self-contained native binary, distributed via GitHub Releases and mirrored through an npm wrapper (`@indoulia/ferret`). The codebase is ~1,189 indexed files (mostly C#, 28 `src/` projects, 34 test projects), under active, fast-moving development (three releases in three days during the most recent sprint burst observed). Architecture, decision-making, and release process are all unusually well-documented for a pre-1.0 project — but the documentation has real, confirmed drift from the code and from itself, some of it deliberately left in place as a test fixture for this very onboarding exercise (see §11 and `AEF-Onboarding-Validation.md`).

## 2. Product Overview

Ferret's own product doc states its identity directly:

> "Ferret is **the AI Workspace Operating System** — a platform that structures, governs, and persists AI-assisted engineering activity within a software repository." (`docs/001-Product/PRD-001.md:58`)

It indexes source code, Markdown, JSON, CSV/TSV, PDF, Word, and Excel files via a dependency-isolated parser pack, and exposes that index to AI agents through both a CLI (`ferret index`, `ferret search`, `ferret context`, `ferret watch`, `ferret doctor`) and an MCP server over stdio (`ferret serve`). Seven defining characteristics are stated in the PRD: AI-agnostic, repository-first, specification-driven, plugin-first, enterprise-ready, open-source, local-first. It explicitly disclaims being an IDE, source-control system, issue tracker, build server, or AI model itself.

The underlying platform concept is called **"ContextOS"** — the ambition of a living knowledge graph of an engineering org's decisions, history, and relationships, of which the `.ferret/` workspace state in this repo is the first concrete implementation. Ferret was previously named **"AISpace"** before a rename in Sprint 5 (ADR-0005); "Ferret (ContextOS)" is how `CHANGELOG.md` still refers to the product.

Target users (`docs/000-Overview/Mission.md`): individual engineers (repo Q&A, spec drafting, AI-assisted review), engineering teams (shared knowledge base, standards enforcement, traceability), and platform/toolchain owners (custom plugins, compliance/access control). There is explicitly no commercial entity behind it and no stated monetization model — sustainability is intended to come from community/enterprise contribution.

**A note for anyone confused by directory names**: there is a `.tokensave/` directory in this repo, and a separately-installed `tokensave` CLI (a general-purpose code-intelligence tool, "semantic graph queries instead of file reads") was used extensively to produce this very onboarding package. **Ferret and tokensave are two distinct, unrelated products.** Ferret's own dogfooding logs mention TokenSave's MCP server running as a sibling process during development sessions on this repo — the team dogfoods a third-party tool alongside their own. See the Glossary (§15) and `Architecture-Inventory.md` if this distinction matters to your task.

## 3. Repository Layout

Full detail in `Repository-Inventory.md`. In brief: `src/` (28 production .NET projects), `tests/` (34 test projects), `docs/` (numbered `0NN-*` topic folders, ADRs, roadmap, governance, archive), plus `samples/`, `templates/`, `packaging/`, `scripts/`, `build/`, and a Node-based `Ferret.Npm/` distribution wrapper. Three separate local-state directories exist side by side and are easy to conflate: `.ai/` (generic AI-agent session/memory scaffold), `.ferret/` (Ferret indexing itself, i.e. dogfooding), and `.tokensave/` (the unrelated third-party tool's index).

## 4. Major Components

| Component | Project(s) | Role |
|---|---|---|
| Core | `Ferret.Core` | Domain contracts/value objects, zero dependencies (enforced by a build-time rule) |
| Runtime | `Ferret.Runtime` | Module lifecycle/DI composition host — **not** the seven-engine host the architecture doc describes (§5) |
| CLI | `Ferret.Cli` | The `ferret` executable and all commands |
| MCP | `Ferret.Mcp` | MCP server/tools for AI-agent integration |
| Connectors / Parsing / Indexing / Search | `Ferret.ConnectorPlatform`, `Ferret.Connectors.Filesystem`, `Ferret.ParserPlatform`, `Ferret.Parsers*`, `Ferret.Indexing`, `Ferret.Search` | The discover → parse → index → query pipeline |
| AI | `Ferret.AI`, `Ferret.Models`, `Ferret.Prompts`, `Ferret.Providers.{Ollama,OpenAi}` | Context assembly and model-provider abstraction |
| Workspace / Federation | `Ferret.Workspace`, `Ferret.Workspace.Graph`, `Ferret.Knowledge.Federation` | Single-repo and multi-repo ("v2") workspace model, federated cross-workspace queries |
| Persistence (v2) | `Ferret.Persistence` | Dependency-graph persistence for the v2 mechanism layer |
| Plugins | `Ferret.Plugins`, `Ferret.Plugin.SDK` | Extension host and third-party plugin contracts |

Full module table and code-vs-docs cross-check in `Architecture-Inventory.md`.

## 5. Architecture Overview

Canonical doc: `docs/002-Architecture/ARCH-001.md` (Status: **Draft**, Pending Architecture Review). It describes a 5-layer model (Presentation → Application → Domain → Infrastructure → Plugin) and seven "engines" (Workspace, Knowledge, Index, Artifact, Memory, Review, Specification) living inside `Ferret.Runtime`. **In the real code, `Ferret.Runtime` is a thin lifecycle host, and four of those seven engines (Artifact, Memory, Review, Specification) have no implementation anywhere in `src/` at all** — a gap independently confirmed against the code and also stated plainly in the docs themselves (`ARCH-024-Artifact-Inventory.md §Critical Findings #3`). Capability that does exist lives in independent top-level projects rather than as Runtime sub-modules.

A second track, "Ferret V2" (`ARCH-023`–`ARCH-037`), adds a frozen "mechanism layer" for AI-derived-artifact reuse on top of V1. This program produced `Ferret.Persistence`, `Ferret.Workspace.Graph`, and `Ferret.Knowledge.Federation` — all real and merged to `main`, none yet reflected in ARCH-001's module list.

Architecture-fitness is partly enforced at build time: `Directory.Build.targets` fails the build if `Ferret.Core` gains a project reference (ARCH001) or if `Ferret.Runtime` references `Cli`/`Mcp` (ARCH002); `Ferret.Architecture.Tests` adds further dependency-direction regression coverage. `tokensave`-based analysis found the code graph sparse and well-partitioned (DSM density 0.002), no severe god classes, and no real circular dependencies (one candidate was investigated and confirmed to be a tool false positive). Full detail, including the three-location ADR-numbering finding, is in `Architecture-Inventory.md`.

## 6. Technology Stack

C# / .NET 9, `System.CommandLine` for the CLI, SQLite for storage, `OpenAI`/`OllamaSharp`/`ModelContextProtocol` for AI integration, `DocumentFormat.OpenXml`/`PdfPig`/`Markdig` for document parsing. Central Package Management via `Directory.Packages.props`. Distributed as a self-contained native binary for four platforms (win-x64, linux-x64, osx-x64, osx-arm64), wrapped by a thin npm launcher (`@indoulia/ferret`). Full stack, dependency notes (including one prerelease-pinned package and one deliberate CPM exception), and packaging detail in `Technology-Inventory.md`.

## 7. Build and Test Process

Build: `dotnet build src/Ferret.sln`. Test: `dotnet test src/Ferret.sln` (no category filtering in CI, despite `CONTRIBUTING.md` documenting a filtered-run workflow). CI (`.github/workflows/ci.yml`) runs the full solution's tests on Ubuntu and Windows on every push/PR, plus a `dotnet format --verify-no-changes` check; it collects coverage but does not gate on any threshold. `docs/009-Testing/README.md` states coverage targets (Core ≥90%/85% line/branch, etc.) that a `tokensave`-based call-graph coverage signal suggests are **not currently being met** (17% for `Ferret.Core`, 6% for `Ferret.Search` — though this tool measurably undercounts real coverage in at least one verified case, so treat these as a floor). Integration tests run in-process against real SQLite/filesystem, not via Docker Compose as `CONTRIBUTING.md` describes. Full detail in `Technology-Inventory.md`.

## 8. Development Workflow

Fork/branch off `main` with `feat/`/`fix/`/`docs/`/`refactor/`/`test/` prefixes, Conventional Commits, `dotnet format` clean, zero warnings (`TreatWarningsAsErrors=true`), ≥80% coverage target per `CONTRIBUTING.md`, PR review from at least one maintainer, squash-merge. `.github/CODEOWNERS` names `@indoulia` as owner on almost every path pattern — in practice a single-maintainer review model today, despite path-based rules suggesting differentiated ownership. Architecture decisions "must be recorded as an ADR in `docs/adr/`" and the PR template has a checklist item for this.

## 9. Governance

Ferret has an unusually explicit, multi-layered governance model for a project this size:

- **ADRs** (`docs/adr/`) for individual architectural decisions — Proposed → Accepted on merge → Deprecated/Superseded.
- **Governance Reviews ("AGR-" records)** (`docs/Reviews/`) for auditing a *series* of architecture documents as one system, closing/freezing sets of decisions at once (e.g. AGR-001 froze 9 decisions across ARCH-023–027 and explicitly states "reopening any of them requires a new governance review, not an inline edit").
- **A narrative Decision Log** (`docs/013-Governance/DECISION-LOG.md`) — the human-readable companion to the ADR index, covering every sprint decision including Rejected/Deferred ones.
- **A `.ai/` role-based agent charter** (ChiefArchitect, SecurityArchitect, etc. in `.ai/agents/`) defining authority/veto rights, feeding the ADR/decision-log workflow.

Notable governance concepts already present in Ferret's own vocabulary, functionally similar to AEF's own governance patterns even though the exact terms differ: **governance gates** (ADR-0025: no work — docs or code — may be committed during an active gate until an explicit governance decision authorizes it, though ADR-0025 itself is still Status: Proposed despite being applied in practice), **decision/architecture freezes** (ADR-0012 froze the Milestone-1 public surface; ADR-0030 froze a set of architectural invariants), and **closed decisions requiring a new review to reopen** (AGR-001). Full inventory, including a workflow doc (`CreateADR.md`) that references a `Decision-Register.md` file that does not exist in the repo, is in the governance section of the discovery notes folded into `Architecture-Inventory.md` and `AEF-Onboarding-Validation.md`.

## 10. Active Roadmap

Two documents claim to represent "the roadmap" and disagree with each other and with reality — see §11. The substantive, current program is the **Workspace Intelligence Platform ("v2")** (`docs/roadmap/Workspace-Intelligence/`), which turns Ferret from a single-repo tool into a federated multi-workspace platform (workspace registry, live cross-workspace queries that are never copied/re-indexed, usage-ledger retention, and a deferred sharing/RBAC layer). Four ADRs govern it: ADR-0026–0028 are Accepted; **ADR-0029 (sharing/RBAC scope) remains Proposed, pending a Founder decision**, but only blocks out-of-scope future work, not the current release. `docs/roadmap/Future/Deferred-Scope.md` catalogs explicitly-deferred items (enterprise scale, full RBAC, billing, cross-workspace conflict resolution) each tied to a named open question rather than silently dropped.

## 11. Current Engineering State

**Read this section before trusting any "current sprint" claim elsewhere in the repo.** Git history on `main` shows the entire Workspace Intelligence Platform (Phases 0–3, Epic 5, T1–T14) is merged and shipped, followed by a second dogfooding wave that found and fixed 9 more real bugs in the MCP surface, and finally an ADR-0030 architecture-conformance governance commit. Yet:

- `docs/000-Overview/PROJECT-STATE.md` says "Current sprint: Sprint 13 — Context Assembly (Not yet started)."
- `docs/001-Product/ROADMAP-001.md` says "Current Sprint — Sprint 10 — Information Retrieval."
- `.ai/session.md` / `.ai/current-context.json` are reset to an empty/pristine state.
- None of the three mentions the Workspace Intelligence Platform, ADR-0026–0030, or Epic 5 at all.

**This is intentional, not an accident to fix.** The repository's own most recent commit (`904d05f`, "chore: strip AI-session operational artifacts for fresh AEF onboarding") states in its message that it deliberately left this exact contradiction — along with the ADR-location fragmentation, overlapping future-vision docs, and stale Sprint-0 placeholder READMEs — "intentionally preserved as real-world signal for the upcoming fresh-onboarding validation run." In other words: this repository was staged, on purpose, to test whether an onboarding process (like this one) can detect drift between what the docs claim and what git/the code actually shows. Treat `git log` and the `docs/roadmap/Workspace-Intelligence/` series as ground truth over `PROJECT-STATE.md`/`ROADMAP-001.md`/`.ai/session.md` until a human reconciles them.

Also note: the `v2/workspace-intelligence-platform` branch and its worktree at `.worktrees/v2-workspace-intelligence` are **not** an active parallel effort — verified via `git log` ancestor checks, that branch is a strict ancestor of `main`; its content has already been fully merged.

## 12. Key Documents to Read

In order, for a new engineer or agent:

1. `docs/000-Overview/Mission.md` and `Vision.md` — why Ferret exists (trust these; not affected by the staleness in §11).
2. `docs/002-Architecture/ARCH-001.md` §1–14 — architecture, read alongside `Architecture-Inventory.md` for where it diverges from the real code.
3. `docs/adr/README.md` — decision history, read alongside `Architecture-Inventory.md`'s ADR-location finding.
4. `docs/roadmap/Workspace-Intelligence/README.md` — the real current program (trust this over PROJECT-STATE.md/ROADMAP-001.md).
5. `docs/013-Governance/DECISION-LOG.md` — current and well-maintained narrative of what's actually been decided.
6. `CONTRIBUTING.md` and `SECURITY.md` — contribution and disclosure process.
7. This package's four companion inventories, and `AEF-Onboarding-Validation.md` for what to independently re-verify before relying on any of the above.

**Do not start from** `docs/000-Overview/PROJECT-STATE.md` or `docs/001-Product/ROADMAP-001.md` for "what's happening now" — both are confirmed stale (§11).

## 13. First-Day Setup

1. Clone the repo; you'll need the .NET 9 SDK.
2. `dotnet restore` then `dotnet build src/Ferret.sln` (or `build/Build.ps1` / `build/Build.sh`).
3. `dotnet test src/Ferret.sln` to run the full suite (no category filter is wired up in CI, despite CONTRIBUTING.md describing one — see `Technology-Inventory.md`).
4. To try the CLI itself: `dotnet run --project src/Ferret.Cli -- <command>`, or `publish.ps1`/`package.ps1` to produce a real self-contained binary and `install.ps1` to put it on PATH.
5. `dotnet format src/Ferret.sln --verify-no-changes` before opening a PR (CI enforces this).
6. If proposing an architectural change, read `docs/adr/README.md` and `docs/adr/0000-template.md` first — ADRs are a hard expectation here, not a nicety.

## 14. Common Pitfalls

- **Don't trust `PROJECT-STATE.md`, `ROADMAP-001.md`, or `.ai/session.md` for current status** — all three are known-stale as of this onboarding pass (§11). Cross-check against `git log` and `docs/roadmap/Workspace-Intelligence/`.
- **Don't assume `docs/adr/` is the only decision record** — `docs/roadmap/Workspace-Intelligence/ADR/` (documented) and `docs/002-Architecture/decisions/` (undocumented, different ID format) also hold real, Accepted decisions.
- **Don't confuse `.ai/`, `.ferret/`, and `.tokensave/`** — generic AI-agent scaffold, Ferret's own dogfooding index, and an unrelated third-party tool's index, respectively.
- **Don't take `docs/010-Security/README.md`'s scanning table at face value** — CodeQL and OWASP Dependency Check are documented as active but are either explicitly disabled (GitHub Advanced Security requires a non-free plan on a private repo) or never configured.
- **Don't assume the stated test-coverage targets are met** — no CI gate enforces them, and the best available signal suggests they currently aren't (with the caveat that the measurement tool itself undercounts).
- **A handful of doc links are dangling** post-cleanup (`SECURITY.md`, `templates/README.md`, `templates/release.md`, `docs/002-Architecture/README.md` — see `Documentation-Inventory.md`) — don't assume every markdown link in the repo resolves.
- **`ADR-0025` (governance-gate rule) is still Status: Proposed** even though later work treats it as settled practice — check its current status before citing it as binding.

## 15. Glossary

| Term | Meaning |
|---|---|
| **Ferret** | This repository's product: an "AI Workspace Operating System" CLI + MCP server for AI-assisted engineering. Formerly named "AISpace." |
| **ContextOS** | The broader platform concept/ambition Ferret is the first implementation of (a living knowledge graph of engineering activity). |
| **tokensave** | A separate, third-party code-intelligence CLI/MCP tool (unrelated product) that the Ferret team dogfoods during their own development sessions on this repo. Not part of Ferret. |
| **`.ai/`** | Repository-local AI-agent session/memory/role scaffolding (generic convention, not Ferret product code). |
| **`.ferret/`** | Ferret's own workspace state from indexing this repo with itself. |
| **`.tokensave/`** | The unrelated `tokensave` tool's local index database for this checkout. |
| **Dogfooding** | Using the real, built `ferret` binary against real repositories (including this one) to find genuine defects before/after each change, always followed by a TDD fix — an explicit, logged practice (`docs/archive/dogfooding/`). |
| **AC-NNN** | "Architecture Constraint" identifiers referenced by ADR-0030 and the architecture-fitness build targets (e.g. AC-001, AC-012). |
| **ARCH-NNN** | Numbered architecture documents in `docs/002-Architecture/`. |
| **ADR-NNNN / ADR-NNN** | Architecture Decision Records — two numbering formats exist across three locations; see §9 and `Architecture-Inventory.md`. |
| **AGR-NNN** | Architecture Governance Review — a governance checkpoint over a *series* of ARCH documents, in `docs/Reviews/`. |
| **Governance gate** | A period (e.g. DOGFOOD-001) during which no work may be committed without an explicit prior governance decision (ADR-0025). |
| **AEF** | The external "AI Engineering Framework" this onboarding package was produced by/for. Ferret has no dependency on it; this repo is used as a validation fixture for it. |
