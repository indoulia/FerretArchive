# Changelog

All notable changes to Ferret (ContextOS) are recorded in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

Nothing yet.

---

## [2.0.0] — Workspace Intelligence Platform — 2026-07-13

### Summary

Ferret v2.0 generalizes the workspace boundary past a single repository. A workspace is now
a queryable unit of knowledge that can reference other workspaces as read-only dependencies —
the same relationship a package has to its dependencies. Every existing single-repo workspace
keeps working unchanged; referencing another workspace is additive, not a migration. Full
customer-facing notes: `docs/012-Releases/v2.0.0.md`.

### Added

**Workspace registry**
- `ferret workspaces create/list/show/add-repo/remove-repo/remove` — identity-based (canonicalized
  git remote, with a documented no-remote/multi-remote fallback), backed by an atomic file-based
  registry (`IWorkspaceRegistry`).
- Zero-action migration: every existing single-repo workspace is auto-wrapped into the registry
  the first time any `ferret workspace` command runs after upgrading.

**Cross-workspace federation**
- `ferret workspaces add-reference/remove-reference` with cycle detection (DAG enforcement).
- Federated `ferret workspaces query` — correct, cited results spanning referenced workspaces
  with zero index duplication on disk.
- Reference pinning (`pin-reference`/`unpin-reference`), content-hash based, fails closed
  (excludes only the stale source) when a pinned reference's content changes.
- `ferret workspaces remove` for bulk cleanup of registered workspaces.

**MCP parity**
- `workspace_list` and `workspace_query` MCP tools — AI-agent clients get the same federation
  capability as the CLI.

**Performance**
- Federated query cache and workspace-reference topology cache, plus cross-workspace
  pull-based invalidation (state-hash mismatch at query time).
- Cross-source BM25 ranking normalization in `FederatedKnowledgeStore.Merge` — per-source
  min-max normalization so results from different workspaces rank comparably (fixes a
  confirmed quality defect where a large corpus's mid-tier hits outranked a smaller corpus's
  most relevant match).

**Observability**
- Structured `ILogger` events on the federation/cache query path (cache hit/miss, per-query
  duration, per-source skip).

### Fixed

- Federated query cache regression (P3-002) found via real multi-workspace dogfooding at scale,
  root-caused and fixed before release.

### Known Limitations

- Federated context scope-narrowing and compression (pre-Planner classifier, post-Scorer
  compressor) are deferred to v2.1 — federated queries return correct, cited results today;
  further token-cost reduction on very large multi-workspace corpora is not yet implemented.
- Usage analytics, dashboard, and cost/observability rollups are not part of this release.
- Multi-role sharing/RBAC is not part of this release, pending a Founder decision on ADR-0029.

---

## [0.16.0] — Enterprise Content Pack 1 — 2026-07-01

### Summary

Ferret now indexes enterprise documents. This release expands parser support from **3
formats to 7** — adding PDF, Word (.docx), Excel (.xlsx), and CSV/TSV — so a single index
covers source code, documentation, structured data, and enterprise documents. It also
**restores anonymous `npm install`**, which broke when the source repository was made
private. Full customer-facing notes: `docs/012-Releases/v0.16.0.md`.

### Added

**Multi-format indexing — 7 parsers (PlainText, Markdown, JSON, CSV, PDF, Word, Excel)**
- PDF, Word (`.docx`), and Excel (`.xlsx`) documents are now parsed and searchable, taking
  recognized formats from 3 to 7 (79 parseable extensions total).
- CSV/TSV — dependency-free, structure-aware parsing (`Data` kind), for Jira/ADO exports,
  RTMs, and backlogs.
- Excel uses a streaming reader for large workbooks, resolves shared strings, and reads
  cached cell values (no formula recomputation). Parsers extract text and lightweight
  metadata only — no chunking, tokenizing, embedding, summarization, or spreadsheet calc.
- Implemented as `Ferret.Parsers.Pdf` (PdfPig), `Ferret.Parsers.Office` (Open XML), a
  dependency-free `CsvParser`, and a `Ferret.Parsers` composition project (`ParserPackModule`).

**File classification**
- MIME resolution reclassifies PDF/DOCX/XLSX as binary-parseable; expanded code/config
  map and binary denylist; new `MediaCategory` (Text / BinaryParseable / BinaryOpaque).
- Filename resolution: extensionless files (`Dockerfile`, `Makefile`) are first-class.
- `ParserOptions.MaxExtractedCharacters` extraction cap (default unlimited; sets
  `Truncated` metadata when applied).

**Diagnostics — `ferret doctor` Parser Platform report**
- Lists installed parsers in registration order, extension coverage (parseable vs. opaque),
  and the parser packages providing each format.
- Global `--verbose` is now recursive; `ferret doctor --verbose` shows all opaque
  extensions plus per-parser priority, media type, and parseable MIME types.

**Benchmarking**
- Deterministic enterprise corpus generator and BenchmarkDotNet suite for parser
  throughput, dispatcher overhead, and large-workbook memory (`tests/Ferret.Benchmarks`).
- Reference results (laptop-class, indicative): PDF ~2,900 docs/sec, Word ~600 docs/sec,
  Excel ~122 docs/sec; dispatcher overhead negligible; 50k-row workbook ~326 MB peak
  working set. See `docs/benchmarks/parser-pack-1/README.md`.

### Fixed

- **Anonymous npm installation restored.** `npm install -g @indoulia/ferret` now downloads
  release binaries from the public `indoulia/ferret-dist` mirror instead of the (now-private)
  source repo — GitHub only serves release assets anonymously from public repos. Overridable
  via `FERRET_DIST_OWNER` / `FERRET_DIST_REPO` / `FERRET_DIST_RELEASE_ENDPOINT`. The release
  pipeline now verifies the download endpoint is anonymously reachable before a release
  completes, so a broken install can no longer ship.

---

## [0.15.0] — Distribution Platform — 2026-06-30

### Summary

Ferret is now distributed as a self-contained application, installable without cloning
the repository or installing the .NET SDK. Establishes GitHub Releases as the single
source of truth for all distribution channels.

### Added

- Self-contained cross-platform binaries (win-x64, linux-x64, osx-arm64, osx-x64).
- Versioned `release-manifest.json` distribution contract (`schemaVersion: 1`).
- NPM installation channel (`@indoulia/ferret`) with SHA256 verification and atomic
  install; uninstall preserves `.ferret` workspaces, indexes, and configuration.
- `release.yml` / `npm-publish.yml` pipeline (decoupled), `ARCH-022`.

---

## [0.14.0] — RC1 — 2026-06-29

### Summary

Ferret RC1 is the first production-ready release. After Sprints 8–13 delivered the
core platform (workspace engine, document parsing, keyword search, MCP server, context
assembly), Sprint 14 hardened it: file watching, incremental indexing, performance
benchmarks, diagnostics, a cross-platform installer, documentation, end-to-end tests,
and mandatory dogfooding. A developer can install Ferret, index a workspace, and have
a Claude Desktop integration running in under five minutes.

---

### Added — Sprint 14

**File Watching**
- `ferret watch` command: monitors workspace for file changes using `FileSystemWatcher`
  with 500 ms debounce, triggers incremental re-index on create/modify/delete events.
- `ferret index --watch` accepted as an alias for `ferret watch`.
- Watcher respects `.ferretignore` and `.gitignore` — ignored files are not watched.
- Startup banner shows workspace path and number of directories under watch.
- Graceful Ctrl+C shutdown with exit code 0.
- Transient I/O errors on individual files are logged and skipped; the watcher continues.

**Incremental Indexing**
- Mtime-based fingerprinting: only changed files are re-parsed on subsequent `ferret index` runs.
- `ferret index --hash` opt-in flag for content-hash change detection (useful after VCS checkout
  with reset timestamps).
- `ferret index --rebuild` bypasses incremental logic and reindexes all files unconditionally.
- Incremental state persisted to `.ferret/index-state.json`; survives process restart.
- Corrupted state triggers automatic fallback to full reindex with a WARN log line.

**Performance**
- 10,000-file workspace indexed in under 60 seconds on Apple M-series / equivalent x64.
- 1,000-file workspace indexed in under 10 seconds.
- `ferret search` returns in under 200 ms for a 10,000-document index.
- `ferret serve` cold-start (process launch to MCP-ready) under 3 seconds.
- Peak index memory: under 512 MB at 10,000 files.
- `ferret serve` idle memory: under 100 MB after indexing 10,000 files.
- CI benchmark tests guard all six performance targets; benchmarks are tagged and skipped
  on machines without the `performance` test category tag.

**Diagnostics**
- `ferret doctor` command: checks workspace existence, index existence, index freshness,
  MCP server reachability, and .NET runtime version. Exits 0 on all-pass, 1 on any fail.
  Each FAIL line includes a one-line remediation hint.
- `ferret --log-level debug` global flag enables verbose logging for any command.
- `ferret index --verbose` prints per-file log lines (filename, parser, document ID).
- Structured log format: ISO 8601 timestamp + level + `[Component]` + message, written to stderr.
- `ferret index` completion summary: `Indexed N files, skipped M files, 0 errors in X.Xs.`

**Configuration**
- `.ferret/config.json` field-level validation: missing or malformed fields produce an error
  naming the specific field and its expected type.
- Unknown config keys produce a WARN (not a crash) naming the unrecognised key.
- `ferret config validate` command: exits 0 on valid config, 1 with diagnostics on invalid.
- `.ferretignore` supported at workspace root; patterns exclude files from indexing and watching.
- Environment variables override config file values; documented in `docs/CONFIGURATION.md`.
- `ferret --version` outputs `ferret X.Y.Z` matching the assembly version.

**Installer and Release Pipeline**
- Self-contained single-binary publish for `win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`
  using `--self-contained -p:PublishTrimmed=true`. Each binary under 100 MB.
- `scripts/install.sh` (macOS/Linux) and `scripts/install.ps1` (Windows): detect platform,
  download the correct binary, place it on PATH. Idempotent.
- GitHub Actions `release.yml` workflow: triggers on `v*` tags, builds all four platform
  binaries, attaches them as release assets.

**Documentation**
- `docs/QUICKSTART.md`: install → init → index → serve → Claude Desktop integration in under
  five minutes. Includes the exact JSON snippet for Claude Desktop `claude_desktop_config.json`.
- `docs/CLI-REFERENCE.md`: every shipped command documented with flags, arguments, exit codes,
  and one example invocation.
- `docs/CONFIGURATION.md`: every `.ferret/config.json` field (type, default, description) and
  every environment variable override.
- `docs/MCP-TOOLS.md`: all four MCP tools (`search`, `read_document`, `workspace_status`,
  `ferret_context`) with input/output schemas and examples.
- `docs/TROUBLESHOOTING.md`: five most common setup errors, `ferret doctor` FAIL messages,
  and remediation steps.
- `samples/` directory: small markdown and code workspace usable for verifying a fresh install.
- `README.md` updated: RC1 install instructions, full feature list through Sprint 13, link to
  `docs/QUICKSTART.md`.
- `docs/DOGFOOD.md`: 25-task log of real engineering work completed using Ferret as the primary
  context source before RC1 was declared.

**End-to-End Tests**
- E2E: index `samples/`, search for known term, assert file in top-3 results.
- E2E: index `samples/`, delete file, reindex, search unique term, assert zero results.
- E2E: start `ferret serve` subprocess, call MCP `search` tool, assert results returned.
- E2E: start `ferret serve` subprocess, call `ferret_context`, assert non-empty package.
- E2E: start `ferret watch`, create new file, wait ≤3 s, assert indexed.
- E2E: `ferret doctor` on correct workspace → exit 0, all PASS.
- E2E: `ferret doctor` with missing index → exit 1, index-check FAIL.

---

### Added — Sprint 13

**Context Assembly**
- `IContextAssembler` and `ContextAssemblyEngine`: builds a ranked context package from search
  results, trimming to a configurable token budget.
- `ContextPackage` / `DocumentExcerpt` value types carry provenance (file path, line range,
  relevance score) alongside the text excerpt.
- `ferret_context` MCP tool: accepts a natural-language query, returns a structured context
  package for use in LLM prompts.
- `IContextScorer`: relevance scoring strategy interface with BM25-based default implementation.
- `IExcerptExtractor`: window-based excerpt extraction; expands to nearest sentence boundary.
- Token budget enforcement: `ContextBudgetEnforcer` trims excerpt list to fit within a
  configurable token limit (default 8,192 tokens).

**MCP Server Enhancements**
- `workspace_status` MCP tool: returns index document count, last-indexed timestamp,
  workspace path, and Ferret version.
- MCP server wires `ferret_context` alongside existing `search` and `read_document` tools.
- Structured JSON error responses for malformed requests (no unhandled exceptions).

---

### Added — Sprints 8–12 (summary for release notes completeness)

- **Sprint 8 (Connector Platform):** `IConnector` abstraction; filesystem connector;
  `.gitignore` / `.ferretignore` filter chain.
- **Sprint 9 (Document Pipeline):** `IDocumentParser` abstraction; Markdown, C#, plain-text
  parsers; `IIndexPipeline` orchestration; `ferret index` command.
- **Sprint 10 (Search Platform):** BM25 keyword search; `IQueryParser`; `ferret search` command;
  JSON and table output formatters.
- **Sprint 11 (Integration Platform):** MCP server (`ferret serve`); `search` and `read_document`
  MCP tools; stdio transport; Claude Desktop integration.
- **Sprint 12 (AI Platform):** `IModelRouter`; `IModelRegistry`; Ollama provider;
  OpenAI provider; `Ferret.Configuration.AI`; AI options validation.

---

### Fixed — Sprint 14

- `ferret index` no longer crashes on binary files (`.exe`, `.png`, `.dll`) — they are
  detected and skipped with a WARN log line.
- `ferret serve` no longer exits with an unhandled exception when the index is absent —
  it starts and returns structured error responses from MCP tools.
- Rapid file modification sequences no longer cause the file watcher to emit redundant
  reindex events — debounce coalesces events within a 500 ms window.
- Corrupted `.ferret/index-state.json` no longer crashes the indexer — automatic fallback
  to full reindex with a WARN.

---

### Removed — Sprint 14

Nothing removed.

---

### Security — Sprint 14

No security-relevant changes in Sprint 14. Authentication and access control are deferred
to V2 (see Section 1.6 of the Sprint 14 spec for the full deferred list).

---

[0.14.0]: https://github.com/indoulia/Ferret/releases/tag/v0.14.0-sprint14

---

## [Unreleased]

### Added
- Initial repository structure (Sprint 0)
- Directory.Build.props with shared SDK settings
- .editorconfig with C# and JSON formatting rules
- .gitignore for .NET / Node / Python workspace
- GitHub Actions workflow stubs (CI, release, security)
- ADR template and first ADR (0001 — use ADRs)
- Document templates: PRD, spec, architecture, API, database, plugin, MCP, CLI, testing, release, versioning
- Bootstrap PowerShell script
- Community files: CONTRIBUTING, CODE_OF_CONDUCT, SECURITY, LICENSE

---

## [0.1.0-alpha] — Unreleased

_Sprint 0 milestone — project foundation only. No production code._

---

<!-- Links -->
[Unreleased]: https://github.com/indoulia/Ferret/compare/HEAD...HEAD
[0.1.0-alpha]: https://github.com/indoulia/Ferret/releases/tag/v0.1.0-alpha
