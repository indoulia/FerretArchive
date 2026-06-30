# Ferret — Glossary

| Field | Value |
|---|---|
| **Document ID** | GLOSSARY-001 |
| **Version** | 1.0 |
| **Status** | Accepted |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-27 |

---

## Purpose

This document defines the canonical vocabulary of the Ferret project. Every term used in specifications, architecture documents, ADRs, API contracts, and source code comments draws its meaning from this glossary. When a term is used in a different sense in a specific document, that document must note the deviation explicitly.

---

## Scope

This glossary covers terms specific to the Ferret platform and the engineering practices it encodes. General programming terms (function, class, interface) are not defined here unless they carry platform-specific meaning. Terms derived from external standards (MCP, SemVer, HTTP) are defined here only to clarify how Ferret uses them, not to restate the standard.

---

## How to Use This Glossary

Terms are listed alphabetically. Cross-references to related terms are noted with → followed by the term name. When a term has a precise technical meaning in addition to its common usage, the technical meaning is stated first.

---

## A

### ADR
**Architecture Decision Record.** A short document that records a significant architectural decision, its context, the alternatives considered, and its consequences. ADRs are stored in `docs/adr/`, numbered sequentially, and never deleted — superseded ADRs are marked as such and link to the successor.

*See also:* → Architecture Review, → Specification, → Governance

### Artifact
A file or document produced by the platform or by an AI-assisted workflow, whether generated, reviewed, or committed. Artifacts include: generated specifications, review documents, index files, session records, and AI interaction logs. Every artifact is attributed — it has a known origin, a creation timestamp, and (if committed) a review record.

*See also:* → Traceability, → Session, → Review

### Architecture Review
A formal evaluation of an architectural design or decision, conducted before or after implementation. Architecture Reviews are recorded in `docs/Reviews/` using the `AR-` identifier prefix. An Architecture Review produces a set of findings categorised by severity (Critical, High, Medium, Low, Observation) and a resolution record for each finding.

*See also:* → ADR, → Review

---

## C

### CLI
**Command-Line Interface.** The `Ferret` executable — the primary interactive interface through which developers invoke platform operations. The CLI wraps the platform's runtime capabilities as commands with consistent argument conventions, output formats, and exit codes. The CLI is the reference implementation of the platform's user-facing behaviour.

*See also:* → Engine, → Tool

### Component
A cohesive unit of responsibility within a Module. A Component corresponds to a namespace or set of related classes within a package. Components interact with other Components through their public interfaces. In C4 model terms, a Component corresponds to a C3-level element.

*See also:* → Module, → Package

### Context
The information made available to an AI model for a specific interaction. Context is assembled by the platform from the Knowledge index, the current Session, relevant Specifications, and the specific input provided by the user. Context is bounded in size (measured in tokens) and is always derived from repository-local sources. The platform assembles context deterministically for a given input and knowledge state.

*See also:* → Knowledge, → Session, → Index

---

## E

### Engine
The core execution subsystem of the Ferret runtime. The Engine coordinates the Agent Runtime, the Knowledge layer, the Plugin Host, and the Model Provider to execute AI-assisted workflows. The Engine is the internal name for the orchestration layer; external interfaces refer to it through the runtime API.

*See also:* → Plugin, → Provider

### Engineering Constitution
The set of documents that define the foundational commitments of the Ferret project: the Vision, the Mission, and the Engineering Principles. The Engineering Constitution is not a legal document; it is an engineering one. It defines the constraints within which all architectural and product decisions must operate. Changes to the Engineering Constitution require an ADR and a vote by the current maintainer group.

*See also:* → Vision, → Mission, → Principles (PRINCIPLES-001)

### Extension
A general term for any code that extends the platform through a published extension point without modifying the core. Extensions include Plugins and any other contribution that conforms to a platform-defined interface. All Extensions declare their dependencies and permissions at activation time.

*See also:* → Plugin, → Provider

---

## G

### Governance
The set of processes, policies, and documents that define how the Ferret project makes decisions, accepts contributions, manages releases, and resolves disputes. Governance documents are stored in `docs/013-Governance/` and at the repository root (CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md). Governance applies to the project itself; it does not prescribe the governance model of projects that use the platform.

---

## I

### Index
The structured data store maintained by the Indexer that represents the platform's knowledge of a repository. The Index maps source symbols, documents, decisions, and their relationships into a queryable form. The Index is stored in the repository under `.ai/` and is updated incrementally by the Indexer. The Index is the primary source from which Context is assembled.

*See also:* → Indexer, → Knowledge, → Context

### Indexer
The platform component responsible for building and maintaining the Index. The Indexer processes source files, documents, and structured data from the repository, extracts symbols and relationships through Parsers, and writes the results into the Index. The Indexer operates incrementally — it processes only files that have changed since the last run.

*See also:* → Index, → Parser

---

## K

### Knowledge
The structured, persistent understanding that the platform maintains about a repository. Knowledge encompasses: source symbols and their relationships, document content and metadata, specification and requirement identifiers, ADR decisions and their status, and the history of AI interactions with the repository. Knowledge is distinct from Context: Knowledge is the full persistent store; Context is a bounded selection of Knowledge assembled for a specific interaction.

*See also:* → Index, → Context, → Session

---

## M

### MCP
**Model Context Protocol.** An open protocol that defines a standard interface through which AI models and hosts exchange context, tool calls, and structured results. Ferret implements the MCP Client role (consuming context from MCP Servers) and the MCP Server role (exposing platform Knowledge to MCP-compatible hosts). MCP transport, capability negotiation, and message formats follow the published MCP specification.

*See also:* → Tool, → Provider

### Module
A top-level unit of decomposition in the Ferret codebase. A Module corresponds to a deployable .NET project (a `.csproj` file and the package it produces). Modules interact only through their published package interfaces. The planned Modules are: `Ferret.Core`, `Ferret.Runtime`, `Ferret.Mcp`, `Ferret.Plugins`, `Ferret.Api`, `Ferret.Cli`.

*See also:* → Package, → Component

---

## P

### Package
The distributable form of a Module — a NuGet package in the case of .NET Modules. Packages are versioned following the project's versioning policy (SemVer). A Package's public API surface is subject to compatibility commitments once it reaches stable status.

*See also:* → Module

### Parser
A component invoked by the Indexer to extract structured information from a specific file type. A Parser for C# files extracts symbols, references, and documentation. A Parser for Markdown files extracts headings, links, and metadata. Parsers are implemented as plugins; the core platform defines the `IParser` interface and the data structures that Parsers populate in the Index.

*See also:* → Indexer, → Plugin

### Plugin
A self-contained unit of functionality that extends the platform through a declared interface. A Plugin has a manifest (`plugin.json`) that declares its identifier, version, permissions, and entry point. Plugins are loaded and managed by the Plugin Host. The core platform defines the interfaces that Plugins implement; it has no dependency on any Plugin implementation.

*See also:* → Extension, → Provider, → Plugin Host (→ Engine)

### Provider
A Plugin that supplies an external capability to the platform — typically an AI model, a storage backend, or an external service integration. The term "Provider" is used specifically when the Plugin's primary role is to supply an external resource rather than to add a new platform capability. A Model Provider implements `IModelProvider`; a Storage Provider implements `IKnowledgeStore`.

*See also:* → Plugin, → Extension

---

## R

### Repository
A version-controlled directory tree that contains the source code, documents, decisions, and platform state of a single project. In Ferret's model, the Repository is the primary unit of knowledge. Everything the platform knows about a project is derived from or stored within the Repository. Ferret does not maintain a knowledge store external to the Repository.

*See also:* → Workspace, → Knowledge, → Index

### Requirement
A stated condition that an implementation must satisfy. Requirements are expressed as acceptance criteria in Specifications. Requirements are identifiable by a structured identifier (e.g. `REQ-001`). The platform provides tooling to link implementation artefacts — code, tests, commits — to the Requirements they satisfy.

*See also:* → Specification, → Work Item

### Review
A formal examination of an artefact — code, specification, architecture, or AI-generated output — by a human reviewer, with or without AI assistance. A Review produces findings and a disposition (approved, approved with changes, rejected). Reviews are recorded in `docs/Reviews/`. Human Reviews are required before any AI-generated artefact is committed.

*See also:* → Architecture Review, → Artifact

---

## S

### Session
A bounded period of interaction between a user and the platform. A Session begins when a user invokes a platform operation and ends when that operation is complete or the interaction is closed. Session state — the current task, active files, recent decisions — is recorded in `.ai/session.md` and is available to the AI as part of the Context for subsequent interactions within the same Session. Sessions are not persistent across system restarts by default.

*See also:* → Context, → Workspace, → Artifact

### Specification
A structured document that defines the intent, scope, acceptance criteria, and constraints of a feature or sprint. Specifications are authored before implementation and approved before work begins. Specifications use the template in `docs/templates/spec.md`. In Ferret, Specifications are the primary inputs from which Requirements are extracted and implementation is validated.

*See also:* → Requirement, → ADR, → Work Item

### Sprint
A time-bounded unit of delivery. In the Ferret project, a Sprint corresponds to a set of Specifications that have been approved and a set of Work Items that have been completed. Sprints are numbered sequentially (Sprint 0, Sprint 1, ...). Sprint boundaries are used to organise specifications, metrics, and release notes.

*See also:* → Work Item, → Specification

---

## T

### Tool
In the context of MCP, a Tool is a discrete operation that an AI model can invoke through the MCP protocol. A Tool has a name, a description, an input schema, and an output schema. Ferret exposes its capabilities as Tools via its MCP Server role. Tool definitions follow the MCP specification's tool definition format.

*See also:* → MCP, → Extension, → Plugin

### Traceability
The property of a system by which every significant artefact can be linked to its origin, the actor that produced it, and the human who approved it. In Ferret, Traceability is maintained by recording interaction identifiers, model versions, and review dispositions as metadata on every committed AI-generated artefact.

*See also:* → Artifact, → Review

---

## W

### Work Item
A discrete unit of work tracked in an issue tracker (GitHub Issues or a compatible system). Work Items are linked to Specifications by identifier. Completed Work Items collectively satisfy the acceptance criteria of the Specification they belong to. Work Items may be bugs, tasks, or user stories.

*See also:* → Specification, → Sprint, → Requirement

### Workspace
The root directory of an Ferret-managed project, which coincides with the root of the Repository. The Workspace contains the `.ai/` folder (platform state), the `docs/` folder (specifications, architecture, and decisions), the `src/` folder (source code), and the other directories defined in the repository layout. The Workspace is the root from which all platform operations are invoked and from which all paths in the platform are resolved.

*See also:* → Repository, → Session, → Knowledge

---

## Cross References

| Document | Relationship |
|---|---|
| [Vision.md](Vision.md) | Motivates the terms defined here |
| [Mission.md](Mission.md) | Applies these terms to describe the project's goals |
| [Principles.md](Principles.md) | Uses these terms to state engineering constraints |
| [docs/002-Architecture/overview.md](../002-Architecture/overview.md) | Uses these terms to describe the system design |
| [docs/adr/README.md](../adr/README.md) | Uses these terms to describe the ADR process |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-06-27 | Ferret Core Team | Initial accepted version — 27 terms |
