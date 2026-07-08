# FEP-002-CAP-09 — Extensibility

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-09 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.9 — Capability Model |
| **Status** | Draft — Prompt 2 output (amended per FEP-003A) |
| **Last Updated** | 2026-07-08 |

---

> **Amendment (2026-07-08).** §2, §4, §5, §6, §7, §9, and §10 below restore the Context Organization extension surface that FEP-001 §2.9 assigns to this capability but this document's original Prompt 2 output omitted. Corrected per the Required Correction in [FEP-003A — Engineering Program Review & Freeze](../reviews/FEP-003A-Engineering-Program-Review.md).

## 1. Purpose

The world outside Ferret — the sources worth acquiring from, the consumers worth serving — will keep changing after this architecture is fixed. Extensibility exists to keep the capability model open to that change without requiring the core capabilities to be redesigned every time it happens, making Product Goal G5 real rather than aspirational.

## 2. Responsibilities

- Define, conceptually, where a new kind of source can be added for Context Acquisition to observe.
- Define, conceptually, where a new kind of structure can be added for Context Organization to extract or relate.
- Define, conceptually, where a new kind of consumer or delivery surface can be added for Context Delivery to serve.
- Ensure that adding a new source type, structure type, or consumer type never requires redefining Context Maintenance's or Assembly's responsibilities.
- Maintain a conceptual inventory of currently supported source types, structure types, and consumer types, so the product's actual extensibility surface is knowable.

## 3. Non-Responsibilities

- Must never itself acquire, organize, assemble, or deliver context — it defines where those capabilities may be extended, it does not perform their work.
- Must never allow a source-specific or consumer-specific behavior to become a special case inside another capability — that would defeat the purpose of having an extension point at all.
- Must never itself decide whether a proposed extension is a good idea — that is a governance decision made elsewhere, using Extensibility's defined points as the mechanism.

## 4. Inputs

- Proposals, described conceptually rather than technically, for new source types, structure types, or consumer types.
- The current, stable boundaries of Context Acquisition, Context Organization, and Context Delivery that extension points must respect.

## 5. Outputs

- Defined, documented extension points at the Acquisition, Organization, and Delivery boundaries.
- A conceptual inventory of currently supported source types, structure types, and consumer types.

## 6. Context Objects

- **Extension Point** — a conceptual, defined place in the capability model where new source, structure, or consumer support can be added without altering the capability it extends.
- **Source Type** — a conceptual category of source Acquisition knows how to observe, such as version control or an issue tracker, described as a category rather than an integration.
- **Structure Type** — a conceptual category of structural pattern Organization knows how to extract or relate, described as a category rather than a schema.
- **Consumer Type** — a conceptual category of consumer Delivery knows how to serve.

## 7. Relationships

Attaches to Context Acquisition, for new source types; Context Organization, for new structure types; and Context Delivery, for new consumer types, per FEP-001 §4 and §2.9. Indirectly protects Context Maintenance and Assembly by ensuring they never need to special-case a specific source, structure, or consumer type.

## 8. Constraints

- **Business.** An extension must never require compromising a capability's boundary as defined in FEP-001 — extensibility is additive, not an excuse to blur responsibility.
- **Product.** The cost of adding a new source or consumer type should not grow with the number already supported; extensibility that gets harder as the system grows has failed its purpose.
- **Context integrity.** A new source type must still satisfy Provenance & Attribution's requirements, and a new consumer type must still be gated by Access Control & Policy — extension points cannot bypass the trust capabilities.

## 9. Success Criteria

- A new source, structure, or consumer type can be described and evaluated against the extension points without needing to redesign any other capability.
- The current inventory of supported source, structure, and consumer types is explicit and known, not implicit in whatever happens to have been built.
- Extending the system does not degrade the guarantees — provenance, access control, freshness — that already-supported types rely on.

## 10. Failure Modes

- **Special-casing** — a new source, structure, or consumer type is added by carving a special case into Context Maintenance or Assembly, or by bypassing the relevant extension point within Acquisition, Organization, or Delivery itself, instead of going through the defined extension point — quietly eroding capability boundaries.
- **Extension point rot** — extension points are defined but not actually exercised or kept current, so every new source or consumer still requires bespoke work in practice.
- **Unbounded extension surface** — anything can be declared a new source type with no evaluation of fit, feeding back into the unbounded acquisition surface risk in FEP-001 §8.
- **Trust bypass** — a new extension skips provenance or access control obligations because the extension point didn't require them.

## 11. Future Evolution

A more formal, evaluable process for proposing and admitting new source and consumer types as the ecosystem around Ferret grows — the basis for FEP-001's Generation 4, Ecosystem. Third-party-authored extensions becoming possible once extension points are mature and stable enough to support them. Extension points evolving to support federation-aware sources and consumers that are naturally multi-workspace in nature.
