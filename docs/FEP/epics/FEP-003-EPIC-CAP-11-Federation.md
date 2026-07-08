# FEP-003-EPIC-CAP-11 — Engineering Program: Federation

| Field | Value |
|---|---|
| **Document ID** | FEP-003-EPIC-CAP-11 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-003 — Engineering Program](../FEP-003-Engineering-Program.md) |
| **Capability Source** | [FEP-002-CAP-11 — Federation](../capabilities/FEP-002-CAP-11-Federation.md) |
| **Status** | Draft — Prompt 3 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Capability Summary

Federation extends the capability model across multiple workspaces, composing already-organized, already-current, already-assemblable context for requests that span more than one workspace. It never acquires, organizes, or maintains context itself, and it never overrides a single workspace's own access decision.

## 2. Engineering Epics

### E11.1 — Federation Scope Resolution

- **Purpose.** Determine which workspaces participate in a cross-workspace request.
- **Scope.** Resolving Workspace Relationships into a concrete Federation Scope for a given request.
- **Success Definition.** The set of workspaces relevant to a cross-workspace request is correctly and predictably resolved.

### E11.2 — Cross-Workspace Composition

- **Purpose.** Compose context from multiple workspaces without duplicating their own capabilities.
- **Scope.** Composing already-assemblable context per workspace into a single cross-workspace result; reconciling relevance and ranking across workspace boundaries.
- **Success Definition.** A cross-workspace result is coherent and does not weaken any contributing workspace's own guarantees.

### E11.3 — Partial-Success Transparency

- **Purpose.** Represent partial composition outcomes honestly.
- **Scope.** Recording per-workspace contribution outcomes; disclosing partial composition to the consumer.
- **Success Definition.** A consumer can always tell which workspaces contributed successfully to a cross-workspace result and which did not, and why.

## 3. Features

### E11.1 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F11.1.1 — Federation Scope Determination | Resolve a request's relevant Federation Scope from declared Workspace Relationships. | Establishes which workspaces Cross-Workspace Composition should draw from. | F01.3.1, F01.3.2 | A cross-workspace request's Federation Scope is resolvable and predictable given the declared relationships. |

### E11.2 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F11.2.1 — Cross-Workspace Context Composition | Compose context contributed by each workspace in a Federation Scope into a single result. | Serves a consumer whose need spans multiple workspaces without a fundamentally different product. | F11.1.1, per-workspace E05.3 already functioning | A composed cross-workspace result correctly reflects contributions from every workspace in scope that succeeded. |
| F11.2.2 — Cross-Workspace Relevance Reconciliation | Reconcile relevance and ranking judgments made independently by each contributing workspace's Assembly. | A cross-workspace result is ranked coherently rather than as an arbitrary concatenation. | F11.2.1 | A cross-workspace ranked result is demonstrably more useful than a naive concatenation of unranked, per-workspace results. |

### E11.3 Features

| Feature | Objective | Product Outcome | Dependencies | Completion Criteria |
|---|---|---|---|---|
| F11.3.1 — Contribution Outcome Recording | Record, per workspace in a Federation Scope, whether it succeeded, was denied, was stale, or was absent. | Provides the basis for honest partial-composition disclosure. | F11.2.1 | Every contributing workspace's outcome for a given cross-workspace request is recorded and retrievable. |
| F11.3.2 — Partial Composition Disclosure | Disclose partial composition to the consumer, consistent with Product Principle P5. | Prevents a partial cross-workspace result from being mistaken for a complete one. | F11.3.1 | A cross-workspace result with one or more non-succeeding contributing workspaces is never presented as complete. |

## 4. Engineering Dependencies

- **Prerequisite Features.** F01.3.1, F01.3.2, and the full per-workspace chain culminating in F05.3.1/F05.3.2 and F06.3.1/F06.3.2 for every workspace expected to participate.
- **Prerequisite Epics.** E01.3 (Workspace Relationships), and — critically — every epic in Context Acquisition, Organization, Maintenance, Assembly, Delivery, Provenance & Attribution, and Access Control & Policy, complete within each individual participating workspace.
- **Prerequisite Capabilities.** Every other capability in the model (FEP-001 §4: Federation depends on the entire rest of the model already being satisfied).

## 5. Execution Order

1. **E11.1** — must exist before any cross-workspace composition can be attempted.
2. **E11.2** — depends on scope resolution and on per-workspace Assembly already being mature.
3. **E11.3** — depends on composition existing, since there is nothing to disclose as partial before a composition is attempted.

## 6. Capability Completion Gates

- **Functional completeness.** A request spanning at least two related workspaces produces a coherent, composed result.
- **Validation readiness.** A simulated denial or staleness in one contributing workspace is verified to produce a correctly disclosed partial result, not a silently narrowed one.
- **Documentation readiness.** Federation Scope, Cross-Workspace Composition, and Contribution Outcome are documented clearly enough that a consumer-facing surface can present partial results honestly.
- **Review completion.** FEP-002-CAP-11's non-responsibilities (no acquiring/organizing/maintaining itself, no overriding a workspace's own access decision, no self-declared relationships) confirmed unviolated.

## 7. Risks

- **This capability cannot be meaningfully planned in detail until the rest of the model is mature.** Because Federation depends on the entire capability model already being satisfied per workspace, this epic/feature breakdown is necessarily more provisional than any other capability's; treat it as a placeholder that will need revisiting once real multi-workspace use cases exist.
- **Privilege-escalation risk is a planning risk, not just an implementation risk.** If Cross-Workspace Composition's features are scoped without an explicit check against each contributing workspace's access policy, the resulting plan itself could imply a design that leaks information no single workspace would have permitted.
- **Relationship model insufficiency.** If Workspace Definition's Relationship Type Model (E01.3) is scoped too narrowly, Federation Scope Resolution may lack the vocabulary to express real organizational relationships, forcing rework across both capabilities.

## 8. Deferred Work

- Federation across organizational boundaries (not just within one organization's related workspaces) — deferred pending a governance decision (FEP-001 Open Question 5).
- Federation-aware extension points — deferred to Extensibility's maturity.
