# FEP-001 — Ferret Product Architecture & Capability Definition

| Field | Value |
|---|---|
| **Document ID** | FEP-001 |
| **Version** | 1.0 |
| **Status** | Draft — Prompt 1 output |
| **Program** | Ferret Engineering Program (FEP) |
| **Last Updated** | 2026-07-08 |

---

## Purpose and Standing

This document defines the product architecture of Ferret as a Context Operating System (Context OS). It is written independently of AEF (Agentic Engineering Framework) implementation, which is under separate, active development. Ferret implementation is intentionally deferred until AEF reaches General Availability; this document makes no implementation, runtime, technology, protocol, or API decisions.

This document does not amend, reconcile, or supersede Ferret's existing product documentation (`docs/000-Overview/`, `docs/001-Product/PRD-001.md`, `docs/002-Architecture/`). Those documents describe a broader product identity — "the AI Workspace Operating System," including an embedded agent runtime and specification-governance workflow — accumulated over the project's prior sprints. This document deliberately narrows scope: Ferret's job is to acquire, organize, maintain, assemble, and deliver engineering context. Reasoning, generation, specification enforcement, and review governance are consumer-side concerns (AEF's, or another AI system's) built on top of what Ferret delivers, not capabilities Ferret itself provides. Where this document's scope differs from the historical documents, that difference is intentional and is tracked as an open question (§9), not silently resolved.

---

## 1. Product Vision

### 1.1 Mission

Ferret is a Context Operating System: infrastructure that continuously acquires, organizes, maintains, assembles, and delivers engineering context — about a codebase, its history, its decisions, and its surrounding artefacts — to any human, AI system, or engineering tool that needs it.

Context is the product. Ferret does not reason over the context it delivers, does not generate engineering artefacts, and does not decide what should be built. It makes sure that whoever or whatever does those things is working from a complete, current, and trustworthy picture of the engineering reality, instead of from whatever fragment happens to be within reach.

### 1.2 Goals

- **G1 — Completeness of context.** No engineering-relevant knowledge that exists in an accessible source should be invisible to a consumer querying Ferret.
- **G2 — Currency of context.** What Ferret delivers should reflect the current state of its sources, with staleness bounded and observable, not silent.
- **G3 — Consumer neutrality.** Ferret must be equally useful to a human, an AI agent, or a conventional engineering tool, without favoring one consumer's shape of interaction over another's.
- **G4 — Trustworthy context.** Every unit of context Ferret delivers must carry enough provenance that a consumer can judge how much to trust it, without Ferret itself asserting that the context is correct.
- **G5 — Extensible acquisition and delivery.** New sources of context and new consumers of context must be addable without redesigning the capabilities in between.
- **G6 — Operable at repository scale and beyond.** The capability model must hold whether context spans one repository or many, without a different product being required at each scale.

### 1.3 Non-Goals

- Ferret does not reason about, generate, or evaluate engineering artefacts (code, specifications, reviews, architecture proposals). That is the job of consumers such as AEF.
- Ferret does not enforce an engineering process (specification-first development, mandatory human review, approval workflows). It may *expose* the state of such a process if that state is itself context, but it does not own or enforce the process.
- Ferret does not execute tasks, take autonomous action, or modify the systems it observes. It is a read-and-organize system with respect to its sources; it is a serve system with respect to its consumers.
- Ferret does not replace the systems it draws context from — version control, issue trackers, documentation platforms, communication tools. It observes and organizes what they contain; it does not become them.
- Ferret does not compete with or embed the reasoning capability of the AI systems that consume its context. It is infrastructure beneath those systems, not a peer to them.

### 1.4 Product Principles

- **P1 — Context over computation.** Where a decision must be made between improving what Ferret knows and improving what Ferret concludes, Ferret improves what it knows. Conclusions belong to consumers.
- **P2 — Provenance is mandatory, not optional.** Context without a traceable origin is not a deliverable — it is noise. Every capability that produces or transforms context must preserve or attach its lineage.
- **P3 — Freshness is a first-class property, not an assumption.** Context has an age. Ferret must be able to state how current any piece of context is, and must never present stale context as current.
- **P4 — No privileged consumer.** Ferret's capabilities are defined by what they do, not by which consumer happens to be asking. A human, an AI agent, and a CI pipeline are equally valid consumers of the same capability.
- **P5 — Degrade by scope, not by silent omission.** When a source is unavailable, unindexed, or out of scope, Ferret must be able to say so. It must never present a partial picture as though it were complete.
- **P6 — Boundaries are capability boundaries, not team boundaries.** The capability model in this document is defined by responsibility, independent of how (or whether) it is eventually staffed, sequenced, or built.

---

## 2. Capability Model

Each capability is defined by its responsibility and its boundary — what it owns and what it explicitly does not. No capability implies a technology, a data structure, or an interface shape.

### 2.1 Workspace Definition

**Responsibility.** Establishes what a "workspace" is: the boundary of a coherent body of engineering context (typically, but not necessarily, a single repository), its identity, and its configuration.

**Boundary.** Owns the concept and identity of a workspace and its declared scope. Does not itself acquire, store, or interpret any content within that scope — that is Context Acquisition's job.

### 2.2 Context Acquisition

**Responsibility.** Discovers and reads engineering-relevant content from sources within a workspace's declared scope — code, documents, history, decisions, conversations, and any other source a workspace is configured to include.

**Boundary.** Owns discovery and reading. Does not interpret, structure, or judge the relevance of what it reads — that is Context Organization's job. Does not decide what "counts" as in-scope beyond what Workspace Definition has declared.

### 2.3 Context Organization

**Responsibility.** Structures acquired content into a form that can be queried, related, and reasoned about at the context layer — extracting entities, relationships, and structure from raw acquired material.

**Boundary.** Owns structuring and relating. Does not decide when structure needs to be refreshed — that is Context Maintenance's job. Does not decide which structured context to surface for a given request — that is Context Assembly's job.

### 2.4 Context Maintenance

**Responsibility.** Keeps organized context current as sources change: detecting change, invalidating what is stale, triggering re-acquisition and re-organization, and tracking the freshness of every unit of context.

**Boundary.** Owns change detection, invalidation, and freshness accounting. Does not itself re-read sources (delegates to Context Acquisition) or re-structure content (delegates to Context Organization). Does not decide what to deliver — only what is current enough to be eligible for delivery.

### 2.5 Context Assembly

**Responsibility.** Composes the specific, relevant, appropriately-scoped body of context that answers a given request — selecting, ranking, relating, and compressing organized context to fit a consumer's need and constraints.

**Boundary.** Owns selection, ranking, and composition for a specific request. Does not acquire or structure new context (consumes what Organization and Maintenance have already produced). Does not decide how the result is transported to the consumer — that is Context Delivery's job.

### 2.6 Context Delivery

**Responsibility.** Makes assembled context available to a requesting consumer, in a form and through a surface appropriate to that consumer, without changing the substance of what was assembled.

**Boundary.** Owns the presentation and hand-off of context to a consumer. Does not decide what context to include — that is Context Assembly's job. Does not reason about or act on the context after delivery — that is entirely the consumer's concern.

### 2.7 Provenance & Attribution

**Responsibility.** Records, for every unit of context, where it came from, when it was acquired or last confirmed current, and what transformations (organization, assembly) it passed through.

**Boundary.** Owns the record of lineage. Does not judge correctness or quality of the underlying content — only tracks its origin and history. Cuts across Acquisition, Organization, Maintenance, and Assembly rather than sitting at one point in the pipeline.

### 2.8 Access Control & Policy

**Responsibility.** Governs which consumers may access which context, consistent with the permissions and policies declared for a workspace and its sources.

**Boundary.** Owns the decision of whether a given request is permitted. Does not own identity itself (see External Systems, §6) — it consumes identity asserted by external systems. Does not own the content being protected.

### 2.9 Extensibility

**Responsibility.** Allows new kinds of sources (for Acquisition), new kinds of structure (for Organization), and new kinds of consumers or delivery surfaces (for Delivery) to be added without altering the capabilities themselves.

**Boundary.** Owns the extension points between capabilities. Does not itself acquire, organize, or deliver anything — it defines where those capabilities may be extended.

### 2.10 Observability & Health

**Responsibility.** Makes the internal state of every other capability inspectable: what has been acquired, how current it is, what has been organized, what has been delivered, and where any capability is degraded or failing.

**Boundary.** Owns visibility into the system's own state. Does not own the context itself, and does not take corrective action — it reports, it does not remediate.

### 2.11 Federation

**Responsibility.** Extends the capability model across multiple workspaces — enabling context to be organized, assembled, and delivered across workspace boundaries when a consumer's need spans more than one.

**Boundary.** Owns cross-workspace composition. Depends on every other capability already functioning correctly within each individual workspace; does not duplicate any of them.

---

## 3. Capability Hierarchy

```
Foundation
├── Workspace Definition

Context Supply Chain (the core product)
├── Context Acquisition
├── Context Organization
├── Context Maintenance
├── Context Assembly
└── Context Delivery

Trust Capabilities (cross-cutting over the supply chain)
├── Provenance & Attribution
└── Access Control & Policy

Platform Capabilities (structural, enabling the rest to evolve)
├── Extensibility
└── Observability & Health

Scale Capability (extends the supply chain, not a peer to it)
└── Federation
```

---

## 4. Capability Dependencies

```
Workspace Definition
        │
        ▼
Context Acquisition ──────► Context Organization ──────► Context Maintenance
        │                           │                            │
        │                           ▼                            │
        │                   Context Assembly ◄───────────────────┘
        │                           │
        │                           ▼
        │                   Context Delivery
        │                           │
        ▼                           ▼
   Provenance & Attribution (attaches to every stage: Acquisition → Delivery)
                                    │
                                    ▼
                          Access Control & Policy (gates Delivery; informed by Workspace Definition)

Extensibility        — attaches to Acquisition (new sources) and Delivery (new consumers)
Observability & Health — reads state from every capability; depends on none of them functionally
Federation            — depends on a complete Context Supply Chain existing per workspace
```

**Reading the dependencies:**

- Nothing functions without **Workspace Definition** — it establishes what is in scope before anything can be acquired.
- The Context Supply Chain is a strict pipeline for how context comes to exist (Acquisition → Organization → Maintenance) and a demand-driven pipeline for how it goes back out (Assembly → Delivery). Maintenance feeds back into Assembly's eligibility, not into Delivery directly.
- **Provenance & Attribution** and **Access Control & Policy** are not pipeline stages; they are obligations that every pipeline stage must honor. A capability that cannot report provenance, or that bypasses access control, is not a conforming implementation of that capability.
- **Extensibility** does not depend on the supply chain; the supply chain depends on Extensibility having defined where new sources and consumers may attach.
- **Observability & Health** depends on every capability exposing state, but no capability depends on it — it is diagnostic, not operational.
- **Federation** is the only capability that depends on the entire rest of the model already being satisfied; it is a multiplier over the model, not an addition to it.

---

## 5. Product Boundaries

### 5.1 Inside Ferret

- Defining and configuring workspace scope.
- Discovering and reading engineering-relevant sources within that scope.
- Structuring raw content into related, queryable context.
- Detecting change and keeping context current.
- Selecting, ranking, and composing context in response to a request.
- Delivering assembled context to a consumer.
- Recording and exposing provenance for all of the above.
- Enforcing access policy over what is delivered to whom.
- Providing extension points for new sources and new consumers.
- Reporting on its own health and the state of its context.
- Composing context across more than one workspace.

### 5.2 Outside Ferret

- Reasoning over context to produce conclusions, plans, code, or recommendations.
- Generating engineering artefacts of any kind.
- Enforcing an engineering methodology (specification-first workflows, mandatory review gates, approval chains) as a process owner.
- Executing changes to source systems (writing code, closing issues, modifying documents).
- Acting as the system of record for anything it observes (code, issues, decisions) — it observes and organizes; it does not own.
- Hosting or providing AI model inference.
- Establishing user or system identity — it consumes identity, it does not issue it.
- Serving as an IDE, build system, CI/CD platform, or project management tool.

### 5.3 The Boundary Test

A candidate capability belongs inside Ferret if removing it would leave a consumer with less complete, less current, less trustworthy, or less accessible *context*. It belongs outside if removing it would only leave a consumer with less *conclusion, action, or process enforcement*. When a proposed capability produces something a consumer could disagree with on grounds other than "this doesn't reflect the source," it is outside Ferret.

---

## 6. External Systems

Ferret's product architecture assumes interaction with the following categories of external system. Only the nature of the interaction is described; no protocol, technology, or integration mechanism is specified here.

| External System Category | Direction | Nature of Interaction |
|---|---|---|
| **Source systems** (version control, issue trackers, documentation platforms, communication/chat archives, file storage, build/CI history) | Ferret reads from | Context Acquisition observes these systems as sources of engineering-relevant content. Ferret does not write back to them. |
| **Consumer systems** (AI agents including AEF, human interfaces, conventional engineering tools) | Ferret is queried by | Consumers request context through Context Delivery and receive assembled, provenance-carrying results. Ferret does not initiate action toward consumers beyond responding to their requests, except where a consumer has explicitly subscribed to be notified of change. |
| **Identity & access systems** | Ferret consumes assertions from | Access Control & Policy relies on identity and permission assertions issued elsewhere; Ferret does not itself establish who a user or system is. |
| **Change-notification sources** | Ferret is notified by, or polls | Context Maintenance depends on learning that a source has changed, whether through the source pushing a notification or Ferret checking for change. Either interaction shape is permitted by this architecture; the choice is a future implementation decision, not a product-architecture one. |
| **Observability sinks** (any external system a deployer chooses to route Ferret's own health/state signals to) | Ferret sends to (optional) | Observability & Health may surface its findings externally; this is described as a capability, not a specific integration. |

---

## 7. Versioning Strategy

Capabilities are grouped into product generations. A generation is a coherent, independently valuable state of the product — each one must stand as something a consumer could actually use, not a pile of partial infrastructure.

| Generation | Theme | Capabilities Reaching Usable Maturity |
|---|---|---|
| **Generation 0 — Single-Source Foundation** | Prove the pipeline shape | Workspace Definition; Context Acquisition (single source category); Context Organization (basic structuring); Context Delivery (single consumer shape) |
| **Generation 1 — Context OS Core** | The product this document is defining | Context Acquisition (multiple source categories); Context Organization (full structuring); Context Maintenance; Context Assembly; Context Delivery (consumer-neutral); Observability & Health |
| **Generation 2 — Trust** | Make context defensible, not just available | Provenance & Attribution (complete, mandatory); Access Control & Policy |
| **Generation 3 — Scale** | Extend beyond one workspace | Federation |
| **Generation 4 — Ecosystem** | Open the boundaries | Extensibility matured into a genuine third-party extension surface for both sources and consumers |

Generations are additive: no capability introduced in an earlier generation is removed or narrowed by a later one. A generation is not a time-boxed release; it is a maturity threshold that Ferret's execution track (post-AEF-GA) will sequence against.

---

## 8. Risks

| Risk | Description | Consequence if Unmanaged |
|---|---|---|
| **Scope regression toward AI Workspace OS** | Ferret's own prior history (existing PRD-001, Mission, Principles, and the FUTURE-002 exploration) already drifted toward embedding agent runtime and review governance. The discipline required to keep Ferret as pure context infrastructure is not free — it must be actively maintained. | Ferret re-absorbs reasoning/generation responsibility, recreating exactly the coupling this architecture is designed to avoid, and duplicating whatever AEF is building. |
| **Designing ahead of the consumer's real contract** | This architecture is written before AEF's actual context requirements are finalized, because AEF is independently under development. | Capabilities may be shaped around assumptions about what AEF needs that turn out to be wrong, requiring rework once AEF's real interface requirements are known. |
| **Provenance and trust capability is easy to underspecify** | "Attach provenance to everything" is simple to state and historically hard to sustain once acquisition sources multiply and organization performs multiple transformation steps. | Consumers receive context they cannot actually evaluate for trust, undermining Product Principle P2 without an obvious point of failure to diagnose. |
| **Two concurrent product narratives** | The historical documents (Vision, Mission, Principles, PRD-001) describe a broader Ferret than this document does, and this document does not reconcile them. | Future readers of the repository encounter two different, unreconciled statements of what Ferret is, with no governance record explaining the relationship, unless this is explicitly resolved (see §9). |
| **Organization/Assembly boundary blur** | The line between "structure context" (Organization) and "select the right context for this request" (Assembly) is a real distinction on paper but easy to blur once a pipeline is under construction — pre-computed structuring can quietly start encoding request-specific assumptions. | Assembly loses its generality; context gets pre-shaped for one class of consumer, violating Product Principle P4 (no privileged consumer). |
| **Unbounded acquisition surface** | "Any engineering-relevant source" is an open-ended category (code, issues, chat, documents, calendars, build logs, and more). Nothing in this architecture caps what counts as in-scope. | Acquisition scope grows without a corresponding way to prioritize, leaving Organization and Assembly perpetually behind an ever-widening intake. |
| **Federation treated as an afterthought** | Federation is described as depending on the full capability model already working per-workspace, but multi-workspace concerns (identity across workspaces, cross-workspace access policy) can leak backward into how earlier capabilities must be designed. | Retrofitting Federation later forces changes to capabilities assumed stable in Generations 0–2. |

---

## 9. Open Questions

1. **What is AEF's actual context contract?** This architecture defines Context Delivery generically, consumer-neutral. Once AEF's real requirements are known, does Ferret's Delivery capability need to be shaped with AEF as a privileged first consumer in practice, even while remaining neutral in principle?

2. **What is the relationship between FEP and the existing product documents?** Vision-001, Mission-001, Principles-001, and PRD-001 describe a broader Ferret (including agent runtime and specification/review governance) than this document does. Is that scope permanently moving to AEF, staying latent in Ferret for a future generation, or does it need an explicit governance decision (an ADR-style record) reconciling the two narratives?

3. **What happens to the specification-driven development and human-review principles?** These are structural principles in the current Principles-001 document. Under this architecture, they are out of scope for Ferret. Does AEF own them entirely, or does Ferret need a future capability that exposes "process state" (e.g., "is there a specification for this," "was this reviewed") as context, without owning the process itself?

4. **Is prior shipped capability (Sprints 0–13 and beyond, per PROJECT-STATE.md) an asset FEP builds from, or does FEP plan from a blank slate?** This document was written without assuming any existing implementation, per the constraint against implementation decisions — but the eventual execution track will need to decide whether existing code is a starting point or prior art to be evaluated fresh.

5. **How far does Federation extend in practice?** Is cross-workspace context composition scoped to "workspaces owned by the same organization," or does the architecture need to anticipate composing context across organizational boundaries (e.g., a shared open-source dependency's workspace)?

6. **Where does the boundary of "engineering-relevant source" actually stop?** Section 6 lists source categories but the product vision's completeness goal (G1) has no natural ceiling. Does a future prompt need to define an explicit, bounded source taxonomy, or does this stay deliberately open-ended?

7. **What governance process applies to FEP itself?** FEP has its own `decisions/` and `reviews/` folders, distinct from `docs/adr/` and `docs/Reviews/`. Does FEP decision-making require the same authority/veto model as the existing `.ai/` agent charter, or does it need its own, lighter-weight process appropriate to a pre-implementation planning track?

8. **What triggers the transition from FEP planning to AEF-consuming execution?** "AEF reaches GA" is stated as the gate in the originating prompt, but this document does not define what evidence establishes that gate has been met, or who makes that determination.

---

## Cross References

| Document | Relationship |
|---|---|
| [FEP-000-Roadmap.md](FEP-000-Roadmap.md) | Program roadmap this document is the first entry in |
| [docs/000-Overview/Vision.md](../000-Overview/Vision.md) | Historical vision document — broader scope, not reconciled with this document (see §9.2) |
| [docs/000-Overview/Mission.md](../000-Overview/Mission.md) | Historical mission document — not reconciled with this document (see §9.2) |
| [docs/000-Overview/Principles.md](../000-Overview/Principles.md) | Historical engineering principles — not reconciled with this document (see §9.3) |
| [docs/001-Product/PRD-001.md](../001-Product/PRD-001.md) | Historical product requirements — not reconciled with this document (see §9.2) |
| [docs/002-Architecture/FUTURE-002-Enterprise-Intelligence-Vision.md](../002-Architecture/FUTURE-002-Enterprise-Intelligence-Vision.md) | Prior exploration of embedding AI into Ferret — informs Risk 1 (§8) |
| [docs/000-Overview/PROJECT-STATE.md](../000-Overview/PROJECT-STATE.md) | Current shipped-capability record — relevant to Open Question 4 (§9) |

---

## Revision History

| Version | Date | Summary |
|---|---|---|
| 1.0 | 2026-07-08 | Initial Product Architecture — FEP Prompt 1 output |
