# Ferret — Mission

| Field | Value |
|---|---|
| **Document ID** | MISSION-001 |
| **Version** | 1.0 |
| **Status** | Accepted |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-27 |

---

## Purpose

This document defines the mission of the Ferret project: what it is building, for whom, and by what standards it measures success. It bridges the long-term vision articulated in Vision.md and the engineering decisions that translate that vision into concrete software. It is the reference document for evaluating scope decisions, community governance choices, and long-term sustainability strategy.

---

## Scope

This document covers:

- The project mission statement
- Measurable success criteria
- The intended user population
- The open-source philosophy governing the project
- Community and enterprise goals
- Long-term sustainability principles

This document does not cover feature specifications, release timelines, or engineering implementation details.

---

## 1. Mission Statement

Ferret is an open-source platform for AI-native software engineering. It provides the infrastructure to build, deploy, and govern AI-assisted engineering workflows — grounded in repository-local knowledge, driven by explicit specifications, and subject to human oversight at every consequential step.

The mission of the project is to make disciplined AI-assisted engineering accessible to any software team, on any codebase, with any AI model, without dependency on any proprietary platform or service.

---

## 2. What Ferret Builds

Ferret is a platform, not a product. It provides:

**A runtime for AI-assisted workflows.** The agent runtime executes multi-step reasoning processes — code review, specification generation, architecture analysis — within the context of a repository and its accumulated knowledge.

**A knowledge layer for repositories.** The indexing and knowledge subsystem builds and maintains a structured, incrementally updated understanding of a codebase: its symbols, its dependencies, its history, and the documents that describe its intent.

**An integration surface via the Model Context Protocol.** Ferret exposes its knowledge and capabilities through MCP, enabling any MCP-compatible host to query the platform and receive structured, context-aware responses.

**A plugin system for capability extension.** The platform defines stable interfaces through which tools, providers, review workflows, and storage backends are contributed by the community without modifying the core.

**A CLI for developer interaction.** Engineers interact with the platform through a consistent, scriptable command-line interface that supports interactive use, automation, and integration into existing CI/CD pipelines.

---

## 3. Success Criteria

Success is defined at three levels: platform, community, and adoption.

### 3.1 Platform Success

The platform succeeds when:

| Criterion | Measure |
|---|---|
| An AI assistant using Ferret can answer questions about a repository that it could not answer without it | Demonstrated by structured evaluation on reference codebases |
| Specifications written using Ferret templates are traceable to implementation and tests | Demonstrated by end-to-end traceability in sample projects |
| A plugin can be written, published, and consumed without modifying the core platform | Demonstrated by third-party plugins in the registry |
| Any conforming AI model can be substituted without loss of platform functionality | Demonstrated by verified compatibility with at least three distinct model providers |
| The platform builds, tests, and runs on Linux, macOS, and Windows with no platform-specific behaviour | Demonstrated by cross-platform CI |

### 3.2 Community Success

The community succeeds when:

| Criterion | Measure |
|---|---|
| Contributors outside the founding team can make meaningful contributions within a single session | Measured by time-to-first-contribution for new contributors |
| The plugin ecosystem includes contributions from independent authors | Measured by number of third-party plugins in production use |
| The governance model allows the project to continue if any individual maintainer becomes unavailable | Demonstrated by documented succession and decision-making processes |
| Security vulnerabilities are reported, assessed, and resolved within stated SLAs | Measured against the SLA published in SECURITY.md |

### 3.3 Adoption Success

Adoption succeeds when:

| Criterion | Measure |
|---|---|
| Engineering teams use Ferret as part of their regular development workflow | Demonstrated by sustained usage metrics from opted-in deployments |
| The platform is deployed in at least one enterprise context with governance and audit requirements | Demonstrated by an enterprise deployment case study |
| AI-generated artefacts produced through Ferret are consistently reviewed and approved by engineers | Measured by review completion rates in tracked deployments |

---

## 4. Intended Users

Ferret is designed for software engineers and engineering teams who want to integrate AI assistance into disciplined engineering practice. It is not designed for non-technical users or for use cases where the engineering process is not the primary activity.

### 4.1 Individual Engineers

An individual engineer uses Ferret to:

- Query their repository using natural language backed by structured knowledge
- Generate specification drafts grounded in existing architecture and decisions
- Review code changes against stated requirements and engineering principles
- Maintain a persistent, growing understanding of their codebase across sessions

The platform reduces repetitive context-setting and increases the relevance of AI assistance to the actual state of the project.

### 4.2 Engineering Teams

An engineering team uses Ferret to:

- Establish and enforce consistent engineering standards across the codebase
- Maintain a shared knowledge base accessible to all team members and to AI systems
- Track the lifecycle of requirements from specification to implementation to validation
- Conduct AI-assisted architecture reviews with documented findings and resolutions

The platform reduces knowledge silos and makes onboarding faster by making project knowledge structured and queryable.

### 4.3 Platform Engineers and Toolchain Owners

A platform engineer or toolchain owner uses Ferret to:

- Build custom plugins that integrate internal tools, services, and knowledge sources
- Deploy the platform as shared infrastructure for multiple engineering teams
- Configure model providers, access controls, and audit trails to meet internal compliance requirements
- Extend the CLI and API to fit the team's existing automation and workflow

The platform is designed to be operated, not just consumed — its configuration is code-like, its state is inspectable, and its behaviour is predictable.

---

## 5. Open-Source Philosophy

### 5.1 Core is Open, Extensions are Flexible

The Ferret core — the runtime, the knowledge layer, the plugin host, the CLI, and the MCP integration — is open-source under the MIT licence. No capability that is necessary for basic operation is proprietary or gated.

Extensions — plugins, integrations, hosted deployments — may be developed under any licence their authors choose. The platform's plugin interfaces are stable, documented, and available to commercial implementations on equal terms.

### 5.2 Decisions are Transparent

Every significant architectural decision is recorded as an Architecture Decision Record (ADR) in the repository. These records are permanent and publicly accessible. Contributors and users can understand the reasoning behind any design choice by reading the relevant ADR.

### 5.3 The Roadmap is Public

The project roadmap is maintained as a publicly visible sequence of milestone goals. Changes to the roadmap are communicated in advance. The community has input into prioritisation through the contribution process.

### 5.4 Governance is Documented

The project governance model — how decisions are made, how maintainers are selected, how disputes are resolved — is documented in the repository and applies to all contributors including the founding team. No contributor has unilateral authority over the project's direction.

### 5.5 Compatibility is a Commitment

Once a public interface reaches stable status (version 1.0 or higher), it will not be changed in a backwards-incompatible way without a major version increment, a deprecation period, and a migration guide. This commitment applies to plugin interfaces, API contracts, document formats, and the knowledge schema.

---

## 6. Community Goals

### 6.1 Lower the Barrier to Contribution

A new contributor should be able to make a meaningful contribution — a bug fix, a documentation improvement, a small feature — within a single working session. The contribution guide, code conventions, and architecture documentation are maintained specifically to make this possible.

### 6.2 Build a Plugin Ecosystem

The long-term quality of the platform depends on a diverse ecosystem of plugins. The project actively supports plugin authors by maintaining stable interfaces, providing the plugin SDK as a first-class artefact, publishing documentation for plugin development, and listing third-party plugins in the official registry.

### 6.3 Maintain a High-Quality Bar

Community contributions improve the platform only when they meet the same quality standard as core contributions: tested, documented, reviewed, and traceable to a requirement. The project does not lower this bar to increase contribution volume. A smaller number of high-quality contributions is preferable to a larger number of low-quality ones.

### 6.4 Foster Inclusive Technical Discourse

Technical discussion in the project — in issues, pull requests, and community forums — is expected to be precise, factual, and respectful. The Code of Conduct is enforced consistently. The goal is a community where technical disagreements are resolved on their merits and where all participants feel safe to contribute.

---

## 7. Enterprise Goals

### 7.1 Deployable in Regulated Environments

Enterprises operating in regulated industries require software that is auditable, configurable, and supportable. Ferret is designed to meet these requirements: its state is inspectable, its AI interactions are logged, its configuration is version-controlled, and its behaviour is deterministic given the same inputs.

### 7.2 On-Premise and Air-Gapped Deployment

No capability of Ferret requires a connection to any external service operated by the project. A team running Ferret with a locally deployed AI model, a local knowledge store, and a local plugin registry has access to the full platform without sending data outside their network boundary.

### 7.3 Integration with Enterprise Toolchains

Enterprise engineering teams operate within established toolchains: version control systems, issue trackers, CI/CD platforms, secret management systems. Ferret integrates with these systems through its plugin architecture rather than replacing them. The platform fits into an existing toolchain; it does not require an existing toolchain to be replaced.

### 7.4 Access Control and Audit

The platform supports role-based access to AI capabilities and knowledge sources. Every AI interaction that results in a committed artefact — a generated document, a review finding, a specification change — is attributed to the user and model that produced it and is retained in the repository history. This satisfies common requirements for AI governance in regulated environments.

---

## 8. Long-Term Sustainability

### 8.1 No Single Point of Control

The project governance model is designed so that no single individual or organisation controls the project's direction. Maintainer rights are distributed, decision-making processes are documented, and the contribution model is open to any qualified contributor.

### 8.2 No Mandatory Dependency on External Services

The platform does not require any external hosted service to operate. Dependencies on external services — cloud storage, hosted model APIs, external indexing services — are all optional and are supplied through the plugin interface. A team can run Ferret entirely on infrastructure they control.

### 8.3 Economic Model

The project does not have a commercial entity behind it that requires monetisation to survive. Sustainability comes from:

- Community contributions that improve the platform over time
- Plugin authors who extend its capabilities for their own use cases
- Enterprises that deploy the platform and contribute improvements back to the core
- Toolchain vendors who integrate the platform as part of their own offering

### 8.4 Versioning and Stability Commitment

The project treats stable API compatibility as a sustainability feature. Engineering teams invest in the platform when they trust that their investment will not be broken by an upgrade. The versioning policy — described in detail in the versioning template — exists specifically to make this trust warranted.

---

## Cross References

| Document | Relationship |
|---|---|
| [Vision.md](Vision.md) | Long-term vision that this mission operationalises |
| [Principles.md](Principles.md) | Engineering principles that implement this mission |
| [Glossary.md](Glossary.md) | Canonical definitions for terms used in this document |
| [CONTRIBUTING.md](../../CONTRIBUTING.md) | Operationalises the open-source philosophy |
| [SECURITY.md](../../SECURITY.md) | Operationalises the security goals |
| [docs/013-Governance/README.md](../013-Governance/README.md) | Governance index |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-06-27 | Ferret Core Team | Initial accepted version |
