# ARCH-028 — Ferret V2 Request Equivalence Architecture

| Field | Value |
|---|---|
| **Document ID** | ARCH-028 |
| **Version** | 1.1 |
| **Status** | Frozen |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Accepted (AGR-002) |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None — this document defines a conceptual relation, not a mechanism; no mechanism decision exists yet to warrant an ADR |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-025 (Artifact Validity Model) §3; ARCH-027 (Dependency Resolution Architecture) §4 — the two frozen sections this document amends |
| **Roadmap Item** | [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) RM-01 |
| **Resolves** | [AGR-001](../Reviews/AGR-001.md) §5, Deferred Question F5 (Request Identity & Equivalence) |

---

## Purpose

This document resolves AGR-001's Deferred Question F5: what makes two requests "the same" for the purpose of dependency resolution. ARCH-027 §4 asserted that a request "already knows which prior artifact... was produced for that same request" without ever defining what makes two requests equivalent. This document supplies that definition.

This is the first amendment to the frozen V2 Foundation Series. It is architecture-only: it defines a relation, not a mechanism. It does not become part of the frozen foundation on its own — per AGR-001 §8, any amendment to ARCH-023 through ARCH-027 requires a new governance review. This document concludes with the specific changes it proposes (§10) and remains a proposal until a new Architecture Governance Review accepts it.

---

## Scope

Covers:
- The concept of a request, as presupposed but never defined by ARCH-025's request-scoped input dependency (§3, shape 5) and ARCH-027's resolution model (§4)
- The properties that constitute a single request's identity
- The relation that determines whether two requests are equivalent
- Which forms of equivalence this architecture recognizes, and which it deliberately does not
- How request equivalence relates to dependency state, artifact validity, and dependency resolution
- The architectural guarantees request equivalence must uphold
- The specific amendments this document proposes to ARCH-025 and ARCH-027

Does not cover:
- Keys, hashes, fingerprints, or any other representation of a request
- Cache structures, retrieval algorithms, or storage mechanisms
- APIs of any kind
- AI provider details
- Any redefinition of an artifact (ARCH-024), of validity (ARCH-025), or of resolution (ARCH-027) beyond the specific amendments proposed in §10
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every term this document builds on — artifact, dependency shape, validity class, resolution outcome — is taken as-is from ARCH-024, ARCH-025, and ARCH-027. No new engine or subsystem is introduced. Where this document names a concept ARCH-025/ARCH-027 presupposed but never defined ("request"), it defines that concept in the same register the frozen series already uses — descriptive and structural, never mechanism-level.

---

## 1. What Is a Request?

**A request is the complete specification an engine is given in order to fulfil a specific need — the terms under which a satisfying artifact would be judged sufficient, prior to any decision about whether to produce one.**

A request is not an artifact. It is what an artifact, if produced, is produced *for*. Every artifact ARCH-024 catalogued that carries a request-scoped input dependency (ARCH-025 §3, dependency shape 5) — `ContextPackage`, `SearchResult` — was produced in answer to some request; ARCH-025 named that dependency shape but did not define the thing making the request. This document does not add a new dependency shape. It defines the entity dependency shape 5 already presupposed.

A request exists independently of whether anything is ever produced or persisted for it. An engine can receive a request and determine no computation is needed at all (per ARCH-027, a Satisfied outcome) — the request still occurred, still had an identity, and still needed a candidate identified for it (or not) before that outcome was reached.

---

## 2. What Architectural Properties Constitute Request Identity?

A request's identity is constituted by exactly three properties, taken together:

1. **The engine responsibility invoked.** Which of the owning engine's already-documented responsibilities (ARCH-001 §7.2) the request calls on — for example, Knowledge Engine's context-assembly responsibility as distinct from its query responsibility. This is not a new concept; ARCH-024 already attributes distinct artifacts (`ContextPackage`, `SearchResult`) to distinct responsibilities of the same real component.
2. **The complete explicit parameter set that responsibility's operation is defined over.** Every parameter the caller explicitly supplies and that bounds what a satisfying artifact must reflect — for `ContextPackage`, this is the full set ARCH-024 §4 already names: the query text, the requested scope, the token budget. Identity is constituted by the whole set, never a subset chosen for convenience.
3. **The ambient dependency scope the request is evaluated against, where not already captured by an explicit parameter in property 2.** Which portion of repository or knowledge state the request implicitly binds the answer to without the caller re-specifying it on every call — a specific workspace, a specific file, a specific specification. This connects a request to the dependency shapes ARCH-025 §3 already defines (shapes 1–4), without being one of them itself. Where a responsibility's operation already takes scope as an explicit parameter (property 2), that parameter is not counted again here — this property exists only for context a request binds to implicitly.

A request's identity is exactly these three properties. It is not a name, a sequence number, a timestamp, or any representation derived from them — this document defines identity structurally (by what was asked), never representationally (by how it might be encoded or compared).

---

## 3. What Makes Two Requests Equivalent?

**Two requests are equivalent when they invoke the same engine responsibility, with parameter sets that are equal under that responsibility's own operation contract, evaluated against the same dependency scope.**

"Equal under the operation's own contract" means equality is judged at the level of what a parameter *means* to the responsibility being invoked, not at the level of its literal representation. Two requests that specify a token budget through different but contract-equivalent means (for example, an explicit value and an equivalent default) are still asking the same question. This document does not define how such contract-level equality would be established — that is a mechanism concern (§9) — only that equivalence is a semantic relation on what was asked, not a syntactic one on how it was written down.

Equivalence is a relation between two requests alone. It does not, by itself, say anything about whether an artifact produced for one of them still holds — that is a separate question (§6).

---

## 4. What Forms of Equivalence Are Architecturally Recognized?

This architecture recognizes exactly one form: **exact, contract-level equivalence**, as defined in §3.

No partial, approximate, fuzzy, or subsuming form of equivalence is recognized. A request whose scope is broader than another's, or whose parameters overlap without being equal, is **not** equivalent under this architecture — even if a satisfying answer to the broader request would, in principle, also satisfy the narrower one. Recognizing such a form would require a rule for comparing and ranking degrees of overlap, which is a retrieval concern (§9), not an architectural one.

This is a deliberate, conservative default, consistent with the fail-closed discipline already established (ARCH-025 §8, ARCH-026 §7, ARCH-027 §5): where equivalence is not exact, requests are non-equivalent, never "probably" equivalent.

---

## 5. Relationship Between Request Equivalence and Dependency State

Request identity (§2) is the formal elaboration of ARCH-025 §3's dependency shape 5 (request-scoped input dependencies) — nothing more. It is not, and does not touch, dependency shapes 1 through 4 (source content, derived-artifact, index/knowledge-state, configuration/registration dependencies).

This separation matters: two requests can be exactly equivalent (§3) while the dependency state shapes 1–4 describe has changed in the interim, and two non-equivalent requests say nothing about whether that state has changed at all. Request equivalence answers "was the same thing asked" — an atemporal question about the requests themselves. Whether the answer to that question is still current is the separate, subsequent question ARCH-025's validity model already answers (§6).

---

## 6. Relationship Between Request Equivalence and Artifact Validity

Request equivalence and artifact validity are two different axes, and this architecture keeps them so deliberately:

- **Request equivalence determines candidacy** — whether a prior artifact is even a candidate to check, because it was produced for an equivalent request (§3).
- **Artifact validity (ARCH-025) determines acceptability** — whether that candidate, once identified, still holds given the current state of dependency shapes 1–4 (ARCH-025 §1, §3).

A candidate must clear both: it must have been produced for an equivalent request, *and* it must still be valid. Request equivalence does not make an artifact valid, and validity does not make an artifact a candidate for a request it was never produced for. Neither concept is redefined by the other.

---

## 7. Relationship Between Request Equivalence and Dependency Resolution

ARCH-027 §4 stated that a request "already knows which prior artifact... was produced for that same request," without defining what that means. This document supplies exactly that definition: **a candidate enters ARCH-027's resolution process (§3) only if it was produced for a request equivalent, per §3 of this document, to the current one.**

This changes nothing about ARCH-027's resolution outcomes (Satisfied / Not satisfied / Indeterminate, ARCH-027 §3) or its architectural guarantees (ARCH-027 §5). It replaces the undefined assumption those sections were built on with a precise gate that must pass before resolution's validity check (ARCH-025) is even reached. Where no equivalent request has a prior artifact, there is no candidate, and — exactly as ARCH-027 §4 already states for that case — the outcome is Not-satisfied by default, not because a dependency changed, but because there was never a candidate.

---

## 8. Architectural Guarantees

| Guarantee | Statement | Basis |
|---|---|---|
| Determined by requests alone | Equivalence is judged solely from the two requests' own identity properties (§2) — never from artifact content, and never from which artifacts happen to exist | §3; extends ARCH-023 §5 no-cross-inference discipline |
| Symmetric and transitive | If request A is equivalent to request B, B is equivalent to A. If A is equivalent to B and B is equivalent to C, A is equivalent to C. Equivalence is a relation, not a search or a ranking | §3, §4 |
| Non-equivalence is the default | Absent an exact, contract-level match (§3, §4), requests are treated as non-equivalent | Extends the fail-closed principle (ARCH-025 §8, ARCH-026 §7, ARCH-027 §5) to request equivalence |
| Deterministic | The same two requests always yield the same equivalence outcome | AG-004 (Deterministic Behaviour), carried forward |
| Independent of dependency state | Equivalence is evaluated only from request identity (§2); it is never influenced by whether dependency shapes 1–4 have changed | §5 |
| No side effects | Determining equivalence never invokes `IModelProvider`, never produces an artifact, and never mutates any persisted state | ARCH-023 §9, carried forward |

---

## 9. Explicit Non-Goals

This document does **not** define:

- Any representation of a request — no key, hash, fingerprint, identifier, or encoding of any kind
- Any mechanism for comparing, storing, or looking up requests
- Any partial, approximate, or subsuming form of equivalence (§4)
- Any retention policy for how long a request's identity remains checkable, or how many prior requests are considered
- Any API surface through which a request is submitted or compared
- Any AI provider detail
- Any change to ARCH-025's validity model or ARCH-027's resolution outcomes beyond the specific amendments in §10
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## 10. Proposed Amendments to the Frozen Foundation

These are proposals. Per AGR-001 §8, they do not take effect until a new Architecture Governance Review (AGR-002) accepts this document and confirms no Closed Architectural Decision is contradicted.

### Amendment 1 — ARCH-025 §3 (Dependency Types That Determine Validity)

**Current text**, dependency shape 5, verbatim (verified against the live document):
> "5. **Request-scoped input dependencies** — parameters supplied by the specific request that produced the artifact (a query string, a token budget), rather than repository state. Governs `SearchResult` and `ContextPackage`."

**Proposed replacement:**
> "5. **Request-scoped input dependencies** — parameters supplied by the specific request that produced the artifact (a query string, a token budget), rather than repository state. Governs `SearchResult` and `ContextPackage`. "Request" and the conditions under which two requests are equivalent are formally defined in ARCH-028."

**Rationale:** ARCH-025 named this dependency shape without defining "request." This amendment adds a cross-reference only — it does not change what the shape covers.

### Amendment 2 — ARCH-027 §4 (Interaction Between Dependency State and Artifact Selection)

**Current text, first sentence of the paragraph only, verbatim (verified against the live document):**
> "The request-scoped input dependency (ARCH-025 §3, dependency shape 5) is what identifies which candidate, if any, is even in scope: an engine fulfilling a specific request (a specific query, a specific file, a specific specification) already knows which prior artifact — if one exists — was produced for that same request."

**Proposed replacement for that sentence only:**
> "The request-scoped input dependency (ARCH-025 §3, dependency shape 5) is what identifies which candidate, if any, is even in scope: a candidate enters resolution only if it was produced for a request equivalent — per ARCH-028's exact, contract-level equivalence relation — to the current one."

**The paragraph's remaining two sentences** ("Dependency state's role in resolution is confirmatory, not exploratory..." through "...both of those are retrieval concerns.") **are untouched** and follow the replacement sentence exactly as they follow the current one.

**Rationale:** Replaces the undefined assumption ARCH-027 §4 relied on with a precise, cited relation. No other part of ARCH-027 changes — its outcomes (§3) and guarantees (§5) are unaffected, as §7 of this document establishes.

### Governance Requirement

This document must be reviewed by a new Architecture Governance Review (AGR-002) before Amendments 1 and 2 are applied to ARCH-025 and ARCH-027. Upon acceptance, ARCH-025 increments to v1.2 and ARCH-027 to v1.1, both citing AGR-002, and this document's own status changes from Draft to Frozen alongside them — consistent with the precedent AGR-001 established for the rest of the series.

---

## Cross References

| Document | Relationship |
|---|---|
| [AGR-001 §5](../Reviews/AGR-001.md) | The deferred question (F5) this document resolves |
| [ARCH-025 §3](ARCH-025-Artifact-Validity-Model.md) | Amended by this document (§10, Amendment 1) |
| [ARCH-027 §4](ARCH-027-Dependency-Resolution-Architecture.md) | Amended by this document (§10, Amendment 2) |
| [ARCH-023 §5, §9](ARCH-023-V2-Architectural-Boundary.md) | No-cross-inference and no-side-effects principles this document extends to request equivalence (§8) |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | RM-01 — this document is that roadmap item |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial Request Equivalence Architecture — resolves AGR-001 F5. Proposed amendments to ARCH-025 and ARCH-027 pending AGR-002. |
| 1.1 | 2026-07-03 | Ferret Core Team | AGR-002 review corrections — clarified the boundary between identity properties 2 (explicit parameters) and 3 (ambient scope) in §2; corrected both proposed amendments' "current text" citations to be byte-exact against the live ARCH-025/ARCH-027 text. No change to §1–§9's conceptual content. |
