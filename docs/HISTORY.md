# Ferret Project History

This document records the narrative history of the Ferret project: founding intent, major pivots, name changes, architectural evolution, and the context behind key decisions.

---

## Origins (Pre-Sprint 0, 2026)

Ferret began as an internal tool to address a recurring pain point on engineering teams: **context loss**. In a typical codebase of 100+ contributors, the knowledge needed to make a good decision is scattered across pull requests, JIRA tickets, Confluence docs, Slack threads, architecture diagrams, and the memory of people who have since left. AI coding assistants were improving rapidly but they had a fundamental gap: they could generate code but they had no memory of your project's specific history, decisions, or conventions.

The initial concept was an "AI workspace" — a tool that would maintain a persistent, structured knowledge layer about your codebase, automatically updated as the repository evolved. The working name was **AISpace** (AI + workspace).

The earliest design sessions established two non-negotiable constraints:
1. All state must live in the repository (`.ferret/` directory), not in a cloud service.
2. The core must be zero-dependency so it can run in air-gapped environments.

---

## Sprint 0 — January 2026: Foundation

Sprint 0 was pure scaffolding: git repository, .NET 9 solution skeleton, StyleCop, xUnit, CI pipeline bootstrap, and the `Directory.Build.props` convention that enforces consistent compilation settings across all 17 projects. No business logic. The goal was a green build from day one.

Key outcome: the multi-project structure (`src/` + `tests/`) that has remained unchanged through Sprint 6.

---

## Sprints 1–3 — February–March 2026: Core Kernel

These sprints built `Ferret.Core` — the permanent foundation. The principle was radical constraint: **Core has no external dependencies**. Every type in Core is a contract, a value object, or an exception. No infrastructure, no I/O, no DI framework.

This was a deliberate choice made after evaluating `Scrutor` (DI scanning) and `MediatR` (mediator pattern) — both were rejected because they would make Core testable only with their frameworks present.

The event system (`Ferret.Events`) was designed in Sprint 3 with a three-tier event taxonomy: Domain events (workspace state changes), Integration events (cross-module), System events (lifecycle). `System.Threading.Channels` was evaluated for the internal event bus and deferred — the simpler in-process bus was sufficient for M1.

---

## Sprint 4 — April 2026: Architecture Documentation Baseline

Sprint 4 produced the architecture document stack (ARCH-001 through ARCH-014) and locked in the public contracts for Runtime and Workspace before any implementation. This was a deliberate sequence: write the interfaces first, make them the design gate, then implement.

`IWorkspaceEngine`, `IWorkspaceLocator`, `IWorkspaceStateStore`, and the full workspace value-object set were defined here and have not changed since. Sprint 4 ended with 119 passing tests and the `v0.4.0-sprint4` tag.

The workspace directory was originally designed as `.ai/` but was updated to `.ferret/` ahead of the Sprint 5 rebrand.

---

## Sprint 5 — May 2026: Runtime Host + Product Rebrand

Sprint 5 delivered the Runtime Host implementation: `Ferret.Runtime` wrapping `Microsoft.Extensions.Hosting` internally. The wrapping was intentional — the `IHost` abstraction was too heavyweight to expose through `Ferret.Core` contracts.

Midway through Sprint 5, the team decided the "AISpace" working title had outlived its usefulness. The product now had a coherent identity: it was a tool that finds things and surfaces context, the way a ferret finds things in a burrow. The rename was applied in a single atomic commit, touching 264 files.

**Version tags at this milestone:**
- `v0.5.0-sprint5` — last tag under the AISpace name
- `v0.5.0-ferret` — first tag under the Ferret name (same codebase, rebrand applied)

See ADR-0005 for the full rebrand decision record.

---

## Sprint 6 — June 2026: Platform Entry Point & CLI

Sprint 6 completed Milestone 1 (Platform Foundation). The CLI entry point, `System.CommandLine` integration, `ICliModule` pattern, `ferret doctor`, `ferret status` (not-running stub), and `ferret --version` were all shipped.

At the end of Sprint 6: **245 tests passing**. The ADR-0012 Platform Foundation Freeze was declared, locking six packages as stable contracts. Sprint 7 onwards is product-building on a stable foundation.

**Tag:** `v0.6.0-sprint6`

---

## Sprint 7 — June 2026 (planned): Workspace Engine

Sprint 7 is the first product sprint after M1: it answers "what can a user do today they couldn't yesterday?" The answer is `ferret workspace init` — create a `.ferret/` workspace that is the long-term foundation for ContextOS.

Sprint 7 also introduces the connector contract architecture (`IConnector`, `ConnectorType`, `ConnectorMetadata`, `ConnectorCapabilities`, `ConnectorHealth`) — contracts only, no implementation. Sprint 8 delivers the first real connector: `FilesystemConnector`.

The `.ferret/` directory created in Sprint 7 is not just a folder structure — it is the skeleton of ContextOS: connectors, indexes, memory, knowledge graph, models, snapshots, telemetry. The workspace feels like an operating system for context, not a folder.

---

## The ContextOS Vision

"ContextOS" emerged as the name for the technology platform that Ferret is built on. It captures the product's long-term ambition: not just indexing a codebase, but maintaining a living model of an enterprise's engineering knowledge — decisions, history, relationships, anomalies, patterns.

The roadmap V3 entries (Enterprise Time Machine, Root Cause Intelligence, Architecture Intelligence) are all expressions of ContextOS applied to specific enterprise problems. The workspace engine (Sprint 7) is the first concrete step toward this vision.

---

## Name Reference

| Era | Product Name | Platform Name | CLI Binary | Namespace |
|---|---|---|---|---|
| Pre-Sprint 5 | AISpace | (none) | `aispace` | `AISpace.*` |
| Sprint 5 onward | Ferret | ContextOS | `ferret` | `Ferret.*` |
