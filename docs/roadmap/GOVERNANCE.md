# Roadmap Governance — Promotion Criteria

The [README](README.md#promotion-lifecycle) states the pipeline:

```
Vision → Research → Proposal → Roadmap → Product Approval → FEP vNext
→ Engineering Program → Engineering Specifications → AEF Execution → Release
```

This document makes each arrow explicit: what must be true to cross it, what evidence proves it, and who decides. An item that can't satisfy a stage's exit criteria stays at that stage — it does not advance on enthusiasm or urgency alone.

---

## 1. Vision → Research

**What qualifies:** Any idea plausibly consistent with the Context OS mission ([FEP-001 §1](../FEP/FEP-001-Product-Architecture.md)). No gate — this is the cheapest stage to enter.

**Exit criteria to become a Research item:** The idea is written down in [`RESEARCH/`](RESEARCH/README.md) as an open question, not an answer — "is X feasible," not "we should build X."

**Owner:** Anyone. No approval required to add a research item.

---

## 2. Research → Proposal

**Entry:** Item exists in [`RESEARCH/`](RESEARCH/README.md).

**Exit criteria (what qualifies a Research item to become a Proposal):**
- The open question has a candidate answer, or a bounded set of options — not full resolution, but enough that a shape can be stated.
- A concrete shape can be written in a paragraph or two: what the capability or feature would concretely be, with no design detail (no APIs, no schemas, no architecture).
- The shape passes the **Boundary Test** ([FEP-001 §5.3](../FEP/FEP-001-Product-Architecture.md)): removing it would leave a consumer with less complete, current, trustworthy, or accessible *context* — not less conclusion, action, or process enforcement. If it fails this test, it is out of scope permanently, not merely stalled at Research.

**Owner:** Roadmap maintainer (whoever curates `docs/ROADMAP/`). Low bar, no Founder sign-off needed — this is an editorial judgment that the idea is now concrete enough to track as a candidate.

---

## 3. Proposal → Roadmap

**Entry:** Item exists in [`PROPOSALS/`](PROPOSALS/README.md).

**Exit criteria (what evidence a Proposal needs to enter the Roadmap):**
- Business value and user value are both articulated, in the same shape as [FERRET-PRODUCT-ROADMAP.md §3](FERRET-PRODUCT-ROADMAP.md#3-future-capability-evolution)'s existing entries (Purpose / Business Value / User Value / Relationship / Why it belongs).
- Its relationship to an existing FEP capability is identified by name (which of the eleven [FEP-002](../FEP/FEP-002-Capability-Catalog.md) capabilities it extends or matures). If it requires a capability FEP-001 doesn't already define, that is flagged explicitly as a bigger decision — it cannot enter quietly as an extension of something it isn't.
- A tentative generation assignment (v3, v4, Long-Term) with a one-line sequencing rationale — why not sooner, why not later.

**Owner:** Roadmap maintainer. Still no Founder sign-off — placing an item in `NEXT/`/`FUTURE/`/`THEMES/` records it as roadmapped, not as approved. This distinction (roadmapped vs. approved) is the entire point of the next stage.

---

## 4. Roadmap → Product Approval

**What constitutes Product Approval:** An explicit, dated decision, scoped to a named item (not "the whole roadmap"), recorded as a decision-log entry — the same pattern already used for the v2.0 milestone (e.g. `00-Vision.md §6`: *"Multi-repo workspace is the v2.0 milestone... Ready for implementation — confirmed by Founder directive"*).

A Product Approval record must state:
- Which specific roadmap item(s) are approved.
- What generation/scope it's approved for.
- Whether it displaces or runs alongside currently committed engineering capacity (Product Approval does not silently assume capacity exists — see `Immediate-Product-Roadmap.md`'s explicit capacity sequencing as the model to follow).

**Owner:** Founder. This is the one stage in the pipeline with a single named authority, consistent with every existing precedent in this repository (`00-Vision.md`, `Immediate-Product-Roadmap.md`, ADR-0026).

**Where recorded:** A decision-log entry under `docs/013-Governance/DECISION-LOG.md` or a roadmap-local decision log, cross-referenced from the approved item's page.

---

## 5. Product Approval → FEP vNext

**What conditions justify creating FEP vNext:**
- At least one Product Approval record exists for an item that requires a capability definition, epic, or specification change — i.e., something FEP v1 doesn't already cover.
- FEP v1 is frozen and immutable ([FEP README](../FEP/README.md)); nothing is ever added to it. Any approved item that needs new planning content necessarily opens a new FEP version. There is no "minor update to FEP v1" path.
- The new FEP version's scope is exactly the approved item(s) — it does not re-open unrelated frozen v1 content as a side effect of being issued.

**Owner:** Whoever issues FEP prompts (the same authority that issued FEP v1's four prompts). Distinct from the Founder role in §4: Product Approval authorizes *that* something should be planned; issuing FEP vNext is the act of actually starting that planning program.

**Exit artifact:** A new `FEP-000-Roadmap.md`-equivalent entry (or a new program roadmap document) recording FEP vNext's first prompt.

---

## 6. FEP vNext → Engineering Program → Engineering Specifications

This transition is internal to FEP's own sequencing and already governed by [FEP-000-Roadmap.md](../FEP/FEP-000-Roadmap.md)'s prompt structure (Product Architecture → Capability Catalog → Engineering Program → Engineering Specifications). This governance document does not duplicate that sequencing — it only asserts that Roadmap governance stops here. Once FEP vNext is issued, FEP's own program roadmap is authoritative for what happens next.

---

## 7. Engineering Specifications → AEF Execution

**Entry:** Engineering Specifications complete for the vNext scope, following the same pattern as [FEP-004](../FEP/FEP-004-Engineering-Specifications.md).

**Exit criteria:** AEF reaches General Availability — the standing gate stated in the [FEP README](../FEP/README.md) and [FEP-000-Roadmap.md](../FEP/FEP-000-Roadmap.md).

**Unresolved, and not invented here:** What evidence establishes that AEF has reached GA, and who makes that determination, is an open question inherited from [FEP-001 §9 Q8](../FEP/FEP-001-Product-Architecture.md) — not something this governance document can resolve on its own, since it concerns AEF (a separate program), not the Roadmap. Until Q8 is answered, this transition has an owner-shaped gap: **no one is yet named as the authority who declares the gate met.** Treat this as a standing risk, not an oversight — see [FERRET-PRODUCT-ROADMAP.md §10](FERRET-PRODUCT-ROADMAP.md#10-open-questions).

---

## 8. AEF Execution → Release

Governed by the existing [RELEASE-PROCESS.md](../012-Releases/RELEASE-PROCESS.md). Not restated here.

---

## Ownership Summary

| Transition | Owner | Sign-off required? |
|---|---|---|
| Vision → Research | Anyone | No |
| Research → Proposal | Roadmap maintainer | No — editorial judgment |
| Proposal → Roadmap | Roadmap maintainer | No — editorial judgment, placement only |
| Roadmap → Product Approval | Founder | **Yes — explicit, dated, scoped decision** |
| Product Approval → FEP vNext | FEP prompt issuer | Yes — act of issuing the first prompt |
| FEP vNext → Engineering Program → Specifications | FEP prompt issuer | Governed by FEP's own sequencing |
| Specifications → AEF Execution | **Unresolved** (FEP-001 §9 Q8) | Unresolved |
| AEF Execution → Release | Per RELEASE-PROCESS.md | Per RELEASE-PROCESS.md |

Only one stage in this pipeline lacks a named owner. That gap is inherited from FEP-001, not created by this document, and should not be silently filled in without resolving Q8 first.
