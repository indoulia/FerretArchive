# Ferret Dogfood Log

**Purpose:** Record every real engineering task completed using Ferret as the primary
context source before RC1 is declared. Unit tests verify correctness; this log verifies
usability. Ferret must not be supplemented with manual file reads or GitHub search for
any task marked complete — if a workaround was needed, record it.

**Gate:** 25 tasks required. All high-impact failures (workaround used on 3+ tasks)
must be fixed and the 25th task must be completed after the last fix is merged.

---

## Task Log

| # | Task | Date | Ferret answered? | Workaround used? | Issue |
|---|------|------|-----------------|-----------------|-------|
| 1 | Locate the class that implements `IIndexPipeline` and read its constructor dependencies | 2026-06-29 | | | |
| 2 | Find all callers of `IIndexEngine.SearchAsync` to understand query call sites | 2026-06-29 | | | |
| 3 | Identify which parsers are registered in the DI container and in what order | 2026-06-29 | | | |
| 4 | Determine what happens when `ferret index` encounters a file larger than the configured size limit | 2026-06-29 | | | |
| 5 | Find the test that covers BM25 scoring and understand what inputs it uses | 2026-06-29 | | | |
| 6 | Locate every place where `.ferretignore` patterns are evaluated and understand the evaluation order | 2026-06-29 | | | |
| 7 | Understand the data flow from `ferret search "term"` CLI invocation to the SQLite query | 2026-06-29 | | | |
| 8 | Find where the MCP `search` tool JSON schema is defined and what validation is applied to inputs | 2026-06-29 | | | |
| 9 | Identify which component is responsible for the 500 ms debounce in `ferret watch` | 2026-06-29 | | | |
| 10 | Determine where incremental index state (`index-state.json`) is read and written | 2026-06-29 | | | |
| 11 | Locate the exception handler that catches I/O errors during file indexing and understand the fallback path | 2026-06-29 | | | |
| 12 | Find the implementation of `ferret doctor` and enumerate which checks it performs | 2026-06-29 | | | |
| 13 | Understand how `--log-level debug` is propagated from the CLI flag to the `ILogger` configuration | 2026-06-29 | | | |
| 14 | Find the publish configuration that enables single-file trimmed output and confirm `PublishTrimmed=true` | 2026-06-29 | | | |
| 15 | Locate the GitHub Actions workflow responsible for release asset publishing and identify the trigger condition | 2026-06-29 | | | |
| 16 | Understand how `ferret config validate` differentiates between missing fields and type-mismatched fields | 2026-06-29 | | | |
| 17 | Find the `ferret_context` MCP tool handler and trace how it invokes `IContextAssembler` | 2026-06-29 | | | |
| 18 | Identify where the token budget is enforced during context assembly and what the default limit is | 2026-06-29 | | | |
| 19 | Locate all E2E tests and understand how they start and stop `ferret serve` as a subprocess | 2026-06-29 | | | |
| 20 | Find the benchmark test for 10,000-file indexing and understand how the test workspace is generated | 2026-06-29 | | | |
| 21 | Determine which `IDocumentParser` handles C# files and what metadata fields it extracts | 2026-06-29 | | | |
| 22 | Understand how `workspace_status` MCP tool computes the last-indexed timestamp | 2026-06-29 | | | |
| 23 | Find the `read_document` MCP tool implementation and confirm it returns full file content without truncation | 2026-06-29 | | | |
| 24 | Locate where environment variable overrides are applied relative to config file loading — confirm precedence order | 2026-06-29 | | | |
| 25 | Understand the full startup sequence of `ferret serve` from `Program.cs` to "MCP server ready" log line | 2026-06-29 | | | |

---

## Failure Analysis

Record any task where `Workaround used?` is Yes, with a root cause and resolution status.

| Task # | Root cause | Sprint 14 fix? | GitHub issue |
|--------|-----------|----------------|-------------|
| *(none yet)* | | | |

---

## Sign-off

- [ ] All 25 tasks completed
- [ ] All high-impact failures (workaround on 3+ tasks) fixed
- [ ] Task 25 completed after last high-impact fix merged
- [ ] `docs/DOGFOOD.md` committed to `master` before tagging RC1
