# Current Release Snapshot

Two tracks currently define "current" for Ferret — they are intentionally not reconciled (see [FEP README](../../FEP/README.md)):

## Engineering Program: FEP v1.0 (frozen)

Complete: [FEP-001 Product Architecture](../../FEP/FEP-001-Product-Architecture.md), [FEP-002 Capability Catalog](../../FEP/FEP-002-Capability-Catalog.md), [FEP-003 Engineering Program](../../FEP/FEP-003-Engineering-Program.md) + [FEP-003A Review](../../FEP/reviews/FEP-003A-Engineering-Program-Review.md), [FEP-004 Engineering Specifications](../../FEP/FEP-004-Engineering-Specifications.md).

Defines eleven capabilities (Workspace Definition; Context Acquisition, Organization, Maintenance, Assembly, Delivery; Provenance & Attribution, Access Control & Policy; Extensibility, Observability & Health; Federation) and a five-generation maturity model. This is planning, not implementation — execution is gated on AEF reaching General Availability.

## Shipped Product: v0.16.0

Enterprise Content Pack 1 — 7 parser formats (source/text, CSV/TSV, PDF, Word, Excel), 79 recognized extensions, first OIDC-based npm publish, anonymous `npm install` restored. See [release notes](../../012-Releases/v0.16.0.md).

DOGFOOD-001 (real-repo dogfooding pass) is in progress on the `dogfooding` branch. See [Immediate-Product-Roadmap.md](../Immediate-Product-Roadmap.md) for the bridge plan between this and the next milestone.

## Strengths

- Reviewed, non-overlapping capability boundaries (FEP-002's own checklist).
- Provenance and access control are cross-cutting obligations by design, not add-ons.
- Zero-dependency Core enforced as an architectural invariant.
- Real, dogfooded parser/connector breadth already in production use.

## Known Limitations

- Workspace boundary is still single-repository in the shipped product.
- No RBAC beyond what v2.0 will ship; no audit logging; no cost/billing infrastructure.
- FEP-001 and the historical/`FUTURE-002` product narratives remain unreconciled — see [FERRET-PRODUCT-ROADMAP.md §9](../FERRET-PRODUCT-ROADMAP.md#9-product-risks).

See [FERRET-PRODUCT-ROADMAP.md §1](../FERRET-PRODUCT-ROADMAP.md#1-current-release) for the roadmap-level summary this expands on.
