# ADR-0028 — Usage Ledger Storage Backend and Retention

| Field | Value |
|---|---|
| **Status** | Accepted (default) — retention window ships as a configurable default, not a blocking Founder decision |
| **Date** | 2026-07-05 |
| **Deciders** | Founder (implementation-readiness review, 2026-07-05: downgraded from blocking) |
| **Milestone** | Workspace Intelligence Platform, Phase 4 |
| **Supersedes** | — |

---

## Context

The Usage Ledger (`../10-Usage-Ledger.md`) is an append-only, immutable event store backing analytics and (eventually) billing. Two things need to be fixed before Phase 4: the storage backend, and how long raw events are kept before being reduced to rollups only.

**Why this isn't a Founder gate:** unlike ADR-0026 (registry model — a data-model choice that's expensive to reverse once workspaces exist) and ADR-0029 (sharing scope — determines what Phase 5 builds), the retention window is a single config parameter on a background cleanup job. Shipping the recommended default and changing it later requires zero rework — no schema change, no migration. Implementation should proceed on the default below; Phase 4 does not block on Founder sign-off.

## Decision

**Backend:** default file-based append-only log behind a new `IUsageLedger` interface, mirroring `IKnowledgeStore`'s pluggable-backend pattern (ARCH-001 §19.3) — not decided here as final for all deployments, since the same abstraction supports swapping to SQLite or a hosted ledger later without changing callers.

**Retention (shipping default, override anytime via config):** raw events retained 90 days rolling; daily/weekly rollups retained indefinitely. Rollups are derived and never mutate raw events, so a future retention-window change doesn't lose historical rollups already computed.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Keep raw events indefinitely | Unbounded storage growth with no analytics benefit — every dashboard in `../11-Dashboard.md` operates on windows well inside 90 days |
| Aggregate-only, discard raw events immediately | Loses replay capability (`../10-Usage-Ledger.md` §4) — if a new rollup definition is needed later, there's nothing to recompute it from |

## Consequences

### Positive
- Bounded raw-event storage growth
- Rollups remain available indefinitely for trend dashboards even after raw events age out

### Negative
- Any analytics need discovered after 90 days that requires raw-event detail (not already captured in a rollup) cannot be backfilled for events older than the window

### Neutral / Risks
- Long-term audit/compliance retention (beyond analytics) is a separate, V3-adjacent RBAC concern per FUTURE-002 §22 and is explicitly not what this ADR's 90-day window is for
