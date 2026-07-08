# Ferret Roadmap — Index

This folder is the **living** product-strategy track for Ferret. It is separate from, and subordinate in authority to, the frozen [Ferret Engineering Program (FEP)](../FEP/README.md).

## Governance: Roadmap vs. FEP

| | ROADMAP (here) | FEP (`docs/FEP/`) |
|---|---|---|
| Nature | Living — revised freely | Frozen per version — FEP v1.0 is immutable |
| Content | Future direction, themes, unapproved ideas | Approved capability model, engineering specifications |
| Authority | Product strategy only | Authoritative engineering baseline |
| Contains | Vision evolution, proposals, research | Capability catalog, epics, specifications |

Rules:
- ROADMAP never redefines a capability FEP has already defined ([FEP-002 Capability Catalog](../FEP/FEP-002-Capability-Catalog.md) is authoritative for the 11 capabilities).
- ROADMAP never writes Epics or Engineering Specifications — that only happens inside FEP, once a Roadmap item is promoted.
- ROADMAP never modifies FEP documents.
- FEP never absorbs Roadmap speculation without an explicit promotion decision (see Lifecycle below).

## Promotion Lifecycle

```
Research → Proposal → Roadmap → Product Approval → FEP vNext → Engineering Program
→ Engineering Specifications → AEF Execution → Release
```

Nothing skips a stage. An idea in `RESEARCH/` has no committed shape yet. An idea in `PROPOSALS/` is a candidate, not a commitment. An idea placed under `NEXT/` or `FUTURE/` is roadmapped but still requires Product Approval before it can seed a future FEP prompt.

**[GOVERNANCE.md](GOVERNANCE.md)** defines the entry/exit criteria and owner for every arrow above — what qualifies a Research item to become a Proposal, what evidence a Proposal needs to enter the Roadmap, what constitutes Product Approval, and what conditions justify opening a FEP vNext.

## Structure

| Path | Contents |
|---|---|
| [FERRET-PRODUCT-ROADMAP.md](FERRET-PRODUCT-ROADMAP.md) | The complete roadmap document — all ten required sections in one place |
| [CURRENT/CURRENT.md](CURRENT/CURRENT.md) | Snapshot of the frozen baseline + what's actually shipped today |
| [NEXT/V2.md](NEXT/V2.md) | Ferret v2 — Workspace Intelligence Platform, already approved and in execution |
| [FUTURE/V3.md](FUTURE/V3.md), [V4.md](FUTURE/V4.md), [LONG-TERM.md](FUTURE/LONG-TERM.md) | Later product generations, not yet approved |
| [THEMES/](THEMES/) | Future capabilities grouped by strategic theme |
| [PROPOSALS/](PROPOSALS/) | Unapproved product ideas |
| [RESEARCH/](RESEARCH/) | Open questions requiring investigation before they can become a Proposal |
| `Workspace-Intelligence/`, `Immediate-Product-Roadmap.md`, `Future/Deferred-Scope.md` | Pre-existing execution-level material for the v2.0 milestone — authoritative for that milestone's engineering detail; not duplicated here |

## Note on a Standing Open Question

This roadmap deliberately does **not** incorporate `docs/002-Architecture/FUTURE-002-Enterprise-Intelligence-Vision.md`'s "Option B" (embedded AI reasoning, agent runtime, model hosting) as future scope. That vision conflicts with FEP-001's frozen Non-Goals (§1.3, §5.2 — Ferret does not reason, generate, or host AI model inference). Reconciling the two narratives is FEP-001 Open Question 2, not something this roadmap can resolve unilaterally. See [FERRET-PRODUCT-ROADMAP.md §9](FERRET-PRODUCT-ROADMAP.md#9-product-risks) and [§10](FERRET-PRODUCT-ROADMAP.md#10-open-questions).
