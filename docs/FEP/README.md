# FEP — Ferret Engineering Program

| Field | Value |
|---|---|
| **Document ID** | FEP-INDEX |
| **Version** | 1.0 |
| **Status** | Active |
| **Last Updated** | 2026-07-08 |

---

## What FEP Is

The Ferret Engineering Program (FEP) is a parallel, versioned planning track for Ferret, run independently of the AEF (Agentic Engineering Framework) implementation effort. AEF is currently under active development; Ferret implementation is intentionally deferred until AEF reaches General Availability.

FEP exists to produce the frozen, executable planning package that AEF will consume as its authoritative input once that GA milestone is reached. It is a program of sequential, numbered prompts, each producing one or more durable artefacts under this folder.

## What FEP Is Not

FEP does **not** replace, amend, reconcile, or migrate any of Ferret's existing product documentation — `docs/000-Overview/`, `docs/001-Product/`, `docs/002-Architecture/`, `docs/adr/`, `docs/Reviews/`, or any other historical record. Those documents remain intact as the current product's history and continue to describe Ferret **as it exists today**. FEP describes Ferret **as it is being planned for its next chapter**. The two tracks are not reconciled against each other by this program; that reconciliation, if it happens, is a future, explicit governance decision — not a side effect of FEP's existence.

Nothing in FEP authorizes engineering work on the `src/` tree. FEP is a planning-only program until AEF reaches GA and a separate, explicit decision activates implementation.

## Structure

```
docs/FEP/
├── README.md                          ← this file — program index
├── FEP-000-Roadmap.md                 ← program roadmap: prompts, sequence, status
├── FEP-001-Product-Architecture.md    ← Prompt 1 output: product architecture & capability model
├── capabilities/                      ← per-capability detail specs (populated by future prompts)
├── epics/                             ← epic-level groupings of capability work (populated by future prompts)
├── specifications/                    ← implementation-independent Engineering Specifications, one per Feature (populated by Prompt 4 — see FEP-004)
├── reviews/                           ← governance reviews of FEP artefacts (FEP's equivalent of docs/Reviews/ AGR records)
└── decisions/                         ← FEP-scoped decision records (FEP's equivalent of docs/adr/)
```

## Numbering Convention

Each FEP prompt produces one primary `FEP-NNN-Title.md` document at the top level of this folder. Detail artefacts that a prompt spawns (a capability spec, an epic, a specification, a review, a decision) live in the corresponding subfolder, named to reference the FEP document that authorized them.

## Program Status

See [FEP-000-Roadmap.md](FEP-000-Roadmap.md) for the current sequence of prompts and their status.
