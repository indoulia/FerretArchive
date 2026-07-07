# V2 Sprint 2 — Architecture Review

| Field | Value |
|---|---|
| **Date** | 2026-07-04 |
| **Scope** | Retrospective on V2 Sprint 1 (Tasks T1–T9, ARCH-023 through ARCH-036, AGR-001 through AGR-004, ADR-0021) and forward planning for Sprint 2 |
| **Status** | Draft — for review; no implementation authorized by this document |
| **Author's note** | Repository-first: every claim below is traced to a specific doc section or file:line found in the current working tree, not to prior conversation |

---

## Revision Note (2026-07-04, post-approval refinements)

This document's Sprint 2 milestone plan (§4) and prioritized roadmap (§6) were approved with three refinement requests, incorporated below without reopening §1–§3 (Sprint 1 retrospective, deferred work, technical debt review — unchanged) or redesigning Sprint 1 itself:

1. **S2-1 split in two.** What was a single milestone (fix Sprint 1 layering debt) is now S2-1A (dependency inversion — inject `IDependencyStateStore` instead of constructing `SpikeDependencyStateStore` directly) and S2-1B (correct the vertical-slice assembly's dependency direction). Rationale for the split and its internal order is in §4.
2. **New milestone S2-0 (Architecture Regression Protection)**, sequenced first. It extends the existing `tests/Ferret.Architecture.Tests` project (reflection-based `xunit` checks, e.g. `ConnectorArchitectureTests.cs`) rather than introducing a second architecture-testing framework. See §4.1.
3. **S2-8/S2-9 swapped.** Corruption/unreadability detection now precedes retention/eviction policy (renumbered S2-8 and S2-9 respectively, reversing their original order). Rationale in §4 and §6.

---

## 0. Method

This review reads, and does not re-derive, three independent evidence sources:

1. The frozen conceptual kernel and mechanism-tier design docs: `ARCH-023` through `ARCH-036`, `V2-IMPLEMENTATION-BACKLOG-001`, `V2-ROADMAP-001-Architecture-Program`, `ADR-0021`.
2. The governance record: `AGR-001` through `AGR-004`, the Sprint 1 vertical-slice plan, the T1–T9 task breakdown, and the Sprint 1 readiness checklist.
3. The actual Sprint 1 code: `src/Ferret.Persistence/`, `tests/Ferret.Persistence.Tests/`, `tests/Ferret.VerticalSliceHost/`, and the `VerticalSlice*.cs` files in `tests/Ferret.Integration.Tests/`.

Where the code and the docs agree, that is called out as validated. Where they diverge, that divergence is itself a finding.

---

## 1. Sprint 1 Retrospective

Sprint 1 shipped six milestones (Milestones 1–6 in the vertical-slice plan, realized as tasks T1–T9). For each, the question is: what does it own, is that placement correct, and should it survive into Sprint 2 unchanged.

| Milestone | Responsibility | Correctly placed? | Should remain unchanged? |
|---|---|---|---|
| **M1 — Scan One File** | Discover a file via the existing Filesystem Connector (`FilesystemConnector.DiscoverAsync`) | Yes — no new code, delegates entirely to the Connector Platform, which ARCH-023 already names as an owning component | Yes |
| **M2 — Parse the File** | Turn a discovered asset into a `Document` via the existing Parser Platform (`ParserDispatcher`, `PlainTextParser`) | Yes — same reasoning, Parser Platform already owns this | Yes |
| **M3 — Build the Dependency Record** | Model one dependency (source-content fingerprint) as data, per ARCH-032 §2 | Yes — `DependencyRecord` is a pure data shape in the new `Ferret.Persistence` project, names no storage technology, matches the mechanism doc exactly | Yes, as a contract. It will need a second, additive shape once configuration/registration dependencies (Epic 1.8) are captured — that is growth, not rework |
| **M4 — Persist the Record (Spike Store)** | Provide `IDependencyStateStore` and one disposable implementation | Interface placement is correct. Implementation placement has one defect: `VerticalSliceCommandHandler` constructs `SpikeDependencyStateStore` directly instead of depending on `IDependencyStateStore` — the one place meant to demonstrate swappable persistence hardcodes the concrete spike type (`VerticalSliceCommandHandler.cs:27`) | The abstraction should remain; the concrete spike store is explicitly disposable per its own doc comment and per ADR-0021's ADR-0001 triviality exemption. The hardcoded construction should not remain — see §3 |
| **M5 — Reload and Resolve** | Decide Satisfied / Not-satisfied / Indeterminate across a genuine process restart | Yes — `RequestEquivalence` and `ResolutionCheck` are pure functions with no I/O, matching ARCH-033's framing of resolution as a decision procedure distinct from retrieval. The fail-closed guarantee (corrupted record → Indeterminate, never Satisfied) is proven by a real test, not asserted | Yes, as a contract. The comparison itself is currently single-shape, exact-match only — correct for Sprint 1's declared scope boundary, will need extension once multiple dependency shapes exist (Epic 2.6) |
| **M6 — Reuse and Verify Identical Output** | Branch CLI output on the resolution outcome without adding any new CLI surface | Structurally correct against ARCH-034 (no new command/flag/field was added, output is byte-identical either path). Location is unusual: the entire vertical slice — driver, CLI module, command handler — lives under `tests/Ferret.Integration.Tests/`, and the new `Ferret.VerticalSliceHost` executable (built to prove a real process boundary) has a `ProjectReference` to that test assembly to get its logic | This was a deliberate, documented choice ("proof-of-concept only, deliberately kept out of production `src/`") and was the right call for Sprint 1 — it let the team validate ARCH-032/033/034 without prejudging whether or how this ever reaches the real CLI. It should not persist unchanged once Sprint 2 does anything beyond validation — see §3 and §5 |

**Overall verdict on Sprint 1's architectural placement:** correct at the contract level (`DependencyRecord`, `IDependencyStateStore`, `ResolutionOutcome`, `RequestEquivalence`, `ResolutionCheck`), with two placement defects that are cheap to fix now and expensive to fix after more is built on top: the hardcoded concrete store in the command handler, and the test-assembly-as-production-dependency shape of the vertical slice. Neither defect contradicts any frozen architectural decision — both are implementation choices Sprint 1 was free to make differently and simply didn't, under time pressure to prove the flow end-to-end first.

---

## 2. Deferred Work

Every item below is deferred by explicit statement in the architecture, backlog, or roadmap documents — none of this is inferred.

| Capability | Why deferred | Address in Sprint 2? |
|---|---|---|
| **Production persistence backend** (Epic 1.4) | Sprint 1 used a disposable spike under the ADR-0001 triviality exemption specifically so Sprint 1 would not be blocked on a storage-technology ADR | **Yes — highest priority.** Nothing else can be validated at realistic scale while the only `IDependencyStateStore` implementation is a single-file, whole-file-rewrite JSON spike |
| **Production serialization format** (Epic 1.5) | Same reasoning as above; ARCH-032 §9 explicitly names this an open "implementation freedom" | Yes, paired with the storage backend decision — they are usually the same ADR in practice |
| **Retention / eviction policy** (Epic 1.6) | ARCH-026 §5 explicitly declines to define one, calling it "a mechanism decision" | Only after the storage backend exists — an eviction policy for a store you're about to replace is wasted design |
| **Corruption / unreadability detection mechanism** (Epic 1.7) | Sprint 1 proved the *outcome* (corrupted record → Indeterminate) with one deliberately-corrupted-record test, not a designed detection strategy | Yes, but after the storage backend — detection strategy is backend-shaped (e.g. checksum vs. format-level validation vs. read failure) |
| **Configuration / registration dependency capture** (parser version, connector config — dependency shape 4) | ARCH-026 §3 states this is "currently unmet for any component"; model/provider configuration identity ownership is flagged **Unassigned** | Yes — this is the prerequisite for richer dependency graphs, and closing "Unassigned" ownership is overdue |
| **Dependency-chain combination** (multi-artifact, Epic 2.3) | Depends on Epic 1.8 (above) existing first | Yes, once 1.8 lands — this is what turns the single-fact record into an actual graph |
| **Key / lookup structure for resolution retrieval** (Epic 2.5) | Sprint 1's readiness checklist explicitly accepted "linear/direct lookup" at Sprint-1 scale | Yes — the current `FindDescriptorAsync` does a full directory enumeration per lookup; this will not survive Sprint 2's larger surface (see §5, performance risk) |
| **Comparison / combination algorithm for multi-shape sets** (Epic 2.6) | Needs the multi-shape dependency model from 1.8/2.3 first | Yes, following 2.3 |
| **Deletion detection and handling** (Epic 2.4) | Recorded as a genuine **unresolved conceptual gap** (not a technology choice) across ARCH-025 §4, ARCH-030 §2, ARCH-032 §9, ARCH-033 §11 — "how a deletion signal is produced" is explicitly not decidable by an ADR alone | **No.** This is explicitly blocked pending a new governance review (ADR-0021 Rule 6). Sprint 2 should not attempt a deletion path |
| **Production concurrency / multi-process model** (Epic 5.2) | Sprint 1 satisfied ADR-0021 Rule 5 with a single-process scope statement, not a solution; the gap was found only during the mechanism-layer review and is addressed nowhere in ARCH-023–036 | Optional — only if Sprint 2 introduces a genuine multi-process usage. Otherwise defer to when that usage is real, per the backlog's own sequencing |
| **RM-05 — AI Integration Architecture** | ADR-0021 Rule 3: "deferred, not abandoned... becomes blocking the moment an AI-derived artifact... enters the reuse path" | No — no Sprint 2 work plans to invoke `IModelProvider` from this path |
| **RM-06 — formal Benchmarking Architecture** | Superseded in practice by extending the existing benchmark suite rather than writing a new ARCH document | No new document; extending the suite (Epic 5.4) is separately sequenced after Sprint 2 proves the flow at scale |
| **Extend benchmark suite with V2 metrics** (Epic 5.4) | Explicitly sequenced "after Sprint 1 proves the flow, before Phase VI benchmarking" | Not yet — Sprint 2 is still proving correctness at scale, not measuring it |
| **`docs/006-CLI/`, `docs/007-SDK/`, `docs/005-MCP/` reconciliation** | ARCH-034 found these are placeholder scaffolding, one describing a REST API with no evidence in `src/`; explicitly called a documentation-maintenance question outside that document's scope | No — unrelated to Sprint 2's architecture work, worth a separate docs cleanup ticket |
| **Review Engine, Specification Engine, Artifact Engine, Memory Engine implementations; `IReranker`, `IVisionModel`, semantic search, `IAssetEnricher`, plugin SDK, telemetry** | Pre-existing V1 gaps (ARCH-024 §10), untouched by and unrelated to the V2 program | No — out of scope for the V2 architecture entirely |

---

## 3. Technical Debt Review

### Acceptable temporary code (leave as-is, on its already-scheduled timeline)

- **`SpikeDependencyStateStore`** — self-documents as non-production, covered by the ADR-0001 triviality exemption, already scheduled for replacement (Epic 1.4/1.5). This is debt Sprint 1 was explicitly authorized to take on.
- **Single dependency shape, exact-match-only request equivalence** — matches Sprint 1's declared scope boundary exactly; extension is scheduled (Epic 1.8, 2.3, 2.6), not forgotten.
- **Hardcoded single parser (`PlainTextParser` only) in the vertical-slice driver** — intentional narrowing to keep the slice minimal; the real Parser Platform already supports more formats and is untouched.
- **No deletion-path code** — correct: implementing one now would violate ADR-0021 Rule 6 (unresolved conceptual gap requires escalation, not implementation).

### Code that should be replaced before Sprint 2 builds further on it

- **`VerticalSliceCommandHandler` directly instantiates `SpikeDependencyStateStore`** (`VerticalSliceCommandHandler.cs:27`) instead of receiving `IDependencyStateStore` through the constructor. This defeats the one architectural point of having the interface. Cheap fix, should happen before Sprint 2's storage-backend work gives this class a second implementation to hardcode incorrectly.
- **`Ferret.VerticalSliceHost` (an executable) has a `ProjectReference` to `Ferret.Integration.Tests` (a test project)** to obtain `VerticalSliceDriver`, `VerticalSliceCliModule`, and `VerticalSliceCommandHandler`. This is a backwards dependency direction (production-shaped code depending on a test assembly) that was acceptable for a throwaway Sprint 1 spike but should not be the shape Sprint 2 extends. See §5 for the recommended fix.
- **Duplicated `Path.Join(rootPath, fileName)` logic in three places** (`VerticalSliceDriver.cs` twice, `VerticalSliceCommandHandler.cs:26`, `Program.cs:27`) — should collapse to one helper before a fourth call site appears.
- **`FindDescriptorAsync` re-runs a full directory enumeration and constructs a fresh `FilesystemConnector` on every call**, including once inside the resolve path and again inside the recompute path for the same invocation. Acceptable at Sprint 1's single-file toy scale; should not survive into Sprint 2's key/lookup-structure work unexamined.
- **`VerticalSliceHostRunner` has no timeout on the child-process wait.** A hung host process today hangs the test run, not just the assertion. Cheap fix, should land before Sprint 2 adds more subprocess-based tests on top of this harness.
- **Fingerprint-comparison logic now exists in two places** — the pre-existing `IndexPipeline.RunAsync` and the new `ResolutionCheck` — without being unified. Not urgent, but worth a single follow-up ticket so the two don't silently diverge.

### Code that should remain permanently

- **`DependencyRecord`, `IDependencyStateStore`, `ResolutionOutcome`, `RequestEquivalence`, `ResolutionCheck`** as contracts — these correctly realize ARCH-032/033 and should be the stable foundation Sprint 2 extends, not replaces.
- **The fail-closed guarantee** (unreadable or corrupted record → `Indeterminate`, never `Satisfied`) — proven by a real test, must never be weakened by any future storage backend.
- **The practice of writing an explicit scope-boundary statement instead of silently doing nothing** (e.g. the single-process declaration satisfying ADR-0021 Rule 5) — this is good discipline and should continue for every future gap Sprint 2 finds that it chooses not to solve immediately.

---

## 4. Proposed Sprint 2 Milestones

Each introduces exactly one architectural concept. Priorities are inherited from `V2-IMPLEMENTATION-BACKLOG-001`.

| # | Milestone | Concept | Backlog ref | Depends on |
|---|---|---|---|---|
| S2-0 | Architecture regression protection | Encode the invariants Sprint 2 must not silently violate as automated tests, extending `tests/Ferret.Architecture.Tests` | — (governance/quality gate, not a backlog epic) | none |
| S2-1A | Dependency inversion | `VerticalSliceCommandHandler` receives `IDependencyStateStore` via constructor instead of constructing `SpikeDependencyStateStore` directly | — (debt cleanup, not a backlog epic) | S2-0 |
| S2-1B | Correct vertical-slice dependency direction | Move vertical-slice logic (`VerticalSliceDriver`, `VerticalSliceCliModule`, `VerticalSliceCommandHandler`) out of `tests/Ferret.Integration.Tests` so `Ferret.VerticalSliceHost` no longer references a test assembly | — (debt cleanup, not a backlog epic) | S2-1A |
| S2-2 | Production storage backend | One ADR-backed storage technology behind the existing `IDependencyStateStore` interface, no interface change | Epic 1.4 | S2-1B |
| S2-3 | Production serialization format | One ADR-backed serialization format for persisted records | Epic 1.5 | S2-2 |
| S2-4 | Key / lookup structure | Replace linear directory-scan lookup with a real index/key structure for resolution retrieval | Epic 2.5 | S2-2 |
| S2-5 | Configuration / registration dependency capture | Add parser-version and connector-config as a second, additive dependency shape; resolve the "Unassigned" model/provider ownership gap | Epic 1.8 | S2-2 |
| S2-6 | Multi-artifact dependency-chain combination | Extend `DependencyRecord` and resolution to a dependency *set*, not a single fact | Epic 2.3 | S2-5 |
| S2-7 | Comparison algorithm for multi-shape sets | Extend `ResolutionCheck` to combine outcomes across a dependency set per ARCH-029's Not-satisfied > Indeterminate > Satisfied rule | Epic 2.6 | S2-6 |
| S2-8 | Corruption / unreadability detection | Replace today's ad hoc `catch JsonException` with a designed detection strategy for the chosen backend | Epic 1.7 | S2-2 |
| S2-9 | Retention / eviction policy | Define and implement what happens to superseded dependency records | Epic 1.6 | S2-2, S2-8 |

Explicitly **not** proposed for Sprint 2: deletion detection (Epic 2.4, blocked pending escalation), production concurrency model (Epic 5.2, only needed once a real multi-process usage exists), benchmark suite extension (Epic 5.4, sequenced after the above), RM-05 AI Integration (not yet triggered).

### Why S2-1 is now two milestones, in this order

S2-1A and S2-1B fix two different Technical Debt Review findings (§3) that happen to sit in the same file today, but they are different architectural concepts: S2-1A is about a *class's* relationship to an abstraction (does it ask for `IDependencyStateStore` through its constructor, or does it hardcode a concrete type it should not know about); S2-1B is about an *assembly's* relationship to another assembly (does `Ferret.VerticalSliceHost`, production-shaped code, depend on `Ferret.Integration.Tests`, a test assembly). A reviewer could accept one without the other — e.g. approve the DI fix while still disputing where the code should physically live — so collapsing them into one milestone forced an all-or-nothing review of two unrelated questions.

**S2-1A before S2-1B, not the reverse.** If S2-1B (the file move) happened first, the moved file would carry the hardcoded-`SpikeDependencyStateStore` defect into its new home, and S2-1B's diff — which should be a mechanical relocation plus reference-direction fix — would also have to explain a behavior change, mixing "moved" and "changed" in one reviewable unit. Doing S2-1A first means S2-1B's diff is a clean move: no production logic changes, only which assembly the (already-corrected) class lives in and which project references which.

### Why S2-8 and S2-9 are swapped

The original order listed retention/eviction (then S2-8) before corruption/unreadability detection (then S2-9). Reconsidering per the approved refinement request: ARCH-026 §5 leaves a superseded record's disposition — "overwrite it, retain it, or discard it" — as an explicit mechanism decision, and ARCH-026 §7 requires that a record which is "missing, corrupted, or unreadable" be treated as unknown validity, never assumed. A retention/eviction policy has to decide what happens to a record it cannot read well enough to tell whether it has been superseded — and it cannot state that rule (e.g. "unconditionally evict/quarantine corrupted records rather than attempting the ordinary supersession comparison") until a corruption/unreadability detection mechanism exists to produce the classification retention would branch on. Corruption detection does not have the same dependency on retention: it only needs the chosen storage backend (S2-2) to know what "unreadable" looks like for that backend. Retention/eviction (S2-9) therefore now depends on both S2-2 and S2-8, where previously the two milestones (then numbered S2-8 and S2-9) had no dependency edge between them at all — that missing edge, not just the ordering, was the actual gap the refinement request surfaced.

---

## 4.1 S2-0 — Architectural Invariants for Regression Protection

Per the approved refinement request, these extend the existing `tests/Ferret.Architecture.Tests` project (reflection-based `xunit` assertions, same style as `ConnectorArchitectureTests.cs` — no IL-inspection library or second test framework introduced). Proposed new file: `tests/Ferret.Architecture.Tests/PersistenceArchitectureTests.cs`.

| Invariant | Protects | Check (reflection-based) | Expected state today |
|---|---|---|---|
| `VerticalSliceCommandHandler` must depend on `IDependencyStateStore`, not construct `SpikeDependencyStateStore` | Abstraction boundary (§3 finding: `VerticalSliceCommandHandler.cs:27`) | Constructor has a parameter of type `IDependencyStateStore`; no field/parameter of concrete type `SpikeDependencyStateStore` | **Red.** Fails against current Sprint 1 code; must turn green when S2-1A lands |
| The assembly containing the vertical-slice CLI module must not reference any `*.Tests` assembly | Dependency direction (§3 finding: `Ferret.VerticalSliceHost` → `Ferret.Integration.Tests`) | `GetReferencedAssemblies()` on that assembly contains no name ending in `.Tests` | **Red.** Fails against current Sprint 1 code; must turn green when S2-1B lands |
| `RequestEquivalence.AreEquivalent` is `static` and not awaitable | Purity of comparison/equivalence (ARCH-028 §3, ARCH-033 §11 — implementation freedom, but must stay a pure function) | Method is `static`; return type is not `Task`, `Task<T>`, or `ValueTask<T>` | Green today — guards against a future milestone (e.g. S2-5's config capture) accidentally adding I/O to make equivalence "smarter" |
| `ResolutionCheck.Compare` is `static` and not awaitable | Same as above, for the comparison procedure (ARCH-033 §5) | Same shape of check | Green today — same forward-looking guard, most relevant once S2-7 extends this method to multi-shape sets |
| `IDependencyStateStore`'s methods name no storage technology or format in their signatures | Abstraction boundary (T2's original acceptance criterion — "names no technology, file format, or key structure" — currently verified only by inspection) | Parameter/return types of the interface's methods are drawn only from `Ferret.Core`/`Ferret.Persistence` model types (e.g. `DependencyRecord`), never a technology-specific type (e.g. no `SqliteConnection`, `JsonDocument`, `FileStream`) | Green today — this is what turns T2's one-time manual inspection into a permanent gate before S2-2 gives the interface a second, real implementation |

The first two rows are deliberately written to be **red before S2-1A/S2-1B and green after** — they are the acceptance criteria for those two milestones, not just future protection, which is why S2-0 is sequenced before them rather than after. The last three rows are green today and exist to stop later milestones (S2-5 through S2-9) from eroding contracts Sprint 1 already got right.

---

## 5. Risks

- **Layering risk.** The Sprint 1 vertical slice already has one backwards dependency (`Ferret.VerticalSliceHost` → `Ferret.Integration.Tests`, fixed by S2-1B) and one hardcoded-concrete-type spot where an abstraction should have been used (fixed by S2-1A). Both are cheap to fix today and will get more expensive the longer S2-2 through S2-9 build on top of them — S2-0 now makes both regressions impossible to reintroduce silently once fixed.
- **Dependency-direction risk.** ARCH-023 is explicit that V2 owns no primary data and depends on exactly eight named V1 components. S2-5's configuration/registration capture must read parser and connector identity through those components' existing surfaces, not by reaching into their internals — the same discipline that kept Sprint 1 clean.
- **Performance risk.** `FindDescriptorAsync`'s O(n) directory scan and the spike store's whole-file rewrite are fine for Sprint 1's one-file proof but will not survive S2-6's multi-artifact chains without S2-4's key/lookup structure landing first. Sequencing S2-4 before S2-6 (as proposed above) avoids validating correctness on a structure that can't hold the data.
- **Maintainability risk.** Three copies of the same path-join logic and two independent implementations of fingerprint comparison (one in `IndexPipeline`, one in `ResolutionCheck`) are small today; each new milestone that touches either without unifying them widens the drift.
- **Testability risk.** No concurrency test exists anywhere in the slice, and `VerticalSliceHostRunner` has no process timeout — a hang becomes a hung test run, not a failed assertion. This should be fixed (alongside S2-1B, cheaply, since it touches the same test harness during the move) before Sprint 2 adds more subprocess-based tests on the same harness.
- **Governance risk.** Deletion detection (Epic 2.4) is a standing temptation once S2-6's dependency chains make "what if a dependency is deleted" a natural next question. Any Sprint 2 work that starts sketching a deletion path without first getting a new governance review would violate ADR-0021 Rule 6.

---

## 6. Recommendations — Prioritized Sprint 2 Roadmap

1. **S2-0 (architecture regression protection) first.** Write and run the invariant tests in §4.1 against the current, unmodified Sprint 1 code. Two of them (constructor-injection, no test-assembly reference) must be observed **red** at this point — that observation is what proves the suite actually detects the defects it claims to, rather than passing vacuously. The other three are expected green and serve as a baseline snapshot before anything else changes.
2. **S2-1A (dependency inversion), then S2-1B (assembly direction).** Each turns one of S2-0's two red checks green; doing them in this order (see §4) keeps S2-1B a pure relocation with no behavior change riding along. Confirm both checks are green before proceeding — this is the gate, not a suggestion.
3. **S2-2 then S2-3 (storage backend, serialization).** These are the load-bearing blockers: every other Sprint 2 milestone either persists more data through this layer or depends on it being real. They are grouped because they are typically resolved by the same ADR.
4. **S2-4 (key/lookup structure) next, before volume grows.** It directly addresses the performance risk above and is naturally sequenced with the storage backend since both touch the store's internal structure — doing it after S2-6 would mean redesigning the lookup a second time once chains exist.
5. **S2-5 (configuration/registration capture), then S2-6 (chain combination), then S2-7 (multi-shape comparison).** This is a strict dependency chain: you cannot combine dependencies that don't yet have a second shape, and you cannot compare across a set until the set exists.
6. **S2-8 (corruption/unreadability detection), then S2-9 (retention/eviction) — reordered.** Both still harden a store that must already be correct and backend-real first, so both wait for S2-2. But retention/eviction cannot fully specify what happens to a record it cannot read (ARCH-026 §5's "overwrite it, retain it, or discard it" versus ARCH-026 §7's fail-closed treatment of unreadable state) until corruption detection exists to classify that case — so corruption detection now comes first, and retention/eviction depends on it directly. Designing either against the spike, or corruption detection against an as-yet-undecided backend, would still be wasted work.
7. **Explicitly out of Sprint 2:** deletion detection stays blocked until a new governance review is requested; the production concurrency model waits for an actual multi-process usage; benchmark-suite extension waits until the above prove the flow at realistic scale; RM-05 waits for an actual AI-derived artifact to enter the reuse path.

No code is authorized by this document. Awaiting approval before any Sprint 2 milestone begins.
