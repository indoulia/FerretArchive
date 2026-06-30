# Sprint 14 Design Specification: RC1 / Production Readiness

**Project:** Ferret (ContextOS)
**Date:** 2026-06-29
**Status:** Authoritative
**Sprint tag:** `v0.14.0-sprint14`

---

## Executive Summary

Sprint 14 is the production readiness sprint. No new platforms are introduced. Every task exists to make the platforms delivered in Sprints 8–13 ship-worthy. After Sprint 14, Ferret is released as RC1.

The sprint delivers seven categories of work: file watching and incremental indexing, performance and memory tuning, diagnostics and logging improvements, installer packaging, end-to-end tests, user-facing documentation, and mandatory dogfooding — 25 real engineering tasks completed with Ferret as the primary context source before RC1 is declared. The RC1 Readiness Checklist in Section 2 is the binding gate — Sprint 14 is complete when every item is checked. Anything not on the checklist is deferred to V2.

The user story is: a new developer downloads Ferret, runs a single install command, initialises a workspace, indexes it, starts `ferret serve`, and has a Claude integration working in under five minutes. File changes are picked up automatically. Errors are diagnosable without reading source code.

---

## Section 1: Sprint Identity

### 1.1 Sprint Name and Tag

**Name:** Sprint 14 — RC1 / Production Readiness
**Tag:** `v0.14.0-sprint14`

### 1.2 Theme

> Make what works in development work in production.

### 1.3 Sprint Goal

> Deliver a single-binary Ferret distribution that a developer can install, run, and rely on — with file watching, incremental indexing, a passing RC1 checklist, and documentation that gets them productive in under five minutes.

### 1.4 User Story

A developer downloads Ferret for their platform. They run `ferret init`, `ferret index`, and `ferret serve`. They open Claude Desktop, point it at the MCP server, and ask a question about their codebase. Claude returns grounded, context-rich answers sourced from the indexed workspace. They edit a file; within two seconds the index updates automatically. When something goes wrong, `ferret doctor` tells them exactly what is misconfigured. They did not read a line of source code.

### 1.5 What a New User Can Do After Sprint 14

Download a single binary, run `ferret doctor` to verify setup, index a workspace, connect Claude Desktop, and have an AI assistant with live awareness of their codebase — without any prior knowledge of Ferret internals.

### 1.6 Non-Goals

Sprint 14 explicitly does not deliver:

- `ferret ask` / conversational chat
- Semantic or vector search
- Knowledge graph
- Multi-workspace federation
- Plugin SDK or third-party connector authoring
- Web UI or REST API
- Authentication or multi-user access control
- Cloud sync or remote workspaces
- Any feature not present on the RC1 Readiness Checklist below

**Scope Gate Rule:** If a feature request is raised during Sprint 14 and it is not already on the RC1 checklist, it goes to the V2 backlog. No exceptions.

---

## Section 2: RC1 Readiness Checklist

This checklist is the gate to RC1. Sprint 14 is complete when every item is checked.
Anything not on this list goes to V2.

---

### Correctness

- [ ] `ferret index` on a fresh workspace produces a non-empty index with the correct document count matching the number of indexable files in the workspace.
- [ ] `ferret search <term>` returns results that actually contain `<term>` in their content or filename (no false positives in top-10 results).
- [ ] `ferret search <term>` returns no results when `<term>` does not appear in any indexed document.
- [ ] `ferret serve` MCP tool `search` returns results consistent with `ferret search` for the same query.
- [ ] `ferret serve` MCP tool `read_document` returns the full content of a document given its path.
- [ ] `ferret serve` MCP tool `ferret_context` returns a structured context package containing at least one document excerpt relevant to the query.
- [ ] Deleting a file from the workspace and running `ferret index` (or waiting for watch) removes the document from search results.
- [ ] Renaming a file in the workspace and running `ferret index` correctly indexes under the new path and removes the old path.
- [ ] `ferret index --rebuild` produces identical results to a fresh `ferret index` on a pristine workspace.
- [ ] Running `ferret index` twice consecutively on an unchanged workspace produces no errors and leaves the index in the same state.

---

### File Watching

- [ ] `ferret watch` command exists and starts a long-running process that monitors the workspace for file changes.
- [ ] `ferret index --watch` is accepted as an alias for `ferret watch` and behaves identically.
- [ ] When a new file is created in the workspace, `ferret watch` triggers reindexing of that file within 2 seconds.
- [ ] When an existing file is modified, `ferret watch` triggers reindexing of that file within 2 seconds.
- [ ] When a file is deleted from the workspace, `ferret watch` removes it from the index within 2 seconds.
- [ ] `ferret watch` respects `.ferretignore` and `.gitignore` — files matching ignore patterns are not watched.
- [ ] `ferret watch` outputs a log line each time a file change is detected, naming the file and the action taken (indexed / removed / ignored).
- [ ] `ferret watch` does not crash or enter an error loop when a file is rapidly modified multiple times in succession (debounce of at least 200 ms).
- [ ] Pressing Ctrl+C while `ferret watch` is running exits cleanly with exit code 0.
- [ ] `ferret watch` continues running after a transient I/O error on a single file (the error is logged, the watcher continues).

---

### Incremental Indexing

- [ ] When `ferret index` runs on a workspace where only one file has changed since the last index run, only that one file is re-parsed and re-indexed (verified via log output showing "1 file changed").
- [ ] When `ferret index` runs on an unchanged workspace, 0 files are re-parsed (log output shows "0 files changed").
- [ ] Incremental reindex of a single changed file completes in under 2 seconds on a modern laptop.
- [ ] Incremental index change detection uses file modification time as the primary signal.
- [ ] A content hash check is available as a fallback when modification time is unreliable (e.g. checked out from VCS with a reset timestamp) — opt-in via `ferret index --hash`.
- [ ] `ferret index --rebuild` bypasses incremental logic and reindexes all files unconditionally.
- [ ] The incremental index state (last-indexed timestamps or hashes) is stored in `.ferret/` and survives process restart.
- [ ] Corrupted incremental state causes `ferret index` to fall back to full reindex with a warning, not a crash.

---

### Performance

- [ ] `ferret index` on a workspace of 10,000 files completes in under 60 seconds on a modern laptop (Apple M-series or equivalent x64).
- [ ] `ferret index` on a workspace of 1,000 files completes in under 10 seconds.
- [ ] `ferret search <term>` returns results in under 200 ms for a 10,000-file index.
- [ ] `ferret serve` startup time (from process launch to MCP-ready) is under 3 seconds on a cold start.
- [ ] Peak memory usage during `ferret index` on a 10,000-file workspace does not exceed 512 MB (measured via process peak RSS).
- [ ] Peak memory usage of `ferret serve` at idle is under 100 MB after indexing a 10,000-file workspace.
- [ ] A benchmark test exists that asserts the 10,000-file index time is under 60 seconds (CI-executable, skipped on machines without a performance tag).
- [ ] A benchmark test exists that asserts `ferret search` returns in under 200 ms at 10,000 documents.

---

### Reliability

- [ ] Running `ferret index` in a directory that is not a Ferret workspace prints a clear error message and exits with a non-zero exit code; it does not crash with an unhandled exception.
- [ ] Running `ferret search` when no index exists prints "No index found. Run `ferret index` first." and exits with exit code 1.
- [ ] Running `ferret serve` when no index exists prints a warning but starts successfully; MCP tools return structured error responses rather than crashing.
- [ ] `ferret index` handles a file that is locked by another process gracefully: skips the file, logs a warning, and continues indexing remaining files.
- [ ] `ferret index` handles a binary file (e.g. a `.exe` or `.png`) without crashing — either skips it (with log) or indexes its metadata only.
- [ ] `ferret serve` does not crash when an MCP client sends a malformed JSON request — it returns a JSON error response and continues serving.
- [ ] All `ferret` commands exit with code 0 on success and non-zero on failure.
- [ ] No unhandled exception stack traces are printed to stdout or stderr in any normal error scenario (wrong args, missing workspace, missing index, locked file, malformed query).

---

### Diagnostics

- [ ] `ferret doctor` checks and reports the status of: workspace existence, index existence, index freshness (last-indexed timestamp), MCP server reachability (if `ferret serve` was previously started), and .NET runtime version.
- [ ] `ferret doctor` exits with code 0 when all checks pass and code 1 when any check fails.
- [ ] `ferret doctor` output clearly labels each check as PASS or FAIL and includes a one-line remediation hint for each FAIL.
- [ ] Every error log line produced by any `ferret` command includes: ISO 8601 timestamp, log level (ERROR/WARN/INFO/DEBUG), component name (e.g. `[IndexPipeline]`), and error message.
- [ ] `ferret --log-level debug` is a supported global flag that enables verbose logging for any command.
- [ ] `ferret index --verbose` prints a per-file log line as each file is indexed (filename, parser used, document ID).
- [ ] `ferret index` prints a summary on completion: "Indexed N files, skipped M files, 0 errors in X.Xs."
- [ ] `ferret watch` prints a startup banner that includes the workspace path and the number of directories being watched.
- [ ] Log output is written to stderr, not stdout, so that stdout remains machine-readable for commands like `ferret search --format json`.

---

### Configuration

- [ ] `.ferret/config.json` is validated on startup; if required fields are missing or malformed, the error message names the specific field and its expected type.
- [ ] An unknown key in `.ferret/config.json` produces a warning (not a crash) and lists the unrecognised key by name.
- [ ] `ferret config validate` command exists and exits 0 on valid config, 1 with a diagnostic message on invalid config.
- [ ] The `.ferretignore` file is supported at the workspace root — files matching its patterns are excluded from indexing and watching.
- [ ] AI provider configuration (Ollama base URL, OpenAI API key) can be set via environment variables as well as `.ferret/config.json` — environment variables take precedence.
- [ ] `ferret --version` outputs the version string in the format `ferret X.Y.Z` (e.g. `ferret 0.14.0`).
- [ ] The version string output by `ferret --version` matches the assembly version in the published binary.

---

### Installation

- [ ] `dotnet publish -r win-x64 --self-contained -c Release` produces a single executable `ferret.exe` that runs on Windows x64 without a .NET runtime installed.
- [ ] `dotnet publish -r osx-arm64 --self-contained -c Release` produces a single executable `ferret` that runs on macOS Apple Silicon without a .NET runtime installed.
- [ ] `dotnet publish -r osx-x64 --self-contained -c Release` produces a single executable `ferret` that runs on macOS Intel without a .NET runtime installed.
- [ ] `dotnet publish -r linux-x64 --self-contained -c Release` produces a single executable `ferret` that runs on Ubuntu 22.04+ without a .NET runtime installed.
- [ ] Each platform binary is under 100 MB after trimming (`-p:PublishTrimmed=true`).
- [ ] An install script (`install.sh` / `install.ps1`) exists that downloads the correct platform binary for the current OS and architecture and places it on the `PATH`.
- [ ] The install script is idempotent — running it twice does not corrupt the installation.
- [ ] After installation, `ferret --version` outputs the correct version string from a shell that did not previously have `ferret` on its `PATH`.
- [ ] GitHub Actions CI publishes platform binaries as release assets on every tagged commit (`v*`).

---

### Documentation Portal

- [ ] `ferret manual` starts an HTTP server on port 7070 and opens `http://localhost:7070/manual` in the default browser.
- [ ] `ferret manual --port 8080` overrides the default port and the server binds to the specified port.
- [ ] All 41 pages accessible and render correctly (no 404, no 500).
- [ ] All 8 top-level sections present: Getting Started, User Guide, Reference, Architecture, Developer Guide, Design Decisions, Troubleshooting, FAQ.
- [ ] Left navigation is present on every page, highlights the current page, and groups pages by section.
- [ ] Full-text search (Lunr.js) returns relevant results for queries that match page titles and sections.
- [ ] Every page displays a persistent footer with Previous / Next navigation, Edit source link, Report issue link, Architecture link, and CLI Reference link.
- [ ] Previous / Next links navigate correctly between adjacent pages; first and last pages have no broken prev/next links.
- [ ] Getting Started section (6 pages) covers: install, `ferret init`, `ferret index`, `ferret search`, and Claude Desktop + Cursor integration with the exact JSON MCP config snippet.
- [ ] CLI Reference documents every shipped `ferret` command with flags, arguments, exit codes, and one example invocation.
- [ ] MCP Reference documents every MCP tool (`ferret_search`, `ferret_read_document`, `ferret_context`, `ferret_workspace_status`) with input schema, output schema, and one example.
- [ ] Architecture Explorer (10 pages) includes ASCII diagrams for platform overview, search flow, and context assembly (minimum 3 of 10).
- [ ] Design Decisions section (8 pages) explains Why SQLite, Why BM25, Why MCP, Why Providers, Why Context Assembly, Why Platform-First, Why Manual.
- [ ] `README.md` at the repository root is updated to reflect RC1: install instructions, feature list through Sprint 13, and a `ferret manual` launch instruction.

---

### Testing

- [ ] E2E test: index the `samples/` workspace, search for a known term that appears in a known file, assert the file appears in the top-3 results.
- [ ] E2E test: index the `samples/` workspace, delete a file, run `ferret index`, search for a term unique to that file, assert zero results.
- [ ] E2E test: start `ferret serve` as a subprocess, connect an in-process MCP client, call the `search` tool, assert results are returned.
- [ ] E2E test: start `ferret serve` as a subprocess, connect an in-process MCP client, call the `ferret_context` tool with a query, assert a non-empty context package is returned containing at least one document excerpt.
- [ ] E2E test: start `ferret watch` as a subprocess, create a new file in the workspace, wait up to 3 seconds, search for content unique to that file, assert a result is returned.
- [ ] E2E test: run `ferret doctor` on a correctly configured workspace, assert exit code 0 and all checks report PASS.
- [ ] E2E test: run `ferret doctor` on a workspace with no index, assert exit code 1 and the index-not-found check reports FAIL.
- [ ] All existing unit tests pass (`dotnet test`) on Windows, macOS, and Linux in CI.
- [ ] All E2E tests pass in CI on at least Windows x64 and Linux x64.
- [ ] Test count does not decrease from Sprint 13 completion count.
- [ ] No test is marked `[Ignore]` or `Skip` without a linked GitHub issue explaining why.

---

### Dogfooding

Ferret must be used daily by at least one developer on real engineering work before RC1 is declared. Unit and integration tests verify correctness; dogfooding verifies usability.

**Setup:**
- [ ] The Ferret repository itself is indexed with `ferret index` and the index is committed to `.ferret/`.
- [ ] Claude Desktop (or Claude Code) is connected to `ferret serve` running on the Ferret repository.

**Usage gate:**
- [ ] At least 25 real engineering tasks have been completed using Ferret as the primary context source — not by falling back to manual file reads or GitHub search.
- [ ] A `docs/DOGFOOD.md` log exists recording each task, whether Ferret answered it correctly, and any failure or workaround.

**Failure resolution:**
- [ ] Every failure or workaround recorded in `docs/DOGFOOD.md` is triaged and assigned either: (a) a fix in Sprint 14, or (b) a linked GitHub issue in the V2 backlog.
- [ ] All high-impact failures (those that required a workaround on more than 3 tasks) are fixed before RC1 is declared.
- [ ] The 25th task is completed after the last high-impact fix is merged — confirming the fix actually resolved the issue in real use.

---

## Section 3: Feature Scoping

### What's in RC1

The following capabilities are in scope for Sprint 14 and must be complete before RC1 is declared:

| Capability | Deliverable |
|---|---|
| File watching | `ferret watch` command; debounced watcher; respects ignore files |
| Incremental indexing | mtime-based change detection; `--hash` fallback; state persisted to `.ferret/` |
| Performance | Benchmarks passing; 10k-file index under 60 s; search under 200 ms |
| Memory | Idle serve under 100 MB; index peak under 512 MB |
| Diagnostics | `ferret doctor` with remediation hints; structured log format; `--log-level debug` |
| Configuration | `ferret config validate`; field-level validation errors; `.ferretignore` |
| Installation | Self-contained binaries for win-x64, osx-arm64, osx-x64, linux-x64 via `publish.ps1`; no CI pipeline |
| Documentation | The Ferret Manual (`ferret manual`): left-nav site on port 7070, 41 pages across 8 sections (Getting Started, User Guide, Reference, Architecture, Developer Guide, Design Decisions, Troubleshooting, FAQ), full-text Lunr.js search, persistent Previous/Next/Edit/Report footer; `README.md` updated |
| E2E tests | 7 E2E scenarios covering index, search, watch, serve, doctor |
| Dogfooding | 25 real engineering tasks on the Ferret repo; all high-impact failures fixed |

### What's Deferred to V2

The following items were raised as candidates for Sprint 14 but are explicitly out of scope. They go to the V2 backlog:

| Deferred Item | Rationale |
|---|---|
| `ferret ask` / conversational chat | Depends on Sprint 15 conversation memory; not ready |
| Semantic / vector search | Depends on embedding infrastructure not yet built |
| Knowledge graph | Sprint 16 capability |
| Plugin SDK / third-party connectors | Authoring experience needs dedicated sprint |
| Web UI / REST API | Out of scope for CLI-first RC1 |
| Multi-workspace federation | Architectural work deferred post-RC1 |
| Authentication / access control | Not required for single-user CLI |
| Cloud sync / remote workspaces | V2 distribution model decision needed |
| Windows filesystem watcher polling fallback | Fallback for network drives; low priority for RC1 |
| `ferret serve --port` (HTTP transport) | MCP-over-HTTP deferred; stdio sufficient for RC1 |
| Telemetry / usage analytics | Privacy design needed before implementation |
| Auto-update mechanism | Deferred until release cadence is established |

---

## Section 4: Sub-plan Index

Each sub-plan is a self-contained implementation plan linked from this spec. Plans are written before the task begins and executed in the order shown.

| Sub-plan | File | Status |
|---|---|---|
| S1: File Watching | `plans/2026-06-29-sprint-14-s1-file-watching.md` | Not started |
| S2: Incremental Indexing | `plans/2026-06-29-sprint-14-s2-incremental-indexing.md` | Not started |
| S3: Performance and Memory Tuning | `plans/2026-06-29-sprint-14-s3-performance.md` | Not started |
| S4: Diagnostics and Logging | `plans/2026-06-29-sprint-14-s4-diagnostics.md` | Not started |
| S5: Configuration Validation | `plans/2026-06-29-sprint-14-s5-configuration.md` | Not started |
| S6: Installer and Release Pipeline | `plans/2026-06-29-sprint-14-s6-installer.md` | Not started |
| S7: Documentation Portal | `plans/2026-06-29-sprint-14-s7-documentation-portal.md` | Not started |
| S8: End-to-End Tests | `plans/2026-06-29-sprint-14-s8-e2e-tests.md` | Not started |
| S9: RC1 Checklist Sign-off | `plans/2026-06-29-sprint-14-s9-rc1-signoff.md` | Not started |

### Execution Order

S1 and S2 are the critical path — file watching depends on incremental indexing infrastructure. S3 (performance) depends on S2 (incremental) being complete so benchmarks measure the optimised path. S4 (diagnostics) and S5 (configuration) are independent and can run in parallel with S3. S6 (installer) is independent and can be started immediately. S7 (documentation) is written last when all commands are stable. S8 (E2E tests) requires S1–S6 to be complete. S9 (sign-off) is the final task — it walks the RC1 checklist top to bottom with binary yes/no for each item.
