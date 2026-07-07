# V2 Sprint 1 — Vertical Slice Execution Plan

> **For agentic workers:** This plan is at milestone granularity, not task granularity. Per its own Note below, converting each milestone into a file-and-code-exact task list (the `superpowers:writing-plans` standard) requires reading the current real source of `FilesystemConnector`, `ParserDispatcher`, `Document`, and one existing persisted-state store first — that verification has not been done yet. Do not begin coding from this document alone; produce the detailed per-milestone task plan after that source review, then execute with `superpowers:subagent-driven-development` or `superpowers:executing-plans`.

**Goal:** Demonstrate that one artifact can be scanned, parsed, persisted, resolved, and reused end-to-end — proving the mechanism architecture (ARCH-032/033/034) is implementable, not building production persistence.

**Redefined success criterion (per ADR-0021's transition):** Sprint 1 proves the architecture is implementable. It does not build persistence, indexing, or caching as product features — those are later sprints (see `docs/002-Architecture/V2-IMPLEMENTATION-BACKLOG-001.md`).

**Architecture:** A thin vertical slice through the existing, real Connector Platform and Parser Platform, plus three new pieces built directly against ARCH-032/033/034's abstractions: a dependency-record model, a persistence interface with a disposable spike implementation, and a minimal resolution check. No production storage or serialization technology is chosen here.

**Tech Stack:** .NET 9, C#, xUnit. No new external package — the spike store needs nothing beyond the .NET file APIs already available.

---

## Global Constraints

- **Concurrency scope (satisfies ADR-0021 Rule 5 for this sprint):** Single-process access only. This sprint makes no claim about, and does not test, concurrent or multi-process access to persisted dependency state. Any code written this sprint may assume it is the only process touching its own spike store file.
- **No production storage or serialization decision is made in this sprint.** The persistence implementation built here is explicitly disposable — a single file under a clearly-namespaced, non-production path — chosen under ADR-0001's "trivial implementation choices do not need an ADR" exemption, per the Readiness Checklist's finding. It must be replaceable without touching the `IDependencyStateStore`-shaped abstraction any later ADR's implementation will satisfy.
- **No deletion-path code.** The deletion-signal-production gap (ARCH-030 §2; ARCH-032 §9) is an unresolved conceptual gap, not an implementation detail — per the Readiness Checklist, no deletion handling is in scope this sprint.
- **Every claimed guarantee traces to a specific ARCH-032/033/034 section.** No new guarantee, dependency shape, or validity concept is invented this sprint — if a milestone seems to need one, stop and escalate per ADR-0021 Rule 6, per the same discipline the whole V2 architecture program has followed.
- **No work, organization, or personal names** in code, comments, or commit messages.
- **Real process restart required for Milestone 4/5** — an in-process, in-memory round-trip does not prove ARCH-026 §1's actual bar ("survives... beyond the process that produced it"). Testing this milestone with two calls in the same process run is not an acceptable substitute.

---

## Milestones

### Milestone 1 — Scan One File

**Realizes:** Existing Connector Platform behavior (ARCH-024 §1) — no new code beyond a thin CLI/test entry point.

**Acceptance criteria:**
- Given a real file path, the existing Connector Platform produces an `AssetDescriptor` with the same fields ARCH-024 §1 already documents.
- No change to `Ferret.ConnectorPlatform` or `Ferret.Connectors.Filesystem` is required.

**Depends on:** Nothing — existing, real code.

**Risk:** Low. The only risk is discovering, on reading the real source, that ARCH-024 §1's description has drifted since it was written — if so, stop and correct ARCH-024 via the normal amendment process before proceeding, don't silently code around the drift.

**Measurable outcome:** One `AssetDescriptor` produced for one real file, inspected manually or via a test assertion.

---

### Milestone 2 — Parse the File

**Realizes:** Existing Parser Platform behavior (ARCH-024 §2).

**Acceptance criteria:**
- Given Milestone 1's `AssetDescriptor`, the existing Parser Platform produces a `Document`.
- No change to `Ferret.ParserPlatform` is required.

**Depends on:** Milestone 1.

**Risk:** Low, same caveat as Milestone 1 regarding source drift.

**Measurable outcome:** One `Document` produced, with non-empty `PlainText`.

---

### Milestone 3 — Build the Dependency Record

**Realizes:** ARCH-032 §2.1 (dependency shape 1: source content), §2.2 (artifact state, Class A only), §2.3 (request-identity properties, minimal form).

**New code:** A dependency-record data model — no persistence yet, no technology commitment.

**Acceptance criteria:**
- The record captures the `Document`'s source-content dependency (its `AssetFingerprint`, per ARCH-024 §1's existing field) — shape 1 only; no shape 4 (parser registration) capture yet, since ARCH-026 §3 already records that as an unmet requirement for any component, not something this sprint closes.
- The record captures a minimal, honest instance of ARCH-028 §2's three request-identity properties: engine responsibility = "parse a file at a given path," explicit parameter set = `{ path }`, ambient dependency scope = none (there is no implicit scope beyond the explicit path for this operation).
- The record optionally carries the `Document`'s own output (its `PlainText`/`Title`), since Milestone 6 needs something to reuse.
- The record introduces no dependency shape beyond what ARCH-025 §3 already defines, and no validity class beyond Class A (ARCH-025 §2) — verified against ARCH-030 §5's matrix before coding, not after.

**Depends on:** Milestone 2.

**Risk:** Medium — this is the first genuinely new code in the sprint. The specific failure mode to guard against: silently inventing a data field that amounts to a sixth dependency shape or a precomputed validity verdict (ARCH-032 §4 explicitly forbids storing one). Review the record's shape against ARCH-032 §2 before writing persistence code in Milestone 4.

**Measurable outcome:** One dependency record, in memory, containing exactly the fields listed above — no more, no fewer.

---

### Milestone 4 — Persist the Record (Spike Store)

**Realizes:** ARCH-032 §1 ("Record," "Retain"), §3 ("Created" lifecycle stage).

**New code:** An `IDependencyStateStore`-shaped interface (name TBD at task-plan time) with exactly one implementation: a disposable spike store writing to a single file under a clearly non-production path (e.g., a scratch/spike subdirectory, not `.ferret/`'s existing production files — `.ferret/workspace.json`, `.ferret/state.json`, `.ferret/connectors.json`, `.ferret/index-state.json`, and the keyword index database, per ARCH-024's inventory, must not be touched or collided with).

**Acceptance criteria:**
- Milestone 3's record, written through the interface, produces a file on disk.
- The interface itself names no technology, format, or key — only "write this record, given this request identity" and "read a record back, given a request identity."
- The spike implementation is clearly labeled, in code comments and in its own file/type name, as non-production and superseded by a future ADR (Implementation Backlog Epic 1, Features 1.4–1.5) — this is not optional polish, it is how the sprint avoids the spike quietly becoming the permanent choice.

**Depends on:** Milestone 3.

**Risk:** Medium. The main risk is scope creep — do not build a general-purpose key-value store here; build exactly enough to persist and later retrieve one record for one request.

**Measurable outcome:** A file exists on disk after the write completes, and the writing process has genuinely exited (not merely returned from a method call) before Milestone 5 begins.

---

### Milestone 5 — Reload and Resolve

**Realizes:** ARCH-033 §1 (Retrieval: Locate, Fetch), §4 (retrieval as lookup, not search), §5 (comparison procedure, single dependency shape), §3 (outcomes).

**New code:** A minimal resolution check — given a request (the same `{ path }` as Milestone 3), locate a persisted candidate whose recorded request identity is equivalent (ARCH-028 §3), fetch its recorded dependency state, and compare it against the file's current fingerprint.

**Acceptance criteria (two scenarios, both required):**
- **Scenario A — file unchanged:** In a **new process**, re-scan the same file (Milestone 1), locate the Milestone 4 record for the same request, compare fingerprints, and produce **Satisfied**.
- **Scenario B — file modified:** Modify the file's content between the Milestone 4 write and the resolution check, then produce **Not-satisfied**.
- **Scenario C — record missing or corrupted (from the Readiness Checklist's corruption-detection requirement):** Delete or truncate the spike store's file, then attempt resolution — must produce **Indeterminate**, never Satisfied.
- No scenario produces a result other than one of the three ARCH-027 §3 outcomes.

**Depends on:** Milestone 4, and a genuine process boundary between writing and reading (Global Constraints).

**Risk:** Medium-High — this is the first real exercise of the fail-closed guarantee (ARCH-032 §6, §7.1; ARCH-033 §7, §8.1) under an actual failure condition, not just a happy path. Do not treat Scenario C as optional.

**Measurable outcome:** All three scenarios produce their required outcome, verified by test, across an actual process restart for Scenario A/B.

---

### Milestone 6 — Reuse and Verify Identical Output

**Realizes:** ARCH-034 §1, §2 (indistinguishable output), §4 (outputs).

**New code:** A thin branch in the existing CLI output path: on Satisfied, populate `CommandResult` from the persisted `Document` output (Milestone 3's optional field) instead of re-parsing; on Not-satisfied or Indeterminate, re-run Milestone 2 and re-persist via Milestone 4.

**Acceptance criteria:**
- CLI output is byte-identical whether produced via reuse (Satisfied path) or via fresh computation (Not-satisfied/Indeterminate path) — this is ARCH-034 §2's indistinguishable-output guarantee, tested for the first time against real code rather than asserted in architecture text.
- No new CLI flag, command, or output field is introduced (ARCH-034's Scope: no API decisions).

**Depends on:** Milestone 5.

**Risk:** Low-Medium. The main risk is accidentally exposing which path was taken (e.g., a stray log line reaching stdout) — that would itself be a finding against ARCH-034 §2, not a cosmetic issue.

**Measurable outcome:** A single test asserting byte-identical `CommandResult` output for the Satisfied and recompute paths.

---

## Sequencing

Milestones 1 → 2 → 3 → 4 are strictly sequential (each consumes the prior's output). Milestone 5 requires Milestone 4's artifact to exist **and** a process boundary to have occurred. Milestone 6 requires Milestone 5's outcome.

## Overall Success Criteria

All six milestones' acceptance criteria pass, including all three of Milestone 5's scenarios, with a genuine process restart exercised for Milestone 5 — not simulated in-process. No milestone required inventing a guarantee, dependency shape, or validity concept beyond what ARCH-032, ARCH-033, or ARCH-034 already state; any point where one seemed necessary was escalated per ADR-0021 Rule 6 rather than implemented ad hoc.

## Source Verification (Complete — 2026-07-03)

The four files this plan depends on were read in full. No drift from ARCH-024's description; two concrete facts worth recording so a future task-level plan doesn't have to re-derive them:

- **`src/Ferret.Connectors.Filesystem/FilesystemConnector.cs`** — `DiscoverAsync` returns `IAsyncEnumerable<AssetDescriptor>`; `AssetDescriptor.Fingerprint` is populated via `AssetFingerprint.CreateLightweight(entry.LastWriteTimeUtc, size)`. `.ferret` is already a hardcoded skip directory during discovery — a Sprint 1 spike store written under `.ferret/` will not be re-scanned as a document.
- **`src/Ferret.ParserPlatform/ParserDispatcher.cs`** — `DispatchAsync(Stream content, AssetDescriptor asset, CancellationToken ct)` returns `ValueTask<ParseResult<Document>>` (not `Task`).
- **`src/Ferret.Core/Documents/Document.cs`** — has a nullable `AssetFingerprint? SourceFingerprint` field, doc-commented as existing specifically to support "incremental indexing in a future sprint" — i.e., dependency shape 1 (source content) already has a landing spot in the real type, not something Milestone 3 needs to bolt on separately.
- **Confirmed by grep across all seven real parsers** (`PdfParser`, `WordParser`, `ExcelParser`, `PlainTextParser`, `MarkdownParser`, `JsonParser`, `CsvParser`): every one sets `SourceFingerprint = context.Asset.Fingerprint`. This was a real risk (an unwired field would have silently broken Milestone 3's premise) and is now closed — Milestone 3 can read `Document.SourceFingerprint` directly rather than re-deriving it from the `AssetDescriptor`.
- **`src/Ferret.Workspace/Persistence/JsonWorkspaceStore.cs`** — the existing, real pattern for `.ferret/`-scoped persisted state: an `internal sealed` static class, `System.Text.Json` with `WriteIndented = true`, plain `File.Create`/`File.OpenRead` + `SerializeAsync`/`DeserializeAsync`, path built from `WorkspaceLayout.RootDirectoryName` plus a filename constant. **Recommendation for Milestone 4's spike store: mirror this exact pattern** rather than invent a different one — it keeps the disposable choice trivial (per ADR-0001) precisely because it introduces no new pattern, only reuses an existing one at small scale.

Task-level (file-and-code-exact) execution planning may now proceed against these verified signatures.

## Related

- [ADR-0021](../../adr/0021-v2-architecture-baseline-complete.md)
- `docs/superpowers/specs/2026-07-03-v2-sprint-1-readiness-checklist.md` — the gate this plan satisfies
- [ARCH-032](../../002-Architecture/ARCH-032-Persistence-Mechanism-Design.md), [ARCH-033](../../002-Architecture/ARCH-033-Dependency-Resolution-Mechanism-Design.md), [ARCH-034](../../002-Architecture/ARCH-034-Surface-Integration-Mechanism-Design.md), [ARCH-035](../../002-Architecture/ARCH-035-Mechanism-Interaction-Model.md)
- [ARCH-024 §1, §2, §7](../../002-Architecture/ARCH-024-Artifact-Inventory.md) — the real existing components this slice reuses
- `docs/002-Architecture/V2-IMPLEMENTATION-BACKLOG-001.md` — where Sprint 2+ work (production ADRs, deletion semantics, benchmarking) is tracked
