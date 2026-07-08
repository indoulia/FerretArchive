# FEP-003A — Engineering Program Review & Freeze

| Field | Value |
|---|---|
| **Document ID** | FEP-003A |
| **Version** | 1.0 |
| **Status** | Complete — quality gate between FEP-003 and FEP-004 |
| **Program** | Ferret Engineering Program (FEP) |
| **Review Scope** | [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md), [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md), [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Reviewer Posture** | Independent Principal Product Architect review, not author self-review |
| **Last Updated** | 2026-07-08 |

---

## Executive Summary

FEP-001, FEP-002, and FEP-003 form an internally coherent, unusually disciplined planning package. The capability model is acyclic, boundaries are mutually cross-checked (each capability's Non-Responsibilities mirror its neighbor's Responsibilities), and the Engineering Program's phasing correctly derives from the dependency graph rather than from arbitrary sequencing. The self-auditing habit each document exhibits (explicit "Review" sections, Risk sections that flag the program's own weak points) is itself evidence of a rigorous process, and several apparent risks are pre-empted structurally rather than by policy statement (notably, Provenance's interleaved rather than phased build).

That said, the review surfaces one verified boundary regression (Extensibility silently dropped Organization's extension point between FEP-001 and FEP-002), one systemic pattern of self-acknowledged unverifiable completion criteria spanning six of eleven capabilities, and a handful of minor documentation gaps. None of these require redesigning the product or capability model. They are closable with targeted corrections, not a rework cycle.

**Freeze Decision: APPROVED WITH MINOR RECOMMENDATIONS** — contingent on one Required Correction (below) before FEP-004 begins generating specifications for the affected capability.

---

## Strengths

1. **Acyclic, minimal dependency graph.** Workspace Definition → Acquisition → Organization → {Maintenance, Assembly} → Delivery, with Provenance and Access Control as cross-cutting obligations rather than pipeline stages, Observability as a leaf node with no downstream dependents, and Federation as the sole capability depending on the whole model. Every dependency in FEP-001 §4, FEP-003 Global Output 2, and the Epic Dependency Graph (Global Output 3) was traced against the individual capability/epic documents — no circular dependency exists, and no phase depends on a "later" one.

2. **Boundaries are checked from both sides.** For every capability pair sampled (Acquisition/Organization, Organization/Assembly, Assembly/Delivery, Maintenance/Assembly), the Non-Responsibility of one is the literal mirror of the other's Responsibility, not just a compatible restatement. This is real boundary discipline, not aspirational language.

3. **Provenance's interleaving is a structural fix, not a policy statement.** Rather than just saying "don't forget provenance," the Engineering Roadmap forces E07.1 to be built concurrently with E02.2/E03.2/E05.3 and calls this out explicitly as the only way to avoid the named "provenance as afterthought" failure mode. This is the strongest piece of engineering judgment in the program.

4. **Observability is correctly non-load-bearing.** Every capability reports to it; nothing depends on it. This is stated in FEP-001 §4, re-verified in FEP-002-CAP-10's own Non-Responsibilities ("must never be a prerequisite for another capability to function"), and honored in the Roadmap (Phase 5 runs in parallel, gates nothing).

5. **Completion criteria are mostly behavioral and falsifiable.** Many read like already-written acceptance tests ("A simulated failure in one source's reading has no effect on the successful reading of others," "A deliberately introduced lineage gap is detected and reported"). This is exactly the shape FEP-004 needs to consume.

6. **Federation is appropriately thin and honest about its own provisionality.** Rather than over-specifying a capability with no real use case yet, its own Risk section states the epic/feature breakdown "is necessarily more provisional than any other capability's" and should be treated as a placeholder. That is the correct level of investment for something gated behind the entire rest of the model.

---

## Weaknesses

1. **Verified boundary regression: Extensibility lost Organization's extension point.** FEP-001 §2.9's Responsibility explicitly assigns Extensibility three extension surfaces: "new kinds of sources (for Acquisition), new kinds of structure (for Organization), and new kinds of consumers or delivery surfaces (for Delivery)." FEP-002-CAP-09's Responsibilities section only names two — Acquisition and Delivery — and reduces Organization to something merely protected from special-casing, not something with its own extension point for new structure types. This drops straight through to FEP-003: EPIC-CAP-09 has E09.1 (Acquisition Extension Points) and E09.2 (Delivery Extension Points), but no epic for extending Organization's structuring behavior. FEP-002's own "Review" section asserts "none narrows a responsibility FEP-001 already assigned" — this claim is not accurate for Extensibility. This is a genuine, checkable inconsistency between a frozen authoritative document and its own elaboration, not a stylistic quibble.

2. **A systemic pattern of self-acknowledged unverifiable completion criteria.** Six of eleven capability/epic documents (Maintenance, Assembly, Delivery, Provenance, Access Control, Observability) contain a Risk entry conceding that a stated completion criterion cannot actually be objectively verified without a definition the program does not yet supply:
   - **Maintenance** — freshness-expectation underspecification and invalidation completeness both leave their stated completion criteria unverifiable as written.
   - **Assembly** — constraint-interpretation ambiguity means "constraints were correctly honored" is not yet objectively checkable.
   - **Delivery** — subscription-scope ambiguity ("standing interest") risks being unverifiably broad.
   - **Provenance** — granularity disputes (what counts as "a unit of context") require explicit reconciliation before transformation-lineage recording can be considered complete.
   - **Access Control** — precedence-rule disputes mean Policy Scope Granularity's completion criteria cannot be objectively verified.
   - **Observability** — ambiguity in what counts as "expected" versus a failure signal is conceded to be hard to verify objectively.

   FEP-003's own Review section asserts "every Completion Criterion is phrased as an observable product behavior" — true at the level of wording, but the documents' own Risk sections concede the underlying definitions that would make those criteria checkable don't exist yet. This is the most consequential finding for Engineering Readiness because it is not one isolated gap but a repeated pattern across roughly half the program.

3. **Two unreconciled product narratives remain open by design.** FEP-001 explicitly leaves the relationship between FEP and `docs/000-Overview/`, PRD-001, etc. unresolved, and this review was not instructed to reconcile it. Noted as a standing risk, not a defect of this review — but it is the single largest piece of "additional product discovery" a reader could stumble into if this isn't resolved before broader consumption of these documents begins.

---

## Risks (ranked by severity)

1. **[High] Unverifiable completion criteria reaching FEP-004 unmodified.** If specifications are generated per-Feature without first closing the ~6 definitional gaps above, the specs inherit untestable acceptance criteria (e.g., a spec for Policy Scope Granularity cannot specify conflict resolution without a precedence rule that doesn't exist yet). This is the single biggest threat to FEP-004 producing genuinely implementable specs.
2. **[Medium] Extensibility's dropped Organization extension point becomes silently permanent.** If FEP-004 proceeds from FEP-003's Epic list as-is, no Engineering Specification will ever be generated for extending Organization's structuring behavior, because no Epic exists for it. The gap will not become visible again until someone tries to add a genuinely new structuring type and discovers there's no defined extension point — exactly the "special-casing" failure mode Extensibility itself was designed to prevent.
3. **[Medium] Unbounded acquisition surface / no ceiling on "engineering-relevant source."** Already self-identified in FEP-001 §8 and Open Question 6, and inherited unresolved into Acquisition's epic risk register. Not a new inconsistency, but a live risk for whoever executes E02.1 first, since "discover what exists within scope" has no natural stopping point.
4. **[Low] Roadmap phase granularity is inconsistent in exactly one place.** Global Output 1 is expressed at Epic granularity except Phase 2/Phase 4's treatment of E08.1/E08.2, which splits at Feature granularity (F08.1.1/F08.2.1 in Phase 2, F08.1.2/F08.2.2 in Phase 4) without saying so explicitly via Feature IDs. Low risk of misreading, easy to fix.
5. **[Low] Minor Epic Dependency Graph omission.** The Global Output 3 table row for E04.1 (Change Detection) lists dependencies "E02.1, E03.3" but F04.1.2 also depends on F01.2.3 (in E01.2). Cosmetic — E01.2 is already satisfied earlier in the roadmap regardless — but the table's own claim to enumerate cross-capability dependencies is incomplete for this one row.
6. **[Low, accepted] Federation and the historical-document reconciliation are both explicitly deferred, provisional, or open.** Correctly flagged by the documents themselves; not a hidden risk.

---

## Recommendations

**Critical**
None. No finding in this review requires redesigning FEP-001's product architecture or FEP-002's capability model.

**Major**
1. Restore Organization's extension point in FEP-002-CAP-09 and FEP-003-EPIC-CAP-09 before FEP-004 generates specifications for Extensibility, so the program matches what FEP-001 §2.9 actually assigned. This is the one Required Correction (see below).
2. Resolve, or explicitly schedule the resolution of, the ~6 definitional gaps named in Weakness #2, before FEP-004 generates specs for the affected Features. This does not need to happen inside FEP-001/002/003 — it can be a short, targeted addendum or an early FEP-004 sub-step — but it should not be silently discovered mid-specification.

**Minor**
1. Make the Phase 2/Phase 4 Feature-level split of E08.1/E08.2 explicit by Feature ID in the Roadmap, matching the granularity used everywhere else.
2. Correct the E04.1 row in the Epic Dependency Graph (Global Output 3) to also list F01.2.3 as a Feature-level dependency.
3. Clarify in FEP-001 §4's dependency diagram that Access Control & Policy gates both Assembly and Delivery (the diagram text currently only says "gates Delivery," while FEP-002/003 correctly and consistently treat it as gating Assembly's Selection & Ranking as well). This is a wording gap, not a substantive inconsistency, since the underlying capability responsibility text already supports it.

**Optional**
1. Consider whether Open Question 2 (historical-document reconciliation) deserves a lightweight ADR-style resolution before FEP-004's work draws broader attention to these documents, purely to avoid an unreconciled repository narrative persisting once specification work begins. Not required for FEP-004 to proceed, since FEP-004 only consumes FEP-001–003, not the historical docs.

---

## Required Corrections

One correction is required before FEP-004 may generate Engineering Specifications for the Extensibility capability:

> FEP-002-CAP-09 and FEP-003-EPIC-CAP-09 must be amended to restore the "new kinds of structure (for Organization)" extension point that FEP-001 §2.9 assigned to Extensibility but FEP-002 dropped. Concretely: add a corresponding Epic analogous to E09.1/E09.2, with at least one Feature for defining and inventorying Organization's structuring extension point.

No other capability requires a correction before FEP-004 may proceed against it. The six definitional gaps in Weakness #2 are recommended (Major), not required, to resolve before FEP-004 starts — they can instead be resolved as FEP-004 reaches each one, provided that resolution happens deliberately rather than being glossed over.

---

## Freeze Decision

**APPROVED WITH MINOR RECOMMENDATIONS**

FEP-001 and FEP-002 (except for the single Extensibility gap) are approved to freeze. FEP-004 may begin generating Engineering Specifications for ten of the eleven capabilities immediately. Extensibility specifically should not proceed until the Required Correction above is applied — a small, contained fix.

---

## Engineering Readiness

| Dimension | Score | Rationale |
|---|---|---|
| Product Definition (FEP-001) | 90% | Vision, goals, principles, and boundaries are unusually crisp and mutually reinforcing. The only real gap is the unresolved relationship to historical product docs (Open Question 2) and the open-ended completeness goal — both self-identified, neither blocking. |
| Capability Model (FEP-002) | 85% | Ten of eleven capabilities are a faithful, non-narrowing elaboration of FEP-001. Extensibility is not — it measurably narrowed FEP-001's assigned scope, which is the deduction here. |
| Engineering Program (FEP-003) | 80% | Epic/Feature structure, dependency graph, phasing, and Critical Path are logically sound and well-justified. The deduction is the systemic pattern of self-acknowledged unverifiable completion criteria across six capabilities — real planning content, but not yet specification-ready content, for those specific Features. |
| Execution Readiness | 82% | Phase 2 (Minimal Context Supply Chain) is specified precisely enough to start generating specs against today. The definitional gaps concentrate in Phase 3/4 territory (Maintenance, Provenance querying, Access Control granularity) rather than in the Phase 2 critical path — good news for sequencing, but it means "ready" is not uniform across the whole program. |
| **Overall Program** | **84%** | A strong, disciplined planning package with one small, mechanical defect and one identifiable, boundable category of follow-up work — not a program with foundational problems. |

---

## Final Statement

**Is the Ferret Engineering Program sufficiently complete and internally consistent to begin Engineering Specification generation?**

Conditionally yes. FEP-001, FEP-002, and FEP-003 are frozen and ready for FEP-004, with one exception: the Extensibility capability (FEP-002-CAP-09 / FEP-003-EPIC-CAP-09) must first be corrected to restore Organization's extension point before its Engineering Specifications are generated. All other ten capabilities may proceed directly into FEP-004.

Separately, before FEP-004 reaches the six Features flagged in Weakness #2 (spanning Maintenance, Assembly, Delivery, Provenance, Access Control, and Observability), the responsible party should resolve the specific definitional gap each of those Features' own Risk sections name — a precedence rule for overlapping policy scopes (Access Control), a bounded definition of "a unit of context" for lineage granularity (Provenance), a concrete notion of "stated constraint" for Assembly's constraint recognition, a bounded description of "standing interest" for Delivery's subscription scope, a definition of what counts as continuity of "the same" entity across a structural change for Maintenance's invalidation completeness, and an explicit source for what counts as an "expected" versus a failure signal in Observability. None of these require reopening FEP-001 or FEP-002's capability model — they are definitional refinements at the Feature level, addressable as part of the relevant Feature's own Engineering Specification.

---

## Cross References

| Document | Relationship |
|---|---|
| [FEP-001-Product-Architecture.md](../FEP-001-Product-Architecture.md) | Reviewed document — Product Vision, Goals, Principles, Capability Model |
| [FEP-002-Capability-Catalog.md](../FEP-002-Capability-Catalog.md) | Reviewed document — per-capability Responsibilities/Non-Responsibilities; Extensibility correction applies here |
| [FEP-003-Engineering-Program.md](../FEP-003-Engineering-Program.md) | Reviewed document — Epics, Features, Global Outputs; Extensibility correction applies to its epic detail doc |
| [FEP-004-Engineering-Specifications.md](../FEP-004-Engineering-Specifications.md) | Downstream deliverable this review gates |
