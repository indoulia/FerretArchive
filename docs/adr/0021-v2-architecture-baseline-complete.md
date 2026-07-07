# ADR-0021 — Milestone: Ferret V2 Architecture Baseline v1 Complete

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-03 |
| **Deciders** | Ferret Core Team |
| **Sprint** | V2 Architecture Program (pre-implementation) |

---

## Context

Between 2026-07-03's architecture sessions, Ferret V2 progressed through a complete, governed architecture program:

- **Conceptual kernel** (frozen): ARCH-023 through ARCH-030, defining V2's boundary, artifact inventory, validity model, persistence requirements, dependency resolution, request equivalence, validity propagation, and dependency participation semantics.
- **Governance record**: AGR-001 through AGR-004, closing nine architectural decisions and resolving all four deferred questions AGR-001 originally identified.
- **Bridge**: ARCH-031, defining what a mechanism architecture is and the evidentiary bar every mechanism document must clear.
- **Mechanism layer**: ARCH-032 (Persistence), ARCH-033 (Dependency Resolution), ARCH-034 (Surface Integration), ARCH-035 (Mechanism Interaction Model), ARCH-036 (Mechanism Validation and Conformance).

A full package review of the mechanism layer, verified line-by-line against the frozen kernel rather than from memory, converged at zero Critical and zero Significant findings across two correction cycles. No Closed Architectural Decision was reopened at any point in the program.

At this point, continuing to produce new ARCH documents (ARCH-037, ARCH-038, ...) without a concrete implementation or benchmark result driving them would not be discovering missing architecture — it would be re-deriving decisions the mechanism layer has already made freedom for. The risk has shifted from "insufficient architecture" to analysis paralysis: further paper design cannot surface the specific gaps that only real code and real measurement will find (the discovered-but-unresolved concurrency/multi-process gap, §Consequences, is a case in point — no further architecture-only review found it before this point).

Two Tier 2 roadmap items from V2-ROADMAP-001 — RM-05 (AI Integration Architecture) and RM-06 (Benchmarking Architecture) — remain unwritten, out of the sequence V2-ROADMAP-001 §8 originally specified. Repository investigation at the time of this decision found:
- RM-05 is not currently blocking: the planned MVP path (scan → index → persist dependency state → resolve → reuse → CLI output) never invokes `IModelProvider`, consistent with ARCH-024's finding that no AI-derived artifact exists in production today.
- RM-06's practical intent is already substantially met by existing repository assets: `docs/archive/superpowers/specs/2026-06-30-benchmark-suite-spec.md` (Approved for implementation) and the Sprint 4 enterprise corpus generator (`tests/Ferret.Benchmarks`, `docs/archive/superpowers/plans/2026-07-01-sprint-4-enterprise-corpus.md`) already provide a benchmark harness and synthetic corpus tiers (Small/Medium/Enterprise) covering indexing, search, and context-assembly measurement. What remains is extending that harness with V2-specific metrics once mechanism code exists — not building a harness from nothing.

## Decision

We declare the **Ferret V2 Architecture Baseline v1** complete and frozen as of this record.

**Covered by this milestone:**

| Layer | Documents |
|---|---|
| Conceptual kernel (frozen, unchanged) | ARCH-023 through ARCH-030 |
| Governance | AGR-001 through AGR-004 |
| Bridge | ARCH-031 |
| Mechanism layer | ARCH-032, ARCH-033, ARCH-034, ARCH-035, ARCH-036 |

**Transition rules:**

1. **No new ARCH document may be created to "complete the architecture."** A new ARCH-0NN document is warranted only when implementation or benchmarking produces concrete evidence of a contradiction in, or a missing concept from, the documents listed above. Evidence means a specific failing behavior or measurement, not a hypothetical.
2. **ADRs and implementation are now the default mode of work.** Technology, storage, key, serialization, and format decisions proceed through `docs/adr/`, per ADR-0001, exactly as the mechanism layer (ARCH-032 §"Interaction With Future ADRs", ARCH-033 similarly) already anticipated.
3. **RM-05 (AI Integration Architecture) is deferred, not abandoned.** It becomes blocking the moment an AI-derived artifact (Review Engine or Artifact Engine output, or any `ChatResponse`/`EmbeddingResult`-class artifact) enters the reuse path this architecture governs.
4. **RM-06 (Benchmarking Architecture) is deferred as a formal ARCH document, superseded in practice by extending the existing benchmark suite** (`docs/archive/superpowers/specs/2026-06-30-benchmark-suite-spec.md`) and corpus generator (Sprint 4) with V2-specific metrics: persistence time, resolution/lookup time, recomputation-avoided rate, and cold-vs-warm-start latency. If that extension later surfaces a genuine architectural question the existing spec's register cannot answer, RM-06 is written then, not before.
5. **The concurrency/multi-process consistency gap, discovered during the mechanism-layer review and not addressed anywhere in ARCH-023 through ARCH-036, must be resolved by explicit statement before Sprint 1 begins** — either as a stated Sprint 1 scope boundary ("single-process access only; concurrent access is explicitly out of scope") or, if that boundary cannot be honestly assumed, as a new governance review. Silence on this point is not an acceptable resolution.
6. **Governance escalation remains exactly as V2-ROADMAP-001 §1 and ARCH-031 §9 already define it**: implementation or an ADR that cannot proceed without contradicting a Closed Architectural Decision or a mechanism-document guarantee halts and escalates to a new Architecture Governance Review. This ADR does not relax that rule — it only removes architecture-for-its-own-sake as an accepted activity going forward.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Continue to RM-05, RM-06, and further mechanism refinement before any implementation | Analysis paralysis risk with no evidentiary basis for further design; the concurrency gap was found by review, not prevented by more review — implementation is now the more productive source of findings |
| Freeze the architecture without resolving how the concurrency gap is handled | Would ship an unstated assumption into Sprint 1 code, exactly the failure mode ARCH-031 §9 and every mechanism document's escalation rule exist to prevent |
| No formal milestone; begin implementation informally | Loses the explicit accountability and clear seam ADR-0012 already established as this repository's pattern for exactly this kind of phase transition |
| Treat RM-06 as still required before benchmarking starts | Ignores that its practical intent is already met by existing, approved repository assets; would duplicate `docs/archive/superpowers/specs/2026-06-30-benchmark-suite-spec.md` rather than extend it |

## Consequences

### Positive
- Implementation Sprint planning can treat the mechanism layer as a stable dependency, per the same reasoning ADR-0012 applied to the platform foundation.
- Architecture review conversations shift from "is this right?" to "does the implementation conform?" (ARCH-036 §1) — a faster, evidence-grounded question.
- The existing benchmark suite and corpus generator are recognized as reusable V2 assets rather than rebuilt, avoiding duplicate effort.
- The concurrency gap is surfaced and tracked rather than silently inherited by whichever engineer writes Sprint 1 first.

### Negative
- Any real conceptual gap beyond the concurrency one already found will now surface during implementation, which is more expensive to correct than a documentation change — accepted as the correct trade-off given two consecutive review cycles found nothing else.
- RM-05's deferral carries re-litigation risk: if AI-derived artifacts become real sooner than expected, RM-05 must be written under schedule pressure rather than proactively.
- Extending the existing benchmark suite for V2 metrics is deferred work, not eliminated work — it must still happen before Track 3's benchmarking claims (indexing/persistence/resolution timing, recomputation avoided) can be measured.

## Related

- ADR-0001: Use Architecture Decision Records
- ADR-0012: Milestone 1 — Platform Foundation Freeze (precedent for this decision's format)
- [AGR-001](../Reviews/AGR-001.md) through [AGR-004](../Reviews/AGR-004.md)
- [ARCH-023](../002-Architecture/ARCH-023-V2-Architectural-Boundary.md) through [ARCH-036](../002-Architecture/ARCH-036-Mechanism-Validation-and-Conformance.md)
- [V2-ROADMAP-001](../002-Architecture/V2-ROADMAP-001-Architecture-Program.md)
- `docs/archive/superpowers/specs/2026-06-30-benchmark-suite-spec.md`
- `docs/archive/superpowers/plans/2026-07-01-sprint-4-enterprise-corpus.md`
