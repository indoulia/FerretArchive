# V2-ROADMAP-001 — Ferret V2 Architecture Program Roadmap

| Field | Value |
|---|---|
| **Document ID** | V2-ROADMAP-001 |
| **Version** | 1.0 |
| **Status** | Active |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Governing Review** | [AGR-001](../Reviews/AGR-001.md) (Accepted) |
| **Foundation** | ARCH-023 through ARCH-030 — **Frozen** |
| **Mechanism Layer** | ARCH-032 through ARCH-036 — Draft, pending Standard Architecture Review; see [ADR-0021](../adr/0021-v2-architecture-baseline-complete.md) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |

---

## Purpose

ARCH-023 through ARCH-027 are frozen. This document does not add to, revise, or reinterpret them — it plans the work that builds on top of them. It treats the frozen series and AGR-001 as fixed references, sequences everything AGR-001 left open or unaddressed by architectural dependency rather than convenience, and states what governance each future step requires before it may itself be considered settled.

This document makes no architectural decision. It reopens no Closed Architectural Decision (AGR-001 §6). Where it appears to describe a future document's content, that description is a scope statement for planning purposes only — the document itself, when written, is where the actual decision is made and reviewed.

---

## Scope

Covers:
- The current status of the frozen foundation and what it settles
- The principle governing how remaining work is prioritized
- Four tiers of remaining work, ordered by architectural dependency
- Entry and exit criteria for each item
- Which items require a new governance review before being considered frozen, and which do not

Does not cover:
- Any redesign of ARCH-023 through ARCH-027
- Any resolution of a deferred architectural question (that happens in the documents this roadmap schedules, not here)
- Any mechanism, schema, API, or algorithm
- Any commitment to dates, sprints, or resourcing — this is an architectural sequence, not a delivery schedule

---

## 1. Foundation Status

ARCH-023 (V2 Architectural Boundary), ARCH-024 (Artifact Inventory), ARCH-025 (Artifact Validity Model), ARCH-026 (Persistence Requirements), and ARCH-027 (Dependency Resolution Architecture) are frozen, per AGR-001. AGR-001 §6 closes nine architectural decisions that this roadmap treats as immutable inputs — they are referenced by number below, never restated as if open. AGR-001 §5 leaves four architectural questions deferred, not closed — those four are exactly what Tier 1 below exists to schedule.

No item in this roadmap may contradict a Closed Architectural Decision. Any future document that finds it cannot proceed without doing so must halt and escalate to a new governance review rather than proceeding — this is the same rule AGR-001 §8 already established, restated here only because it is the rule this entire roadmap is sequenced around.

---

## 2. Prioritization Principle

Remaining work is ordered by **architectural dependency** — what a piece of work requires to be conceptually sound — never by **implementation convenience** — what would be easiest or fastest to build next.

Concretely, this means: a mechanism-level design is never scheduled ahead of the architectural question it would otherwise have to silently assume an answer to. For example, designing a persistence mechanism before Invalidation Propagation Timing (§3, RM-02) is resolved would force that design to guess at a semantic the frozen series deliberately left open — even though, in isolation, building the mechanism might be the more tractable near-term task. This roadmap does not permit that ordering.

---

## 3. Tier 1 — Deferred Architectural Questions

These are AGR-001 §5's four items, unchanged. Resolving one means amending a frozen foundation document, which requires a new governance review before the amendment is itself frozen (AGR-001 §8).

| ID | Question | Depends On | Blocks | Entry Criteria | Exit Criteria |
|---|---|---|---|---|---|
| RM-01 | Request Identity & Equivalence | AGR-001 Accepted | RM-05, RM-08 | ARCH-027 frozen at v1.0; no dependent Tier 2/3 work has proceeded past a provisional stage on an assumed answer | A revision to the appropriate frozen document states what makes two requests "the same" for resolution purposes, in the same non-mechanism register as the frozen series (no key, index, or algorithm named); revision passes a new governance review confirming no AGR-001 §6 decision is contradicted |
| RM-02 | Invalidation Propagation Timing | AGR-001 Accepted | RM-06, RM-07 | ARCH-025 frozen at v1.0 | A revision to ARCH-025 §5 states whether propagation is eager, lazy, or both are architecturally permitted, without prescribing a mechanism; passes a new governance review |
| RM-03 | Deletion Semantics | AGR-001 Accepted | RM-07 (partial) | ARCH-025 frozen at v1.0 | A revision to ARCH-025 §4 names deletion as a distinct invalidation source, or explicitly declines to with stated rationale; passes a new governance review |
| RM-04 | Validity-Class / Dependency-Shape Matrix | AGR-001 Accepted | RM-07 | ARCH-025 frozen at v1.0 | A revision to ARCH-025 §2–3 (or an appendix) provides the mapping, remains descriptive only, introduces no new component; passes a new governance review |

**Priority order within Tier 1:** RM-01 first — it is the most consequential gap AGR-001 identified and blocks the most downstream work. RM-02 second — it blocks both an architecture refinement (RM-06) and a mechanism design (RM-07). RM-03 and RM-04 can proceed in either order, or be batched with RM-02 into a single ARCH-025 amendment reviewed once rather than three times — a governance-overhead reduction this roadmap recommends but does not mandate.

---

## 4. Tier 2 — Architecture Refinements

New system-boundary-level documents, held to the same rigor as ARCH-023 through ARCH-027 — each is a **new addition to the foundation**, not a mechanism design, and each requires its own new governance review before it may be considered frozen.

| ID | Document | Fills | Depends On | Entry Criteria | Exit Criteria |
|---|---|---|---|---|---|
| RM-05 | AI Integration Architecture | The "AI Integration" item in ARCH-023's Expected V2 Architecture Series | RM-01, RM-02 resolved and refrozen (or explicitly named as open assumptions the document states it does not resolve) | ARCH-023 §9, ARCH-025, ARCH-026, ARCH-027 available as frozen references; RM-01/RM-02 status known | Defines the concrete contract between V2's resolution boundary and the engines that own `IModelProvider` invocation, per ARCH-023 §9; contradicts no AGR-001 §6 decision; defines no mechanism; passes a new governance review before being treated as frozen |
| RM-06 | Benchmarking Architecture | The "Benchmarking" item in ARCH-023's Expected V2 Architecture Series | RM-05 frozen; RM-02 resolved | RM-05 available as a frozen reference | Defines, architecturally, what evidence would demonstrate the Core V2 Principle (ARCH-023 §5) is upheld — without naming tools, thresholds, or a benchmarking mechanism; passes a new governance review |

---

## 5. Tier 3 — Mechanism-Level Design

The "how" every frozen document explicitly excluded: cache/storage design, retrieval algorithms, keys, APIs. These implement within boundaries the foundation already fixed; they do not extend the foundation itself.

| ID | Document | Depends On | Entry Criteria | Exit Criteria | Governance |
|---|---|---|---|---|---|
| RM-07 | Persistence Mechanism Design | ARCH-026 (frozen); RM-02, RM-03, RM-04 resolved | Tier 1 items RM-02–RM-04 frozen | Specifies how dependency state is actually stored, consistent with every constraint ARCH-026 already fixed (repository-local, owned by the existing component, additive not parallel) | Standard Architecture Review (`AR-`); escalate to a new governance review only if a conceptual gap is discovered |
| RM-08 | Resolution Mechanism Design | ARCH-027 (frozen); RM-01 resolved; RM-07 complete | RM-01 frozen; RM-07 complete | Specifies how a candidate is actually checked (retrieval approach, keys), consistent with ARCH-027's guarantees (§5) | Standard Architecture Review; escalate on conceptual gap |
| RM-09 | V2 Surface Design (CLI/MCP) | RM-07, RM-08 complete | RM-07, RM-08 complete | Specifies any CLI/MCP-facing surface for V2 capabilities, per existing `docs/006-CLI/`/`docs/007-SDK/` conventions | Standard Architecture Review or SDK-level review, per existing convention |

---

## 6. Tier 4 — Implementation Planning

Sprint- and code-level planning, following the repository's existing process rather than a new one.

| ID | Item | Depends On | Entry Criteria | Exit Criteria | Governance |
|---|---|---|---|---|---|
| RM-10 | V2 Sprint Specification & Plan | RM-07, RM-08, RM-09 complete | Tier 3 complete | A sprint specification and plan exist under `docs/archive/superpowers/specs`/`docs/archive/superpowers/plans`, following the same process that already produces Ferret's real specification and plan documents (ARCH-024 Critical Finding 3) | Normal PR / code review; no architecture governance review required unless implementation surfaces a conceptual gap |

---

## 7. Governance Requirements Summary

| Tier | Nature of work | Governance required |
|---|---|---|
| 1 — Deferred Architectural Questions | Amendment to a frozen foundation document | New governance review (AGR-series) before the amendment is refrozen — mandatory, per AGR-001 §8 |
| 2 — Architecture Refinements | New system-boundary-level document, extends the foundation | New governance review before the document is treated as frozen — mandatory, same standard AGR-001 applied |
| 3 — Mechanism-Level Design | Implements within already-frozen boundaries | Standard Architecture Review (`AR-`); escalates to a new governance review only if it cannot proceed without contradicting a Closed Decision |
| 4 — Implementation Planning | Sprint/code-level execution | Normal PR/code review; no architecture-level review required |

The general rule this table encodes: **anything that could plausibly touch a Closed Architectural Decision gets a governance review; anything that only builds within decisions already closed does not.**

---

## 8. Sequencing Summary

Priority order, by architectural dependency, ignoring tier boundaries where a lower-tier item is unblocked earlier:

1. RM-01 (Request Identity & Equivalence)
2. RM-02 (Invalidation Propagation Timing)
3. RM-03, RM-04 (Deletion Semantics; Validity-Class/Dependency-Shape Matrix — order interchangeable; may be batched with RM-02)
4. RM-05 (AI Integration Architecture)
5. RM-06 (Benchmarking Architecture)
6. RM-07 (Persistence Mechanism Design)
7. RM-08 (Resolution Mechanism Design)
8. RM-09 (V2 Surface Design)
9. RM-10 (V2 Sprint Specification & Plan)

This order reflects dependency, not effort — RM-01 is scheduled first because the most work downstream depends on it, not because it is the easiest item to resolve.

---

## 9. Baseline Transition (Post ADR-0021)

Tier 3 (RM-07, RM-08, RM-09) is complete — realized as ARCH-032, ARCH-033, and ARCH-034 respectively, with ARCH-035 and ARCH-036 as unscheduled additions composing and validating the three. Per [ADR-0021](../adr/0021-v2-architecture-baseline-complete.md), the program now transitions from architecture-primary to implementation-primary work:

- **RM-05 and RM-06 are deferred, not abandoned.** RM-05 (AI Integration Architecture) is not currently blocking, since no planned implementation work invokes `IModelProvider`. RM-06 (Benchmarking Architecture) is superseded in practice by extending the already-existing, already-approved `docs/archive/superpowers/specs/2026-06-30-benchmark-suite-spec.md` and Sprint 4 corpus generator with V2-specific metrics, rather than writing a new ARCH document first.
- **RM-10 (Implementation Planning) is now the active tier.** Further ARCH-series documents are warranted only when implementation or benchmarking surfaces concrete evidence of a conceptual gap — exactly the standard this roadmap's §7 already set for Tier 3, now extended to the program as a whole.
- **One gap surfaced by the Tier 3 mechanism-package review has no owner yet**: concurrency and multi-process consistency for persisted dependency state is addressed nowhere in ARCH-023 through ARCH-036. ADR-0021 requires this to be resolved by explicit statement before Sprint 1, not left implicit.

---

## Impact on Existing Architecture

**Existing components reused.** This roadmap reuses ARCH-023 through ARCH-027 and AGR-001 in full, as fixed references. It introduces no new artifact, validity concept, persistence requirement, or resolution concept beyond what those documents already state.

**Existing components extended.** None. This is a planning document; nothing it schedules has been designed yet.

**Existing components intentionally unchanged.** All nine Closed Architectural Decisions (AGR-001 §6) and the frozen text of ARCH-023 through ARCH-027 are unmodified by this document.

**New concepts introduced.** One, purely organisational: the four-tier work classification (Deferred Architectural Questions → Architecture Refinements → Mechanism-Level Design → Implementation Planning) and the RM-01 through RM-10 item identifiers used to track them. Neither introduces a new component, interface, or architectural decision — they exist only to sequence work already implied by ARCH-023's Expected V2 Architecture Series and AGR-001's deferred items.

---

## Cross References

| Document | Relationship |
|---|---|
| [AGR-001](../Reviews/AGR-001.md) | Governing review — source of the four Tier 1 items (§5) and the nine Closed Decisions this roadmap treats as immutable (§6) |
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Frozen foundation; source of the Expected V2 Architecture Series items this roadmap schedules as RM-05 and RM-06 |
| [ARCH-024](ARCH-024-Artifact-Inventory.md) | Frozen foundation; source of the existing specification/plan process cited in RM-10's exit criteria |
| [ARCH-025](ARCH-025-Artifact-Validity-Model.md) | Frozen foundation; the document RM-02, RM-03, RM-04 amend |
| [ARCH-026](ARCH-026-Persistence-Requirements.md) | Frozen foundation; the document RM-07 implements against |
| [ARCH-027](ARCH-027-Dependency-Resolution-Architecture.md) | Frozen foundation; the document RM-01 amends and RM-08 implements against |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial V2 architecture program roadmap, built on the frozen foundation (ARCH-023–ARCH-027) and AGR-001. |
| 1.1 | 2026-07-03 | Ferret Core Team | Added §9 (Baseline Transition) recording ADR-0021's milestone declaration; updated Foundation field to ARCH-023–ARCH-030 and added a Mechanism Layer field. No change to Tier 1–4 tables or sequencing. |
