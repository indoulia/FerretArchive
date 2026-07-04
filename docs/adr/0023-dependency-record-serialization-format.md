# ADR-0023 — Dependency-Record Serialization Format

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-04 |
| **Deciders** | Ferret Core Team |
| **Sprint** | Sprint 2 (S2-3) |
| **Supersedes** | — |

---

## Context

ADR-0022 decided *where and how* `FileDependencyStateStore` persists a record (local filesystem, atomic write via temp-file-then-rename) but explicitly deferred *what bytes are written* — the wire format — to this ADR, keeping the exact same ad hoc JSON shape `SpikeDependencyStateStore` already used (direct `System.Text.Json` serialization of `DependencyRecord`, default PascalCase property names, no schema marker, nulls written literally).

That shape has three gaps relative to "production serialization format":
1. No schema version is recorded, so a future format change has no marker to gate on.
2. Property names come from reflection over `DependencyRecord`'s C# member names, coupling the wire format to a domain type's implementation detail rather than to a deliberate contract.
3. Serializing `DependencyRecord` directly means any future serialization-only concern (attributes, ignore conditions, versioning) would have to live on the domain type itself — the same type `RequestEquivalence`, `ResolutionCheck`, and `VerticalSliceDriver` consume — rather than staying confined to the storage layer.

A repository-wide survey (done for ADR-0022) found `ConnectorInstanceStore` (`src/Ferret.ConnectorPlatform/ConnectorInstanceStore.cs`) already establishes exactly this pattern for another Ferret store: an explicit, private JSON DTO (`JsonConnectorsFile`/`JsonConnectorInstance`) with `[JsonPropertyName]` on every property, `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, and an embedded `schemaVersion` field.

## Decision

We will serialize through a private envelope type, `FileDependencyStateStore.JsonDependencyRecordEnvelope` (with a nested `JsonAssetFingerprint` DTO for the fingerprint), rather than serializing `DependencyRecord` directly:

- **Format:** JSON (unchanged from Sprint 1/S2-2 — no new dependency, matches every other Ferret store).
- **Property names:** explicit `camelCase`, set via both `[JsonPropertyName]` on every envelope property (so casing is pinned regardless of any C# member rename) and `JsonNamingPolicy.CamelCase` on the shared options, mirroring `ConnectorInstanceStore` exactly.
- **Schema version:** every write embeds `"schemaVersion": "1.0"`. Every read accepts whatever version is present and maps it into a `DependencyRecord` without acting on it — this ADR intentionally does not add version-gated rejection or migration logic; it only ensures the *data needed for that later behavior exists on disk from now on*. `ConnectorInstanceStore` establishes the same precedent (it embeds a schema version primarily for a narrow present use — gating a backup — without a general migration engine).
- **Null handling:** `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` — a null `PlainText` is omitted from the file rather than written as `"plainText": null`.
- **Domain-type isolation:** `DependencyRecord` (and `AssetFingerprint`, a `Ferret.Core` type used far beyond this store) receive no serialization attributes and no changes at all. All format-specific detail — property names, the schema version field, null-omission — lives entirely inside `FileDependencyStateStore`'s two private DTOs and its `ToRecord`/`ToEnvelope` mapping methods.

This keeps the format deterministic (a fixed, explicit shape with no reflection-derived surprises — verified by a same-input round-trip byte-equality test) and versionable in the future (the marker exists; the behavior gated on it does not need to exist yet).

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Keep serializing `DependencyRecord` directly (status quo from Sprint 1/S2-2), PascalCase, no envelope | No schema version to evolve from later; couples the wire format to the domain type's member names; inconsistent with the one other "production" JSON store already in this codebase. |
| Add `[JsonPropertyName]`/schema-version properties directly to `DependencyRecord` | Leaks a storage-format concern into a domain type consumed by `RequestEquivalence`, `ResolutionCheck`, and `VerticalSliceDriver` — exactly what the "avoid introducing serialization concerns into higher architectural layers" goal rules out. |
| A binary format (MessagePack, protobuf) | No current requirement for compactness or cross-language interop; would add a new dependency for no benefit at Sprint 2's one-record-per-file scale; every existing Ferret store is JSON. |
| Add version-gated read behavior now (reject or migrate on schema mismatch) | Out of scope for "select and implement a format" — this is a corruption/compatibility-handling *policy* decision, not a wire-format decision, and risks pre-empting S2-8's corruption-detection design before it exists. |

## Consequences

### Positive
- The wire format is fully explicit and owned by one file (`FileDependencyStateStore.cs`) — nothing about it depends on `DependencyRecord`'s member names or attribute state.
- A future schema change has a version field to gate on without touching every historical file's shape retroactively.
- Consistent with `ConnectorInstanceStore`'s established JSON conventions — a developer who has seen one production Ferret store recognizes the shape of this one.
- `IDependencyStateStore`, `DependencyRecord`, `RequestEquivalence`, `ResolutionCheck`, and `ResolutionOutcome` are all untouched.

### Negative
- Two small private DTOs plus mapping methods (`ToRecord`/`ToEnvelope`) add code that a direct-serialize-the-domain-type approach would not need.
- No real migration path exists yet if `schemaVersion` ever needs to gate behavior — only the marker is in place.

### Neutral / Risks
- No migration is needed from S2-2's file shape to this one: every S2-2-era store file is ephemeral test-harness output (temp directories recreated per test run), not real persisted user data, so there is nothing to migrate.
- `SpikeDependencyStateStore` is intentionally left serializing `DependencyRecord` directly, unversioned — it remains a frozen Sprint-1 reference implementation (per ADR-0022) and is not expected to converge with the production format.

---

## Cross References

| Document | Relationship |
|---|---|
| [ADR-0022](0022-dependency-state-store-filesystem-backend.md) | Storage-mechanism decision this ADR was explicitly deferred from |
| [ARCH-032](../002-Architecture/ARCH-032-Persistence-Mechanism-Design.md) | §9 leaves serialization format as implementation freedom — this ADR resolves it |
| [V2 Sprint 2 Architecture Review](../superpowers/plans/2026-07-04-v2-sprint-2-architecture-review.md) | Names Epic 1.5 (production serialization format) as S2-3 |
| `src/Ferret.ConnectorPlatform/ConnectorInstanceStore.cs` | Existing JSON-DTO/schema-version convention this ADR reuses |
