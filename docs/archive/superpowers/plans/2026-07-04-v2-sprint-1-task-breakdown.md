# V2 Sprint 1 — Task Breakdown, Dependency Graph, Commit Plan, Test Plan

**Companion to:** `docs/superpowers/plans/2026-07-03-v2-sprint-1-vertical-slice.md` (milestone-level plan)
**Produced by:** Repository Discovery + Assessment (Steps 1–2 of the mandated implementation workflow)
**Status:** Task-level design only. No code written. Awaiting approval per Step 6.

---

## Task Breakdown (Step 3)

### T1 — Dependency Record Model

**Purpose:** Realize ARCH-032 §2.1 (shape 1), §2.2 (Class A artifact state), §2.3 (minimal request identity) as a plain data model. No persistence, no technology commitment.

**New files:** One new type (name TBD at implementation time, e.g. `DependencyRecord`) in a new project (see Implementation Decision #3, below) or an existing one — **recommendation:** a new project, `src/Ferret.Persistence` (or similar; final name is a naming choice, not an architectural one), mirroring the existing one-project-per-concern convention (`Ferret.ConnectorPlatform`, `Ferret.ParserPlatform`, `Ferret.Indexing` are each their own project for one ARCH-023-approved component).

**Files modified:** None — this is additive only.

**Interfaces:** None — a plain record type, following `Document`'s own pattern (`public sealed record Document` in `Ferret.Core.Documents`).

**Classes:** One record type carrying: the `AssetFingerprint` (reusing `Ferret.Core.Connectors.AssetFingerprint`, not a new type — shape 1), the three ARCH-028 §2 identity properties in their Sprint-1-minimal form (engine responsibility = a fixed string/enum value; explicit parameters = the file path; ambient scope = none), and optionally the `Document`'s own `PlainText`/`Title` (for Milestone 6 reuse).

**Methods:** None beyond what a record auto-generates (equality, `with`-expressions). No behavior lives here.

**Tests:** Construction and equality tests only (matches `AssetFingerprintTests`' style — a thin, behavior-free type gets thin tests).

**Dependencies:** `Ferret.Core` (for `AssetFingerprint`, `Document`, `AssetId`).

**Rollback strategy:** Delete the type; nothing else references it yet.

**Acceptance criteria:** A record instance can be constructed with all required fields; equality holds for two instances built from identical inputs (needed for later comparison logic).

**Estimated complexity:** Trivial.

**Risk:** Low. The only real risk is silently modeling a sixth dependency shape or a precomputed verdict — reviewed against ARCH-032 §2 and ARCH-030 §5's matrix before this task is considered done, not after.

---

### T2 — Persistence Abstraction Interface

**Purpose:** Realize ARCH-032 §1 ("Record," "Retain," "Expose for consultation") as an interface, with no technology commitment. Mirrors the shape of the existing `IIndexStateStore` (`src/Ferret.Core/Indexing/IIndexStateStore.cs`) — same style (async, `CancellationToken ct = default`, `ValueTask` for reads / `Task` for writes), same spirit (fingerprint-keyed persistence), narrowed to what Sprint 1 needs.

**New files:** One interface, e.g. `IDependencyStateStore` (name TBD), in the same new project as T1.

**Files modified:** None.

**Interfaces:** `IDependencyStateStore` with, at minimum: a method to record a `DependencyRecord` for a given request identity (mirrors `SetFingerprintAsync`), and a method to retrieve one by request identity (mirrors `GetFingerprintAsync`, returning null/none when absent — never throwing on "not found").

**Classes:** None (interface only).

**Tests:** None — an interface has no behavior to test.

**Dependencies:** T1 (the record type the interface's methods traffic in).

**Rollback strategy:** Delete the interface; nothing depends on it until T3 implements it.

**Acceptance criteria:** The interface compiles and names no technology, file format, or key structure — verified by inspection against ARCH-032 §1 and the Scope section of both ARCH-032 and the Readiness Checklist.

**Estimated complexity:** Trivial.

**Risk:** Low.

---

### T3 — Spike Store Implementation

**Purpose:** Realize ARCH-032 §3 ("Created" lifecycle stage) as a disposable, non-production implementation of T2's interface. Per ADR-0001's triviality exemption — no ADR required.

**New files:** One class, e.g. `SpikeDependencyStateStore` (name should make its disposability obvious, mirroring how `NullIndexStateStore`'s name makes its role obvious), in the same project as T1/T2.

**Files modified:** None.

**Interfaces:** Implements T2's `IDependencyStateStore`.

**Classes:** `SpikeDependencyStateStore`. **Pattern to mirror:** `JsonIndexStateStore` (`src/Ferret.Indexing/JsonIndexStateStore.cs`) — eager load in constructor, in-memory dictionary, explicit `SaveAsync` flush, plain (non-atomic) `File.WriteAllTextAsync`. This is deliberately the *simpler* of the two existing persistence patterns found in discovery (see Implementation Decision #2) — appropriate for a throwaway spike, not appropriate to carry forward to production.

**Methods:** Constructor taking a file path; implementations of T2's two methods; a private `Load`/`Save` pair mirroring `JsonIndexStateStore`'s.

**Tests:** Round-trip test (write, then read back, same process) — a **necessary but not sufficient** test; the real proof requires an actual process restart, which is T9's job, not T3's unit test.

**Dependencies:** T1, T2.

**Rollback strategy:** Delete the class; T2's interface remains usable by a future, real implementation without any change.

**Acceptance criteria:** A record written via this store, then read back via a **new instance** of the same class pointed at the same file, returns an equal record.

**Estimated complexity:** Low.

**Risk:** Medium — the specific risk is this "spike" quietly becoming the permanent implementation by default, exactly as the Readiness Checklist warns. Mitigation: the type name, a code comment, and this task's own description all say so; the Repository Integrity Check (Step 5) re-verifies this before Sprint 1 is considered closed.

---

### T4 — Request-Equivalence Check

**Purpose:** Realize ARCH-028 §3 (exact, contract-level equivalence) and ARCH-033 §4 (retrieval as lookup, not search) in minimal form — comparing two instances of Sprint 1's minimal request-identity shape (T1).

**New files:** One method or small type, e.g. a static `RequestEquivalence.AreEquivalent(...)` (name TBD), in the same project.

**Files modified:** None.

**Interfaces:** None required — a pure function is sufficient at this scale (per ARCH-033 §11, the internal data structure/algorithm is implementation freedom).

**Methods:** A single equality check over the three ARCH-028 §2 properties in their Sprint-1-minimal form.

**Tests:** Equivalent requests match; a request differing in path does not; TDD-first — write these two tests before the method exists.

**Dependencies:** T1.

**Rollback strategy:** Delete the method; nothing else exists yet to depend on it besides T7.

**Acceptance criteria:** Exactly reproduces ARCH-028 §4's "no partial/approximate form" rule — no test may pass for a near-match.

**Estimated complexity:** Trivial.

**Risk:** Low.

---

### T5 — Comparison Procedure

**Purpose:** Realize ARCH-033 §5 (comparison procedure, single dependency shape) and §3 (three outcomes) for Sprint 1's one-shape (source content) case.

**New files:** One method or small type, e.g. `ResolutionCheck.Compare(...)` (name TBD).

**Files modified:** None.

**Methods:** Given a recorded `AssetFingerprint` and a current one, and a flag for "record readable," return one of three outcomes: Satisfied (fingerprints equal), Not-satisfied (fingerprints differ), Indeterminate (record unreadable/missing). Mirrors `IndexPipeline.RunAsync`'s existing `storedFingerprint == computedFingerprint` comparison (lines 130–142 of `src/Ferret.Indexing/IndexPipeline.cs`) — the one already-working example of exactly this comparison in the codebase — narrowed to also produce Indeterminate, which `IndexPipeline` does not currently need because it always has a `NullIndexStateStore` fallback rather than a genuinely unreadable file.

**Tests:** Three tests, one per outcome, TDD-first.

**Dependencies:** T1 (the fingerprint type it compares).

**Rollback strategy:** Delete the method.

**Acceptance criteria:** All three ARCH-027 §3 outcomes are reachable and none other is ever produced.

**Estimated complexity:** Low.

**Risk:** Medium — this is the first place Indeterminate is actually exercised against a real failure condition (Milestone 5, Scenario C in the milestone plan). Do not skip that test case.

---

### T6 — Scan-Parse-Record-Persist Driver

**Purpose:** Compose Milestones 1–4 into one runnable path: scan one file (existing `FilesystemConnector`), parse it (existing `ParserDispatcher`), build a `DependencyRecord` (T1), persist it (T3 via T2).

**New files:** One orchestration class/method, e.g. `VerticalSliceDriver.ScanAndPersistAsync(...)` (name TBD).

**Files modified:** None — reuses `FilesystemConnector` and `ParserDispatcher` as-is; no changes to either.

**Interfaces consumed:** `IAssetReader.OpenAsync` (or the connector's `DiscoverAsync` for one path — **implementation decision, not architectural**: for a single known path, calling `OpenAsync` directly against a manually-constructed `AssetDescriptor`, or running `DiscoverAsync` and filtering to one match, are both reasonable; recommend the latter, since `BuildDescriptor` is private and `DiscoverAsync` is the only public path to a correctly-built `AssetDescriptor` without duplicating `FilesystemConnector`'s internal fingerprinting logic), `IParserDispatcher.DispatchAsync`, T2's store interface.

**Tests:** One integration-style test: given a real temp file, the driver produces a persisted record whose fingerprint matches the file.

**Dependencies:** T1, T2, T3, and the existing `FilesystemConnector`/`ParserDispatcher` (read-only reuse).

**Rollback strategy:** Delete the driver class; `FilesystemConnector` and `ParserDispatcher` are untouched, so nothing else regresses.

**Acceptance criteria:** Milestone 1–4's acceptance criteria (per the milestone plan) all pass through this single call path.

**Estimated complexity:** Medium — first task wiring existing and new code together.

**Risk:** Medium. `IndexPipeline` is the existing precedent for this exact composition (discover → dispatch → record fingerprint), but it is workspace/connector-manager-oriented (`IConnectorManager.GetActiveConnectorsAsync`) for multi-file runs. Building a minimal, single-file driver rather than extending `IndexPipeline` avoids pulling in workspace/connector-manager setup irrelevant to a one-file proof — noted here as a scoping call, not escalated as an Implementation Decision, since it is trivially reversible and affects no ARCH-032/033 guarantee.

---

### T7 — Resolve-and-Reuse Driver

**Purpose:** Compose Milestone 5 (locate, fetch, compare) and the reuse/recompute decision into one runnable path, callable from a **separate process invocation** than T6.

**New files:** One orchestration class/method, e.g. `VerticalSliceDriver.ResolveAndReuseAsync(...)`.

**Files modified:** None.

**Interfaces consumed:** T2 (fetch), T4 (equivalence), T5 (comparison), and — on Not-satisfied/Indeterminate — re-invokes T6.

**Tests:** Three integration tests, one per Milestone 5 scenario (A: unchanged → Satisfied; B: modified → Not-satisfied; C: record missing/corrupted → Indeterminate) — each run as a genuinely separate process from the one that persisted the record.

**Dependencies:** T2, T3, T4, T5, T6.

**Rollback strategy:** Delete the driver; T6 remains independently runnable and testable.

**Acceptance criteria:** All three scenarios in the milestone plan's Milestone 5 pass, with a real process boundary — not simulated.

**Estimated complexity:** Medium-High — first task exercising fail-closed under a genuine failure condition (Scenario C) rather than a happy path.

**Risk:** High relative to other tasks in this sprint — this is where ARCH-032/033's central guarantees are tested against reality for the first time. Do not treat Scenario C as optional; do not proceed to T8 until it passes.

---

### T8 — CLI Output Integration

**Purpose:** Realize ARCH-034 §1, §2 (indistinguishable output), corrected against the real `CommandResult`/`IFerretContext` shapes discovered in Step 1.

**Correction to the Sprint 1 milestone plan, discovered during this task's design:** `CommandResult` (`src/Ferret.Cli/Cli/CommandResult.cs`) is an `internal enum { Success = 0, Failure = 1, Cancelled = 130 }` — a process-exit signal, not a content carrier. The milestone plan's Milestone 6 description ("populate `CommandResult` from the persisted `Document` output") is **not actually true** of this codebase and must not be implemented as written. The real pattern, confirmed in `StatusCommandHandler.cs`: a handler writes content via `context.Services.Output.WriteLine(...)` (an `IFerretContext.Services.Output` abstraction) and separately returns a `CommandResult` value as the exit signal. ARCH-034 §2's *guarantee* (identical content regardless of reuse vs. recompute) is unaffected by this correction — only the mechanism by which content reaches the console changes.

**New files:** Either a new `ICommandHandler` implementation (if this slice is to be reachable as a real CLI command) or, for Sprint 1's proof purposes only, a test-only driver that calls the same `Output.WriteLine` path — **recommendation:** a real, minimal `ICommandHandler` implementation, since it costs little more than a test double and produces something a person can actually run, which better serves "Sprint 1 proves the architecture is implementable."

**Files modified:** Likely `src/Ferret.Cli`'s command registration (wherever `ICommandHandler` implementations are wired into the CLI's command table) — exact file not yet identified; identify it in this task, not assumed here.

**Interfaces consumed:** `ICommandHandler`, `IFerretContext`, T7.

**Tests:** One test asserting byte-identical `Output.WriteLine` content for the Satisfied path vs. the Not-satisfied/recompute path (ARCH-034 §2, now tested for real).

**Dependencies:** T7.

**Rollback strategy:** Remove the command registration and the handler class; no other command is touched.

**Acceptance criteria:** Milestone 6's acceptance criteria, corrected per the above: content written via `Output.WriteLine` is identical regardless of path; `CommandResult` returned is `Success` in both cases (or the appropriate value if either path can legitimately fail for unrelated reasons — e.g. file not found — which is not a reuse-vs-recompute distinction and must remain a distinction either path can produce identically).

**Estimated complexity:** Medium — first task touching the CLI's existing command-registration surface, which has not yet been located precisely.

**Risk:** Medium. The specific risk this task exists to prevent: a stray `Output.WriteLine` call (e.g., a debug/log line) reaching the same stream and breaking the indistinguishability assertion — this would be a real finding against ARCH-034 §2, not a test-flakiness issue to work around.

---

### T9 — End-to-End Composed-Sequence Test Suite

**Purpose:** Validate ARCH-035 §1's full composed sequence (request → retrieval → comparison → decision → reuse/recompute → surface) end-to-end, across a real process boundary, per the Overall Success Criteria in the milestone plan.

**New files:** One integration test project or test class (place alongside existing integration tests — `tests/Ferret.Integration.Tests` already exists per Step 1 discovery and is the established location for exactly this kind of cross-component test).

**Files modified:** None.

**Tests:** The three scenarios from T7, now exercised through T8's real CLI path rather than the driver directly — this is the difference between "the mechanism works" (T7) and "the mechanism works as a person would actually invoke it" (T9).

**Dependencies:** T1–T8, all complete.

**Rollback strategy:** N/A — a test suite; failing tests block the sprint from being declared complete, they don't get "rolled back."

**Acceptance criteria:** Matches the milestone plan's "Overall Success Criteria" verbatim.

**Estimated complexity:** Medium.

**Risk:** Low, provided T1–T8 are individually correct — this task's job is composition verification, not new logic.

---

## Implementation Dependency Graph

```
T1 (Record model)
 ├──► T2 (Store interface)
 │      └──► T3 (Spike store impl) ──┐
 ├──► T4 (Equivalence check) ────────┤
 └──► T5 (Comparison procedure) ─────┤
                                      ▼
                         T6 (Scan-Parse-Record-Persist)
                                      │
                                      ▼
                         T7 (Resolve-and-Reuse) ◄── requires T2,T3,T4,T5,T6
                                      │
                                      ▼
                         T8 (CLI Output Integration)
                                      │
                                      ▼
                         T9 (End-to-end composed-sequence tests)
```

T1 is the single root dependency for everything. T2/T3, T4, and T5 can proceed in parallel once T1 exists (three independent engineers or three sequential sessions — no ordering constraint between them). T6 needs all three. T7 needs T6 plus T4/T5 directly. T8 and T9 are strictly sequential after that.

---

## Commit Plan

Each commit corresponds to one task, in dependency order, each independently compilable and independently reviewable — no commit leaves the repository in a non-building or non-passing state.

| Commit | Task | Approx. lines | Single responsibility |
|---|---|---|---|
| 1 | T1 | < 100 | Add the dependency record type and its equality tests |
| 2 | T2 | < 50 | Add the persistence abstraction interface |
| 3 | T3 | < 150 | Add the spike store implementation and its round-trip test |
| 4 | T4 | < 80 | Add the request-equivalence check and its tests |
| 5 | T5 | < 120 | Add the comparison procedure and its three-outcome tests |
| 6 | T6 | ~150–250 | Add the scan-parse-record-persist driver and its integration test |
| 7 | T7 | ~200–300 (may need splitting — see below) | Add the resolve-and-reuse driver and its three scenario tests |
| 8 | T8 | ~150–250 | Add the CLI command handler and registration, plus the indistinguishability test |
| 9 | T9 | ~150–250 | Add the end-to-end composed-sequence test suite |

**On the ~300-line target:** Commit 7 (T7) is the most likely to exceed it, since it must cover three distinct scenarios (Satisfied/Not-satisfied/Indeterminate) each with real setup (a genuine process boundary). If it does, split it into 7a (Scenario A — happy path) and 7b (Scenario B+C — the two failure/change paths), since 7a alone already delivers a reviewable, meaningful increment (the reuse path working at all) before the harder failure-mode work lands.

---

## Test Plan

- **Unit tests** (TDD-first, per task): T1 (equality), T2 (none — interface), T3 (round-trip within one process), T4 (equivalence true/false), T5 (three outcomes).
- **Integration tests** (cross-component, single process): T6 (scan→parse→record→persist).
- **Integration tests requiring a genuine process boundary** (the one non-negotiable testing requirement from the milestone plan's Global Constraints): T7's three scenarios, and T9's repetition of them through the real CLI path. These must actually terminate and restart a process — an in-process "call the method twice" test does not satisfy ARCH-026 §1's bar and must not be accepted as a substitute.
- **What is explicitly not tested this sprint:** concurrency/multi-process access (out of scope per the milestone plan's Global Constraints), deletion handling (blocked, per Epic 2.4 of the Implementation Backlog), production storage/serialization correctness (spike only).

---

## Related

- `docs/superpowers/plans/2026-07-03-v2-sprint-1-vertical-slice.md` — the milestone plan this task breakdown implements; see its "Source Verification" section for the four files this design is grounded in
- `docs/superpowers/specs/2026-07-03-v2-sprint-1-readiness-checklist.md`
- `docs/002-Architecture/V2-IMPLEMENTATION-BACKLOG-001.md`
- [ARCH-032](../../002-Architecture/ARCH-032-Persistence-Mechanism-Design.md), [ARCH-033](../../002-Architecture/ARCH-033-Dependency-Resolution-Mechanism-Design.md), [ARCH-034](../../002-Architecture/ARCH-034-Surface-Integration-Mechanism-Design.md), [ARCH-035](../../002-Architecture/ARCH-035-Mechanism-Interaction-Model.md)
