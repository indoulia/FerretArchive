# ADR-0022 — Dependency-State Store: Local Filesystem with Atomic Writes

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-04 |
| **Deciders** | Ferret Core Team |
| **Sprint** | Sprint 2 (S2-2) |
| **Supersedes** | — |

---

## Context

ARCH-032 (Persistence Mechanism Design) §9 explicitly leaves the storage technology behind `IDependencyStateStore` as implementation freedom. Sprint 1 satisfied this under ADR-0001's triviality exemption with `SpikeDependencyStateStore` — a disposable, explicitly non-production implementation with no atomic writes, no concurrency handling, no versioning, and no deletion support (per its own doc comment).

The Sprint 2 Architecture Review (`docs/superpowers/plans/2026-07-04-v2-sprint-2-architecture-review.md`) identifies replacing this spike as the highest-priority Sprint 2 item (Epic 1.4): "nothing else can be validated at realistic scale while the only `IDependencyStateStore` implementation is a single-file, whole-file-rewrite JSON spike." The specific defect is that the spike's write path (`File.Create` directly on the target path, then serialize into it) is not crash-safe: a process killed or crashed mid-write leaves a truncated, unreadable file. ARCH-026 §7 already requires that an unreadable record be treated as unknown validity (safe), but a store that avoids producing unreadable records in ordinary crash scenarios in the first place is straightforwardly more production-ready than one that doesn't.

This decision is scoped narrowly to *where and how bytes are written to persist one record* — it does not revisit the wire format (S2-3), multi-record/indexed retrieval (S2-4), multi-artifact dependency chains (S2-6), corruption detection (S2-8), or retention/eviction (S2-9), all of which are separate, later Sprint 2 milestones.

A survey of every existing persistence mechanism in this repository (`JsonIndexStateStore`, `JsonWorkspaceStore`, `ConnectorInstanceStore`) found that all of them already use the local filesystem with JSON, and one of them — `ConnectorInstanceStore` (`src/Ferret.ConnectorPlatform/ConnectorInstanceStore.cs`) — already establishes a proven atomic-write pattern in this codebase: write to `<target>.tmp`, then `File.Move(tmpPath, target, overwrite: true)`.

## Decision

We will introduce `FileDependencyStateStore` (`src/Ferret.Persistence/FileDependencyStateStore.cs`) as the production `IDependencyStateStore` implementation. It keeps the exact same single-file, one-record-per-store shape and the exact same JSON representation as `SpikeDependencyStateStore` (no interface change, no wire-format change), and differs only in `SetRecordAsync`: it writes to a temporary file in the same directory and then atomically renames it over the target path, mirroring `ConnectorInstanceStore`'s existing, proven pattern rather than inventing a new one.

The composition root (`VerticalSliceCliModule.ConfigureServices` and `Ferret.VerticalSliceHost`'s `Program.cs`) now registers `FileDependencyStateStore` instead of `SpikeDependencyStateStore`. The spike is not deleted: it remains in `src/Ferret.Persistence/` and its own test suite (`SpikeDependencyStateStoreTests`) continues to pass, kept as a reference implementation per the Sprint 2 Architecture Review's explicit instruction, until it is fully superseded.

Multi-process concurrency safety (file locking) is deliberately not addressed here — the Sprint 2 Architecture Review lists it as a separate, later-or-never item (Epic 5.2) that applies only once a genuine multi-process usage exists beyond the already-tested sequential process-restart scenarios.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Keep `SpikeDependencyStateStore` as the registered implementation | Explicitly disposable per its own doc comment and ADR-0001's triviality exemption; not crash-safe; the Sprint 2 Architecture Review names replacing it as the highest-priority item. |
| SQLite (via `Microsoft.Data.Sqlite`, already present transitively in the solution) | No existing Ferret persistence mechanism uses a database; introduces a new dependency and a schema/migration concern for a single-record store with no query or indexing requirement yet (S2-4/S2-6 not started). Revisit if/when multi-record or indexed access is actually needed. |
| A new one-file-per-key directory layout, keyed by a hash of the request identity | Would let the store hold more than one record at once, but that is a key/lookup-structure decision explicitly reserved for a later milestone; today's vertical slice still only ever manages one record per store instance regardless of storage technology. |
| In-memory store with periodic flush | Reintroduces exactly the "reuse across process restarts" gap V2 exists to close (ARCH-026 §1); a fresh process would see no prior state until the next flush, or none at all if it exits first. |

## Consequences

### Positive
- A process crash or kill during a write can no longer leave a truncated, half-written record — `File.Move` is atomic at the OS level for a single writer, so the target file is always either the old complete record or the new complete record, never a partial one.
- Zero new external dependencies; matches every other persistence mechanism already in this codebase.
- `IDependencyStateStore`, `DependencyRecord`, `RequestEquivalence`, `ResolutionCheck`, and `ResolutionOutcome` are all untouched — this is purely a new implementation behind an existing, unchanged interface.
- The JSON on disk is byte-for-byte the same shape the spike already produced, so no migration step is needed and `SpikeDependencyStateStoreTests` continues to exercise a still-supported, still-correct implementation.

### Negative
- Still only one record per store instance — does not yet solve the "realistic scale, many records" problem; that remains for S2-4/S2-6.
- One additional file-system operation (`File.Move`) per write, and a `.tmp` file transiently exists on disk during a write — negligible cost for a single small JSON record.

### Neutral / Risks
- Concurrent writers to the same store from multiple processes are not made safe by this change (no locking) — unchanged risk profile from the spike, deliberately deferred to Epic 5.2.
- If a process crashes between `File.Create(tmpPath)` and `File.Move`, an orphaned `.tmp` file can be left on disk. It is never read by `GetRecordAsync` (which only reads the target path) and does not affect correctness; cleaning up stray temp files is a retention concern (S2-9), not addressed here.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-032](../002-Architecture/ARCH-032-Persistence-Mechanism-Design.md) | §9 leaves storage technology as implementation freedom — this ADR resolves it |
| [ARCH-026](../002-Architecture/ARCH-026-Persistence-Requirements.md) | §7's fail-closed principle, which this decision reduces the frequency of triggering (but does not alter) |
| [V2 Sprint 2 Architecture Review](../superpowers/plans/2026-07-04-v2-sprint-2-architecture-review.md) | Names Epic 1.4 (production storage backend) as S2-2, highest Sprint 2 priority |
| `src/Ferret.ConnectorPlatform/ConnectorInstanceStore.cs` | Existing atomic-write pattern this ADR reuses rather than reinventing |
