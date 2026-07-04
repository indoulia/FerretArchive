# 002 — Architecture

System design documents for Ferret.

---

## Index

| Document | ID | Description | Status |
|---|---|---|---|
| [ARCH-TEMPLATE-001 — Architecture Document Standard](ARCH-TEMPLATE-001.md) | ARCH-TEMPLATE-001 | Required sections, metadata, diagram conventions, review checklist, quality gates | Accepted |
| [ARCH-001 — Overall System Architecture](ARCH-001.md) | ARCH-001 | Platform architecture: layers, modules, dependency rules, domain view, fitness functions, capability matrix | Draft |
| ARCH-002 — Ferret.Core Architecture | ARCH-002 | Core module: interfaces, value objects, domain events, extension points | Planned |
| [ARCH-003 — Workspace Architecture](ARCH-003.md) | ARCH-003 | Workspace Engine: components, data flows, configuration reference, error handling | Draft |
| ARCH-004 — Knowledge Architecture | ARCH-004 | Knowledge Engine: graph model, query model, context assembly internals | Planned |
| ARCH-005 — Index Architecture | ARCH-005 | Index Engine: pipeline, change detection, atomicity, parser dispatch | Planned |
| ARCH-006 — Memory Architecture | ARCH-006 | Memory Engine: session state, repository memory, working sets | Planned |
| ARCH-007 — Plugin Architecture | ARCH-007 | Plugin Host: lifecycle, isolation, permissions, SDK, registry | Planned |
| ARCH-008 — Review & Specification Architecture | ARCH-008 | Review Engine + Specification Engine: lifecycle, finding model, approval gates | Planned |
| ARCH-009 — CLI Architecture | ARCH-009 | CLI: command hierarchy, output formats, exit codes, shell completion | Planned |
| ARCH-010 — MCP Architecture | ARCH-010 | MCP Server + Client: tools, resources, transports, protocol versioning | Planned |
| [ARCH-011 — Configuration Architecture](ARCH-011.md) | ARCH-011 | Configuration: sources, precedence, schema, secret resolution, validation | Draft |
| ARCH-012 — Security Architecture | ARCH-012 | Security model: trust boundaries, plugin sandbox, audit, sensitive data | Planned |
| [ARCH-013 — Event Architecture](ARCH-013.md) | ARCH-013 | Domain events: full catalogue, schemas, delivery model, publisher/consumer map | Draft |
| [ARCH-014 — Platform Error Model](ARCH-014.md) | ARCH-014 | Exception hierarchy, error codes, propagation rules | Draft |
| ARCH-015 — Telemetry Architecture | ARCH-015 | Structured logging, distributed tracing, metrics, exporters | Planned |
| [Overview](overview.md) | — | Placeholder — superseded by ARCH-001 | Superseded |
| [ARCH-023 — Ferret V2 Architectural Boundary](ARCH-023-V2-Architectural-Boundary.md) | ARCH-023 | Foundation document for V2: scope, non-goals, principles, and V1 component dependencies | Frozen (AGR-001) |
| [ARCH-024 — Ferret Artifact Inventory](ARCH-024-Artifact-Inventory.md) | ARCH-024 | Canonical inventory of every artifact Ferret produces today, traced to real code; identifies gaps vs. ARCH-001 | Frozen (AGR-001) |
| [ARCH-025 — Ferret V2 Artifact Validity Model](ARCH-025-Artifact-Validity-Model.md) | ARCH-025 | Defines when an artifact is valid and reuse-eligible: validity classes, dependency types, invalidation sources, minimum-invalidation principles | Frozen (AGR-001) |
| [ARCH-026 — Ferret V2 Persistence Requirements](ARCH-026-Persistence-Requirements.md) | ARCH-026 | Defines what dependency state must survive process termination for the validity model to be evaluated deterministically; requirements only, no mechanism | Frozen (AGR-001) |
| [ARCH-027 — Ferret V2 Dependency Resolution Architecture](ARCH-027-Dependency-Resolution-Architecture.md) | ARCH-027 | Defines how an engine determines whether an existing artifact satisfies a request without recomputation; resolution, not retrieval | Frozen (AGR-001) |
| [V2-ROADMAP-001 — Ferret V2 Architecture Program Roadmap](V2-ROADMAP-001-Architecture-Program.md) | V2-ROADMAP-001 | Sequences remaining V2 work (deferred questions → architecture refinements → mechanism design → implementation) by architectural dependency | Active |
| [ARCH-028 — Ferret V2 Request Equivalence Architecture](ARCH-028-Request-Equivalence-Architecture.md) | ARCH-028 | Resolves AGR-001 F5: defines request identity and equivalence; amends ARCH-025 §3 and ARCH-027 §4 | Frozen (AGR-002) |
| [ARCH-029 — Ferret V2 Validity Propagation Architecture](ARCH-029-Validity-Propagation-Architecture.md) | ARCH-029 | Resolves AGR-001 F7: reframes propagation as temporal consistency, not scheduling; amends ARCH-025 §5 and ARCH-027 §3 | Frozen (AGR-003) |
| [ARCH-030 — Ferret V2 Dependency Participation Semantics](ARCH-030-Dependency-Participation-Semantics.md) | ARCH-030 | Resolves AGR-001 F6 and F9 (batched): deletion semantics and the canonical Validity-Class × Dependency-Shape matrix; amends ARCH-025 §3 and §4 | Frozen (AGR-004) |
| [ARCH-031 — Ferret V2 Mechanism Architecture Principles](ARCH-031-Mechanism-Architecture-Principles.md) | ARCH-031 | Bridge between the frozen V2 Foundation and Tier 3 mechanism-level design: what a mechanism architecture is, which guarantees it may never weaken, and what evidence it must provide before approval | Draft — pending governance review |
| [ARCH-032 — Ferret V2 Persistence Mechanism Design](ARCH-032-Persistence-Mechanism-Design.md) | ARCH-032 | V2-ROADMAP-001 RM-07: realizes ARCH-026's persistence requirements at the mechanism tier — responsibilities, inputs, lifecycle, guarantees, and the boundary with resolution (RM-08) | Draft — pending Standard Architecture Review |
| [ARCH-033 — Ferret V2 Dependency Resolution Mechanism Design](ARCH-033-Dependency-Resolution-Mechanism-Design.md) | ARCH-033 | V2-ROADMAP-001 RM-08: realizes ARCH-027/028/029's resolution, equivalence, and propagation model at the mechanism tier, consuming ARCH-032 | Draft — pending Standard Architecture Review |
| [ARCH-034 — Ferret V2 Surface Integration Mechanism Design](ARCH-034-Surface-Integration-Mechanism-Design.md) | ARCH-034 | V2-ROADMAP-001 RM-09: what must remain true of the existing CLI/MCP surface (ARCH-024 §7) when it exposes a resolution-confirmed reuse; defines no API | Draft — pending Standard Architecture Review |
| [ARCH-035 — Ferret V2 Mechanism Interaction Model](ARCH-035-Mechanism-Interaction-Model.md) | ARCH-035 | Composes ARCH-032/033/034 into one verified request-to-surface sequence with no responsibility gap or overlap; not a roadmap item | Draft — pending Standard Architecture Review |
| [ARCH-036 — Ferret V2 Mechanism Validation and Conformance](ARCH-036-Mechanism-Validation-and-Conformance.md) | ARCH-036 | Extends ARCH-031's evidentiary standard to implementations of ARCH-032/033/034; distinguishes conformance from benchmarking (RM-06); not a roadmap item | Draft — pending Standard Architecture Review |
| [V2-IMPLEMENTATION-BACKLOG-001 — Ferret V2 Implementation Backlog](V2-IMPLEMENTATION-BACKLOG-001.md) | V2-IMPLEMENTATION-BACKLOG-001 | Delivery-side counterpart to V2-ROADMAP-001, per ADR-0021: epics/features/tasks mapped to ARCH-032–036 and their ADR dependencies | Active |

---

## Design Principles

1. **Modularity** — single-responsibility components with well-defined interfaces
2. **Extensibility** — new capabilities added via plugins, not by modifying core
3. **Observability** — structured logging, tracing, and metrics built in from day one
4. **Security by default** — no component exposes an insecure default
5. **Testability** — all logic unit-testable without running external services

---

## C4 Model Levels

| Level | Scope | Status |
|---|---|---|
| C1 — System Context | Ferret vs external actors | Draft (in overview.md) |
| C2 — Container | Deployable units | Draft (in overview.md) |
| C3 — Component | Internal structure per container | Planned — Sprint 1+ |
| C4 — Code | Class diagrams | Generated — Sprint 1+ |

---

## Template

Use [docs/templates/architecture.md](../templates/architecture.md) for new architecture documents.
