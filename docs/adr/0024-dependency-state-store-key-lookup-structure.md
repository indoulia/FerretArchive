# ADR-0024 — Dependency-State Store: Key/Lookup Structure

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-04 |
| **Deciders** | Ferret Core Team |
| **Sprint** | Sprint 2 (S2-4) |
| **Supersedes** | — |

---

## Context

Since S2-2, `FileDependencyStateStore` has been bound to exactly one fixed file, supplied to its constructor. `GetRecordAsync(engineResponsibility, requestPath)` and `SetRecordAsync(record)` already express a genuine compound-key lookup at the `IDependencyStateStore` interface level (ARCH-028 §2's request identity), but the S2-2/S2-3 implementation could only ever hold **one** record at a time: a second `SetRecordAsync` call for a different key would silently overwrite whatever the file already held for a different key, because there was nowhere else for it to go. The Sprint 2 Architecture Review's performance risk section names this directly: a store that cannot hold more than one record "will not survive Sprint 2's larger surface" once more than one dependency record needs to coexist.

The naive fix — store many records under one directory and find the right one by reading every file and comparing identities — reintroduces exactly the kind of linear/scanning lookup the review calls out as unsustainable (the same shape as `VerticalSliceDriver.FindDescriptorAsync`'s full directory enumeration, which is a separate, connector-side concern this ADR does not touch). S2-4's job is to give `FileDependencyStateStore` a real per-key lookup without reaching for either extreme: neither a single-slot store, nor a linear scan, nor a full external index/database.

This decision is scoped narrowly to *how a key maps to a location* — it does not touch the storage mechanism decided in ADR-0022 (still local filesystem, still atomic temp-file-then-rename per write) or the per-record wire format decided in ADR-0023 (the envelope written at whatever path is resolved is byte-for-byte identical to before).

## Decision

We will compute each record's file location directly from its key, rather than storing it at a caller-supplied fixed path or discovering it by scanning a directory. `FileDependencyStateStore`'s constructor argument becomes a root directory rather than a single file. A new private method, `GetRecordFilePath(engineResponsibility, requestPath)`, joins the two request-identity components with a NUL separator, hashes the result with SHA-256, and returns `<rootDirectory>/<hex-hash>.json`. Both `GetRecordAsync` and `SetRecordAsync` call this same function to resolve the file for a given key before doing any I/O.

This is a pure function of the key: given the same two identity strings, it always returns the same path, in O(1) time, with no directory read and no separate index/manifest file to keep consistent with what is actually on disk. The defensive identity check already present since S2-2 (comparing the envelope's own `engineResponsibility`/`requestPath` against the query) is kept — it now guards against a genuine SHA-256 collision or a manually edited file, rather than being the store's only means of distinguishing one record from another.

The change is confined to `FileDependencyStateStore`; `IDependencyStateStore`'s signatures, `DependencyRecord`, and the JSON envelope from ADR-0023 are all unchanged. Callers (`VerticalSliceCliModule`, `Ferret.VerticalSliceHost`'s `Program.cs`) pass the same string value they already had (the CLI-supplied `storePath`) into the constructor unchanged — from the outside, `FileDependencyStateStore` still looks like "give me a path, get a store"; only its internal treatment of that path (as a directory root rather than one target file) changed. Higher layers (`VerticalSliceDriver`, `VerticalSliceCommandHandler`, `RequestEquivalence`, `ResolutionCheck`) never see a file path, a hash, or a directory listing — they only ever see `DependencyRecord`, `ResolutionOutcome`, and the two identity strings already part of `IDependencyStateStore`'s contract.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Keep the single-fixed-file design (status quo from S2-2/S2-3) | Cannot hold more than one record at a time; explicitly named as the Sprint 2 Architecture Review's performance risk to close before more of Sprint 2 builds on this store. |
| Store all records in one directory, found by scanning every file and comparing identities on each `GetRecordAsync` | Reintroduces the linear-lookup problem this milestone exists to eliminate; lookup cost grows with the number of records ever written. |
| A separate index/manifest file (key → filename) maintained alongside the record files | A real "index structure" with its own consistency and atomic-update burden (the manifest itself must never fall out of sync with the files it describes) — heavier than this milestone's scope, and unnecessary when a direct hash computation gives an equivalent O(1) answer with nothing to keep in sync. |
| A real embedded database (SQLite, LiteDB) | Same rejection as ADR-0022: no dependency this repository doesn't already avoid for exactly this kind of small, single-writer state, and no query/indexing requirement beyond point lookup by an already-fixed compound key. |

## Consequences

### Positive
- `FileDependencyStateStore` can now hold any number of distinct records, each independently retrievable, which S2-5/S2-6 need once more than one dependency shape/artifact exists.
- Lookup and write are both O(1) in the number of other records the store holds — no scan, no index file to synchronize.
- `IDependencyStateStore`, `DependencyRecord`, `RequestEquivalence`, `ResolutionCheck`, `ResolutionOutcome`, and the ADR-0023 wire format are all untouched.
- No change to the vertical slice's observable CLI behavior or process-boundary test scenarios — the same `storePath` string still flows through the same call sites; it is simply now the root of a directory instead of a single filename.

### Negative
- File names on disk are now opaque hashes rather than a human-recognizable single file — a minor debuggability cost, accepted because nothing outside this class was ever meant to read the store's files directly.
- A SHA-256 hash of two short strings is a very small amount of extra CPU work per call, negligible next to the file I/O it precedes.

### Neutral / Risks
- A hash collision would make two distinct keys resolve to the same file; the retained identity check inside the envelope prevents a collision from ever returning the wrong record — it returns null (safe, forces recompute) rather than silently attributing one key's answer to another.
- The root directory is never cleaned up by this class — an ever-growing set of per-key files is a retention concern (S2-9), not addressed here.

---

## Cross References

| Document | Relationship |
|---|---|
| [ADR-0022](0022-dependency-state-store-filesystem-backend.md) | Storage mechanism (local filesystem, atomic writes) this ADR builds on without changing |
| [ADR-0023](0023-dependency-record-serialization-format.md) | Wire format this ADR builds on without changing |
| [V2 Sprint 2 Architecture Review](../superpowers/plans/2026-07-04-v2-sprint-2-architecture-review.md) | Names Epic 2.5 (key/lookup structure) as S2-4 and its performance-risk rationale |
