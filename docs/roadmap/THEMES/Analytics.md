# Theme: Analytics

Observability, usage, and cost visibility — making Ferret's own state and consumption inspectable, per [FEP-002-CAP-10](../../FEP/capabilities/FEP-002-CAP-10-Observability-Health.md).

## Roadmap Items

- **Usage & Cost Analytics** — dashboards over the Usage Ledger (token counts today, per [09-Analytics.md](../Workspace-Intelligence/09-Analytics.md); cost attribution later). See [FUTURE/V3.md](../Future/V3.md).
- **Cost / billing infrastructure** — layering a pricing-rate rollup over existing token events. Currently deferred, blocked on an unresolved multi-user cost-attribution question — see [Deferred-Scope.md](../Future/Deferred-Scope.md).

## Design Note

The Usage Ledger's schema is deliberately built so a cost model can be added later without a schema migration — analytics maturity here is a sequencing choice, not a redesign.
