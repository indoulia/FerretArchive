# Workspace Intelligence Platform — Roadmap Index

Milestone: Ferret v2.0, first milestone after Dogfooding. Full context: `00-Vision.md`.

## Reading Order

| # | Doc | Answers |
|---|---|---|
| 00 | Vision | What / Why |
| 01 | Architecture | How (system-level) |
| 02 | Workspace Model | How (data model) |
| 03 | Cross-Workspace References | How (the "no duplication" requirement) |
| 04 | Knowledge Graph | How (schema additions) |
| 05 | Context Optimization | How (the differentiator) |
| 06 | Incremental Indexing | How (stays fast as things change) |
| 07 | Caching | How (stays fast under load) |
| 08 | Telemetry | How (observability) |
| 09 | Analytics | How (what's measured) |
| 10 | Usage Ledger | How (event storage) |
| 11 | Dashboard | How (what's shown) |
| 12 | API | How (surface area) |
| 13 | Storage | How / decision gate |
| 14 | Migration | How existing users are unaffected |
| 15 | Execution Plan | In what order |

`ADR/` — the four decisions that gate implementation. `Backlog/` — the ordered ticket list. `../Future/Deferred-Scope.md` — what's explicitly cut from v1 and why.

## Every Open Decision, In One Place

| Decision | Where | Status |
|---|---|---|
| Workspace registry model (identity-based local registry, recommended) | ADR-0026 | **Requires Founder decision** |
| v1 sharing/permission scope (4 roles, recommended) | ADR-0029 | **Requires Founder decision** |
| Usage ledger raw-event retention (90 days, recommended) | ADR-0028 | **Requires Founder decision** |
| Reference resolution strategy (live federation, not copy) | ADR-0027 | Accepted — non-negotiable requirement, not a founder choice point |
| Everything else in 00–15 | — | Ready for implementation once the three decisions above close |

**Nothing in Phase 1 is blocked on ADR-0028 or ADR-0029 — only ADR-0026.** See `15-Execution-Plan.md` §5.

## What's Deliberately Not Here

Full enterprise RBAC, org-wide analytics/billing, Ferret Hub/cloud sync, and 100K-repository scale work are cut from this milestone. See `../Future/Deferred-Scope.md` for what and why — each entry names the specific FUTURE-002 open question or V3 deferral it's consistent with, so cutting it now isn't a new decision, it's honoring one already made.
