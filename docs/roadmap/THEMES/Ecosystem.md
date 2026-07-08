# Theme: Ecosystem

Third-party extensibility and marketplace — Generation 4 of [FEP-001 §7](../../FEP/FEP-001-Product-Architecture.md), where [CAP-09 Extensibility](../../FEP/capabilities/FEP-002-CAP-09-Extensibility.md) matures into a genuine external extension surface.

## Roadmap Items

- **Extension Marketplace** — a public registry of third-party source connectors, structure extractors, and delivery integrations. First steps in [FUTURE/V4.md](../Future/V4.md); full maturity in [FUTURE/LONG-TERM.md](../Future/LONG-TERM.md).

## Open Question

Whether this matures into a public marketplace or stays enterprise-internal (private per-organization connector registries) is unresolved — see [FERRET-PRODUCT-ROADMAP.md §10](../FERRET-PRODUCT-ROADMAP.md#10-open-questions).

## Sequencing Rationale

Ecosystem openness is sequenced last because it depends on Extensibility's extension points (already defined in FEP-001 §2.9) being proven internally first — through Enterprise Knowledge's own connector additions — before third parties are given the same surface.
