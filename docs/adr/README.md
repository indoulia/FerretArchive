# Architecture Decision Records (ADRs)

ADRs capture significant architectural decisions, their context, and their rationale.

---

## Index

| ID | Title | Status | Date |
|---|---|---|---|
| [0001](0001-use-architecture-decision-records.md) | Use Architecture Decision Records | Accepted | 2026-06-27 |
| [0005](0005-product-rebranding.md) | Product Rebranding: AISpace to Ferret | Accepted | 2026-06-27 |
| [0011](0011-rename-Ferret-sdk-to-Ferret-plugin-sdk.md) | Rename Ferret.Sdk to Ferret.Plugin.SDK | Accepted | 2026-06-27 |
| [0012](0012-milestone-1-platform-foundation-freeze.md) | Milestone 1: Platform Foundation Freeze | Accepted | 2026-06-28 |
| [0021](0021-v2-architecture-baseline-complete.md) | Milestone: Ferret V2 Architecture Baseline v1 Complete | Accepted | 2026-07-03 |
| [0022](0022-dependency-state-store-filesystem-backend.md) | Dependency-State Store: Local Filesystem with Atomic Writes | Accepted | 2026-07-04 |
| [0023](0023-dependency-record-serialization-format.md) | Dependency-Record Serialization Format | Accepted | 2026-07-04 |
| [0024](0024-dependency-state-store-key-lookup-structure.md) | Dependency-State Store: Key/Lookup Structure | Accepted | 2026-07-04 |

---

## When to Write an ADR

Write an ADR when a decision:
- affects more than one component or team boundary
- involves a technology or pattern that will be hard to reverse
- has notable trade-offs worth documenting
- was reached after meaningful debate

Trivial implementation choices do not need an ADR.

---

## Lifecycle

| Status | Meaning |
|---|---|
| **Proposed** | Under discussion — not yet accepted |
| **Accepted** | Approved and in effect |
| **Deprecated** | Superseded or no longer relevant |
| **Superseded** | Replaced by a newer ADR |

---

## Creating a New ADR

1. Copy `0000-template.md` and name it `NNNN-short-title.md` (next sequential number).
2. Fill in all sections.
3. Open a PR — discussion happens there.
4. Merge = status becomes **Accepted**.
