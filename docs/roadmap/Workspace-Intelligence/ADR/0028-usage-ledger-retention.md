# ADR-0028 — Usage Ledger Storage Backend and Retention

| Field | Value |
|---|---|
| **Status** | Proposed — requires Founder decision on retention window |
| **Date** | 2026-07-05 |
| **Deciders** | Founder |
| **Milestone** | Workspace Intelligence Platform, Phase 4 |
| **Supersedes** | — |

---

## Context

The Usage Ledger (`../10-Usage-Ledger.md`) is an append-only, immutable event store backing analytics and (eventually) billing. Two things need to be fixed before Phase 4: the storage backend, and how long raw events are kept before being reduced to rollups only.

## Decision

**Backend:** default file-based append-only log behind a new `IUsageLedger` interface, mirroring `IKnowledgeStore`'s pluggable-backend pattern (ARCH-001 §19.3) — not decided here as final for all deployments, since the same abstraction supports swapping to SQLite or a hosted ledger later without changing callers.

**Retention (recommendation pending sign-off):** raw events retained 90 days rolling; daily/weekly rollups retained indefinitely. Rollups are derived and never mutate raw events, so a future retention-window change doesn't lose historical rollups already computed.

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
