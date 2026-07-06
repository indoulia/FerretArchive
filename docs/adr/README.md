# Architecture Decision Records (ADRs)

ADRs capture significant architectural decisions, their context, and their rationale.

---

## Index

| ID | Title | Status | Date |
|---|---|---|---|
| [0001](0001-use-architecture-decision-records.md) | Use Architecture Decision Records | Accepted | 2026-06-27 |
| [0005](0005-product-rebranding.md) | Product Rebranding: AISpace to Ferret | Accepted | 2026-06-27 |
| [0011](0011-rename-aispace-sdk-to-aispace-plugin-sdk.md) | Rename AISpace.SDK to AISpace.Plugin.SDK | Accepted | 2026-06-27 |
| [0012](0012-milestone-1-platform-foundation-freeze.md) | Milestone 1: Platform Foundation Freeze | Accepted | 2026-06-28 |
| [0013](0013-capability-based-platform-architecture.md) | Capability-Based Platform Architecture | Accepted | 2026-06-28 |
| [0014](0014-document-processing-architecture.md) | Document Processing Architecture | Accepted | 2026-06-28 |
| [0015](0015-information-retrieval-architecture.md) | Information Retrieval Architecture | Accepted | 2026-06-28 |
| [0016](0016-integration-platform-architecture.md) | Integration Platform Architecture | Accepted | 2026-06-28 |
| [0017](0017-mcp-runtime-architecture.md) | MCP Runtime Architecture | Accepted | 2026-06-28 |
| [0018](0018-application-layer-reserved.md) | Application Layer Reserved (Ferret.Application) | Reserved | 2026-06-28 |
| [0019](0019-ai-platform-architecture.md) | AI Platform Architecture | Accepted | 2026-06-29 |
| [0020](0020-prompt-platform-architecture.md) | Prompt Platform Architecture | Accepted | 2026-06-29 |
| [0021](0021-v2-architecture-baseline-complete.md) | Milestone: Ferret V2 Architecture Baseline v1 Complete | Accepted | 2026-07-03 |
| [0022](0022-dependency-state-store-filesystem-backend.md) | Dependency-State Store: Local Filesystem with Atomic Writes | Accepted | 2026-07-04 |
| [0023](0023-dependency-record-serialization-format.md) | Dependency-Record Serialization Format | Accepted | 2026-07-04 |
| [0024](0024-dependency-state-store-key-lookup-structure.md) | Dependency-State Store: Key/Lookup Structure | Accepted | 2026-07-04 |
| [0025](0025-uncommitted-work-during-active-governance-gate.md) | Uncommitted Work During an Active Governance Gate | Proposed | 2026-07-04 |

ADR-0026 through 0029 (Workspace Intelligence Platform) are tracked separately in [docs/roadmap/Workspace-Intelligence/ADR/](../roadmap/Workspace-Intelligence/ADR/), indexed in that roadmap's own README.

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
