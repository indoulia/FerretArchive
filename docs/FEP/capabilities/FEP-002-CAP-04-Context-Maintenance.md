# FEP-002-CAP-04 — Context Maintenance

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-04 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.4 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

Context that silently goes stale is worse than no context, because it is trusted without deserving to be. Context Maintenance exists to keep what Ferret knows in step with what is actually true of its sources, and to make the currency of that knowledge knowable rather than assumed.

## 2. Responsibilities

- Detect that a source, or the structured context derived from it, may have changed.
- Determine when previously acquired or organized context should be considered stale.
- Trigger re-acquisition (via Context Acquisition) and re-organization (via Context Organization) when change is detected.
- Track and expose the freshness — age, last-confirmed-current time — of every unit of context.
- Invalidate context that is no longer valid, for example because its source was removed or its scope changed, so it cannot be mistakenly assembled as current.
- Distinguish between "not yet checked," "confirmed current," and "known stale" states.

## 3. Non-Responsibilities

- Must never itself re-read a source — it delegates that to Context Acquisition.
- Must never itself re-derive structure — it delegates that to Context Organization.
- Must never decide what to deliver to a specific request — it determines eligibility only, which Assembly then uses.
- Must never silently drop stale context without recording that it did so — silence here directly violates Product Principle P3.

## 4. Inputs

- Change signals from source systems or from polling — either interaction shape is architecturally valid per FEP-001 §6.
- Structural change signals from Context Organization.
- Workspace scope changes from Workspace Definition.

## 5. Outputs

- Freshness state — current, stale, or unknown — for every unit of context.
- Re-acquisition and re-organization triggers.
- Invalidation signals for context that is no longer valid.

## 6. Context Objects

- **Freshness State** — the conceptual status of a context unit's currency, with an associated age.
- **Change Signal** — a conceptual event indicating that a source or derived structure may need re-processing.
- **Invalidation** — a conceptual record that a previously valid context unit is no longer valid, and why.

## 7. Relationships

Triggers Context Acquisition and Context Organization. Consumes scope-change signals from Workspace Definition. Supplies freshness state that Context Assembly uses to judge eligibility. Supplies freshness facts to Provenance & Attribution. Reports staleness and invalidation activity to Observability & Health.

## 8. Constraints

- **Business.** Freshness expectations may vary per workspace, per Workspace Definition's configuration; Maintenance honors workspace-declared expectations rather than one fixed standard everywhere.
- **Product.** Maintenance must never present unconfirmed context as confirmed-current; when currency cannot be determined, the honest state is "unknown," not "assumed current."
- **Context integrity.** Invalidation must propagate completely — a unit invalidated at the source must not remain assemblable as though nothing happened.

## 9. Success Criteria

- The freshness of any given context unit is knowable at any time.
- Change in a source is reflected within the bounds a workspace has declared acceptable.
- No context is delivered as current when its actual freshness state is stale or unknown — enforced jointly with Context Assembly.

## 10. Failure Modes

- **Silent staleness** — context ages past its freshness expectation without this being reflected anywhere visible, violating P3.
- **Change storms** — overly sensitive change detection triggers excessive re-acquisition or re-organization, starving the pipeline.
- **Orphaned invalidation** — a source is removed but its derived context is never invalidated, leaving ghost context assemblable indefinitely.
- **Freshness blindness** — a workspace's freshness expectations are undeclared or unclear, leaving Maintenance with no standard to check itself against.

## 11. Future Evolution

Workspace- and source-specific freshness expectations becoming more granular — some sources near-real-time, others acceptable at a daily cadence. Predictive staleness, anticipating that certain context is likely to age based on historical change patterns rather than purely reactive detection. Maintenance signals becoming an input to Federation's cross-workspace freshness reconciliation.
