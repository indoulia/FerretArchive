# FEP-004-SPEC-F06.3.2 — Denial/Absence Disambiguation

## 1. Specification Metadata

| Field | Value |
|---|---|
| **Specification ID** | FEP-004-SPEC-F06.3.2 |
| **Capability** | [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) |
| **Epic** | E06.3 — Access-Respecting Hand-off |
| **Feature** | F06.3.2 — Denial/Absence Disambiguation |
| **Parent Documents** | [FEP-004 — Engineering Specifications](../FEP-004-Engineering-Specifications.md) · [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) · [FEP-003-EPIC-CAP-06 — Context Delivery](../epics/FEP-003-EPIC-CAP-06-Context-Delivery.md) · [FEP-002-CAP-06 — Context Delivery](../capabilities/FEP-002-CAP-06-Context-Delivery.md) · [FEP-001 — Product Architecture](../FEP-001-Product-Architecture.md) |
| **Status** | Draft — Prompt 4 output |
| **Version** | 1.0 |
| **Last Updated** | 2026-07-08 |

---

## 2. Purpose

A consumer who cannot tell whether they were denied context or whether nothing relevant existed cannot trust anything Ferret tells them — the two outcomes look identical unless Delivery makes the difference visible. Denial/Absence Disambiguation exists to guarantee that these two outcomes are always distinguishable to the consumer, protecting trust in the whole system.

## 3. Scope

- Producing a distinguishable outcome for the consumer when context was withheld due to an access denial (per F06.3.1's gating).
- Producing a distinguishable outcome for the consumer when no relevant context existed at all.
- Ensuring these two outcomes are never presented in a way that could be confused with one another, across every supported delivery surface.

## 4. Out of Scope

- Making the gating decision itself (which content is withheld and why) — that is F06.3.1 (Access-Gated Delivery), a precondition of this Feature.
- Determining that no relevant context exists in the first place — that is a Context Assembly concern (selection producing an empty or gap-reported result, F05.3.2); this Feature consumes that outcome, it does not produce it.
- Recording why specific context was excluded from an assembled result for reasons other than access (e.g., freshness, relevance) — that is Assembly Gap Reporting (F05.3.2), a distinct concept from a denial outcome.
- Selecting the delivery surface through which either outcome is communicated — that is F06.1.1 (Delivery Surface Selection).

## 5. Engineering Requirements

1. When context is withheld from a consumer due to an access denial (F06.3.1), that outcome must be represented in a way that is observably different from an outcome representing "no relevant context existed."
2. When no relevant context exists for a request, that outcome must be represented in a way that is observably different from a denial outcome.
3. This distinction must be preserved across every supported delivery surface (F06.1.1) and every delivery mode (one-off and subscription notification).
4. A consumer must be able to determine, from the delivered outcome alone, which of the two states (denial or absence) applies, without needing separate out-of-band confirmation.
5. The disambiguation must not itself reveal more about denied content than the fact that a denial occurred — disambiguation must not become a channel for leaking the substance of denied context.

## 6. Inputs

- The gating outcome produced by Access-Gated Delivery (F06.3.1), including any denial.
- The outcome of Assembly's selection, indicating whether relevant context existed at all.

## 7. Outputs

- A denial outcome, observably distinguishable to the consumer.
- An absence outcome, observably distinguishable to the consumer, and distinguishable from a denial outcome.

## 8. Preconditions

- Access-Gated Delivery (F06.3.1) has produced a gating outcome for the request.

## 9. Postconditions

- The consumer can determine, from what they received, whether they were denied context or whether nothing relevant existed — never left to guess.
- No denial is ever mistakable for an absence, and no absence is ever mistakable for a denial, in what the consumer observes.

## 10. Dependencies

**Capability dependencies.** Access Control & Policy (indirectly, via F06.3.1's gating outcome, which originates a denial state this Feature must represent distinctly).

**Epic dependencies.** None beyond this capability's own E06.3 — this Feature builds directly on its sibling Feature within the same epic.

**Feature dependencies.** F06.3.1 (Access-Gated Delivery) — per the E06.3 Features table.

**External dependencies.** None beyond those already required by F06.3.1; this Feature adds no new external dependency of its own.

## 11. Constraints

**Business constraints.** Denials and partial deliveries must be distinguishable from the simple absence of relevant context, so a consumer is not misled into thinking nothing existed when something existed but was restricted (FEP-002-CAP-06 §8, Context integrity).

**Product constraints.** The disambiguation itself must not become a new fidelity concern — the distinguishing signal must be presented as faithfully as any other delivered outcome (consistent with F06.1.2's fidelity guarantee).

**Context integrity constraints.** Ambiguous denial is an explicit Failure Mode of this capability (FEP-002-CAP-06 §10) and must never occur.

**Trust constraints.** Per P5, degrading by scope must be visible — a denial or an absence are both forms of "less than everything," and each must be named as what it is, never smoothed into silence.

**Policy constraints.** Per P6, this Feature must not re-derive or second-guess the gating decision made by F06.3.1; it consumes that decision as given.

## 12. Acceptance Criteria

1. A simulated denial and a simulated absence, delivered through the same surface, produce observably different outcomes to the consumer.
2. This distinction holds across every supported delivery surface (F06.1.1).
3. This distinction holds for both one-off delivery and subscription notification (F06.2.2) delivery modes.
4. A consumer can determine which of the two states occurred without needing information beyond what was delivered.
5. A denial outcome does not reveal the substance of the denied content beyond the fact of denial itself.

## 13. Validation Requirements

- That every denial outcome and every absence outcome are independently identifiable and never conflated, across all surfaces and modes.
- That the disambiguation signal itself is delivered with the same fidelity guarantees as any other delivered outcome.
- That disambiguation does not leak denied content's substance.

## 14. Failure Conditions

- **Ambiguous denial.** A consumer cannot tell whether they were denied context or whether none existed, undermining trust in the whole system, in tension with Product Principle P5 (FEP-002-CAP-06 §10). Expected behavior: this state must never be allowed to persist undetected; any surface or mode found to produce it is treated as a capability-level defect.
- **Denial signal leakage.** The act of disambiguating a denial inadvertently reveals something about the denied content's substance. Expected behavior: treated as an access-control defect in its own right, since it partially defeats the purpose of the original denial.

## 15. Traceability

Product Vision (Mission: delivers trustworthy engineering context to any human, AI system, or engineering tool that needs it) → Goal G4 (Trustworthy context) → Product Principles P4 (No privileged consumer), P5 (Degrade by scope, not by silent omission) → Capability FEP-002-CAP-06 (Context Delivery) → Epic E06.3 (Access-Respecting Hand-off) → Feature F06.3.2 (Denial/Absence Disambiguation).

## 16. Future Considerations

- As Access Control & Policy matures partial-permission outcomes (F08.2.2 — Partial Permission Outcomes), this Feature's disambiguation model may need to extend from a two-state (denial/absence) distinction to a richer set of distinguishable outcomes.
- Delivery patterns spanning federated workspaces, once Federation matures, must preserve this disambiguation guarantee across workspace boundaries (FEP-002-CAP-06 §11).
