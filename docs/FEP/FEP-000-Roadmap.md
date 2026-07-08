# FEP-000 — Program Roadmap

| Field | Value |
|---|---|
| **Document ID** | FEP-000 |
| **Version** | 1.0 |
| **Status** | Active |
| **Last Updated** | 2026-07-08 |

---

## Purpose

This document tracks the sequence of Ferret Engineering Program prompts, their intent, their outputs, and their status. It is updated after every prompt completes. It does not contain product content itself — see [FEP-001-Product-Architecture.md](FEP-001-Product-Architecture.md) and later documents for that.

---

## Program Sequence

| Prompt | Title | Output | Status |
|---|---|---|---|
| Prompt 1 | Product Architecture & Capability Definition | [FEP-001-Product-Architecture.md](FEP-001-Product-Architecture.md) | Complete |
| Prompt 2 | Capability Catalog | [FEP-002-Capability-Catalog.md](FEP-002-Capability-Catalog.md) + 11 detail docs under [capabilities/](capabilities/) | Complete |
| Prompt 3 | Engineering Program Definition | [FEP-003-Engineering-Program.md](FEP-003-Engineering-Program.md) + 11 detail docs under [epics/](epics/) | Complete |
| Prompt 4 | Engineering Specification Generation | [FEP-004-Engineering-Specifications.md](FEP-004-Engineering-Specifications.md) + 61 detail docs under [specifications/](specifications/) | Complete |
| Prompt 5 | *Not yet issued* | — | Not started |

Future prompts will extend this table. Each prompt's scope is defined at the time it is issued, not pre-committed here — this roadmap records what has happened and what is currently in flight, not a speculative full plan.

---

## Program Constraints (standing, across all prompts)

These constraints were established for Prompt 1 and carry forward to every subsequent FEP prompt unless a prompt explicitly revises them:

- No implementation, code, APIs, class designs, database designs, storage designs, runtime designs, AI provider selection, protocol designs, deployment designs, or programming-language decisions until AEF reaches GA and a separate decision activates implementation.
- FEP does not modify, reconcile, or migrate `docs/000-Overview/`, `docs/001-Product/`, `docs/002-Architecture/`, `docs/adr/`, or `docs/Reviews/`.
- Each prompt's output is additive to this folder structure — see [README.md](README.md).

---

## Revision History

| Version | Date | Summary |
|---|---|---|
| 1.0 | 2026-07-08 | Initial roadmap; Prompt 1 recorded as complete |
| 1.1 | 2026-07-08 | Prompt 2 (Capability Catalog) recorded as complete |
| 1.2 | 2026-07-08 | Prompt 3 (Engineering Program Definition) recorded as complete |
| 1.3 | 2026-07-08 | Prompt 4 (Engineering Specification Generation) recorded as complete |
