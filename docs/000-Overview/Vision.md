# Ferret — Vision

| Field | Value |
|---|---|
| **Document ID** | VISION-001 |
| **Version** | 1.0 |
| **Status** | Accepted |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-27 |

---

## Purpose

This document articulates the long-term vision of the Ferret project: why it exists, the problems it is designed to solve, where the field of AI-assisted software engineering is heading, and how Ferret positions itself within that future. It is a durable reference for contributors, maintainers, and the community. It does not describe features or implementation plans — those belong in specifications and roadmaps.

---

## Scope

This document covers:

- The foundational motivation for Ferret
- The systemic problems in current AI-assisted engineering tooling
- The long-term trajectory of AI in software development
- Ferret's differentiated approach
- The design philosophy that governs all architectural decisions
- The guiding objectives that shape the project's direction

This document does not cover sprint scope, feature specifications, API contracts, or implementation guidance.

---

## 1. Why Ferret Exists

Software engineering has never been a purely individual act. It involves coordination across people, time, and systems — across requirements, decisions, implementations, tests, and deployments. Engineering teams accumulate knowledge: in code, in documents, in conversations, in commit history. The gap between the knowledge that exists and the knowledge that any one person or tool can access at a given moment is one of the core frictions in professional software development.

AI language models have demonstrated real capability to assist with code generation, explanation, and review. Yet most AI-assisted tooling treats this assistance as a transaction: a developer provides a prompt, the AI returns a response, and the context ends there. The AI knows nothing about the project's architecture, its past decisions, its coding conventions, or the intent behind the code it is reading. Each interaction begins from zero.

This statelessness creates a class of problems that improve slowly as AI models become more capable but never fully resolve through model capability alone. A model with unlimited context still cannot act on information it has never been shown. A more sophisticated reasoning system still cannot recall a decision that was never recorded in a form it can access.

Ferret exists to close this gap — not by building a smarter AI, but by building a better-informed one. It is a platform that gives AI systems persistent, structured, repository-local knowledge of the project they are working within, and couples that knowledge with the engineering discipline — specifications, ADRs, tests, reviews — that transforms individual work into institutional knowledge.

---

## 2. The Problems Ferret Addresses

### 2.1 Context Fragmentation

A typical engineering project distributes knowledge across version control, issue trackers, design documents, chat logs, email threads, and the heads of team members. No single system holds a coherent picture. AI assistants connected to only one of these systems — typically the source code — operate with an incomplete and often misleading view of what the project is and why it is the way it is.

### 2.2 Transient AI Interactions

Current AI coding tools operate session by session. A developer explains a design choice to their AI assistant on Monday and must explain it again on Wednesday, because no durable memory links the two conversations to the repository context that made them meaningful. This imposes cognitive load on developers who must repeatedly re-establish context, and it makes AI assistance unreliable as a long-term collaborator.

### 2.3 Lack of Traceability Between Intent and Implementation

In professional software development, there is a chain of intent: a business requirement drives an architectural decision, which drives an implementation, which drives a test, which drives a deployment. When any link in that chain is broken — when code exists with no traceable origin in a requirement, or when a requirement is fulfilled by code that no review has validated — quality degrades and maintenance costs rise.

AI tools that generate code without understanding or respecting this chain of intent introduce technical debt at the speed of inference. They are generative without being deliberate.

### 2.4 Toolchain Lock-in

The current landscape of AI-assisted development is fragmented across proprietary tools, each with its own model, its own context format, its own plugin system, and its own cost structure. Engineering teams that invest deeply in one toolchain find themselves tightly coupled to decisions made by a single vendor. When models improve, change their APIs, or change their pricing, the cost of adaptation falls entirely on the engineering team.

### 2.5 Missing Engineering Discipline

AI assistance lowers the cost of generating code to near zero. It does not lower the cost of maintaining it. A system that can generate a thousand lines of code in minutes has no inherent mechanism to ensure those lines are tested, documented, reviewed, or traceable to a requirement. Without engineering discipline built into the platform, AI assistance accelerates the accumulation of unmaintained, undocumented, untestable code.

---

## 3. The Future of AI-Assisted Software Engineering

The current phase of AI-assisted development is characterised by tools that assist individual developers with individual tasks in isolated sessions. This is a useful but shallow integration of AI into the engineering process.

The trajectory of the field points toward a deeper integration, where AI systems participate in the full software development lifecycle — not merely at the code-writing stage. In this future:

**Requirements are machine-readable and traceable.** Specifications are not documents written for human readers alone; they are structured artefacts that AI systems can parse, reason about, and use to validate that implementation matches intent.

**Architectural decisions are persistent and contextualised.** Every significant design choice is recorded with its rationale and trade-offs. AI systems can retrieve these records to avoid repeating past analysis and to flag when proposed changes conflict with established decisions.

**Code reviews are augmented, not replaced.** AI systems review code against the project's stated principles, its conventions, and the requirements the code is meant to satisfy. Human engineers review the AI's findings and make final judgements. The division of labour is explicit and disciplined.

**Knowledge is repository-local and incrementally built.** The AI's understanding of a project grows as the project grows. New code, new documents, and new decisions are indexed and made available to future interactions. No session starts from zero.

**The platform is separable from the model.** As AI models improve — and they will continue to improve — a well-architected platform enables teams to adopt better models without rebuilding their toolchain. The platform's value is in its structure, its integrations, and its accumulated knowledge, not in any particular model.

Ferret is designed to be a foundation for this future. It does not wait for AI models to solve these problems through raw capability. It builds the engineering scaffolding that makes the capability meaningful.

---

## 4. How Ferret Differs from Existing Tools

### 4.1 Repository-First, Not Session-First

Ferret treats the repository as the primary unit of knowledge. Everything the platform knows about a project is derived from or stored within the repository. This makes the platform's knowledge portable, auditable, and version-controlled alongside the code it describes.

### 4.2 Specification-Driven

Ferret is built on the premise that software should be specified before it is implemented. The platform provides first-class support for specifications — structured documents that define intent, scope, acceptance criteria, and constraints. AI assistance is grounded in these specifications; it does not operate in their absence.

### 4.3 AI-Model Agnostic

Ferret does not tie its value to any particular AI model or provider. The platform defines interfaces through which models are consumed, and any conforming implementation may be substituted. Teams choose the model that best fits their cost, capability, and compliance requirements.

### 4.4 Plugin Architecture

Ferret is extended through a typed, versioned plugin system. Core functionality is minimal and stable. Capabilities — language support, tool integrations, review workflows, reporting — are delivered as plugins. This separates the platform's stability from the rate of change in the tools and services it integrates.

### 4.5 Human Review as a Structural Requirement

Ferret does not present AI output as authoritative. Every significant AI action — a generated specification, a code review finding, an architectural recommendation — is a candidate for human review, not a final decision. The platform makes it easy to review, approve, reject, and audit AI contributions. This is not a feature; it is a design constraint.

---

## 5. Long-Term Vision (5+ Years)

### 5.1 The Intelligent Engineering Workspace

Within five years, the practice of software engineering will be shaped significantly by AI systems that maintain persistent, structured understanding of the projects they support. The question will not be whether AI participates in software development, but how that participation is governed, audited, and aligned with engineering values.

Ferret's long-term vision is to be the platform layer for this governed participation — the substrate on which AI-assisted engineering workflows are built, regardless of the models, tools, or services those workflows consume.

### 5.2 From Tool to Platform

An AI coding assistant is a tool. A platform is a foundation that others build on. Ferret aims to be the latter: a stable, documented, extensible foundation that communities, enterprises, and independent engineers can extend to fit their workflows. The platform's value compounds over time as the ecosystem of plugins, integrations, and knowledge formats grows.

### 5.3 From Project-Scale to Enterprise-Scale

The principles that make Ferret useful for a single repository become more valuable at enterprise scale: consistent engineering standards across many repositories, shared knowledge structures, governed AI access, and auditable AI participation. The platform is designed from the outset to scale to this context.

### 5.4 Community as a Core Asset

A platform's longevity depends on its community. Ferret's long-term health depends on a community of contributors who improve the core, build plugins, write documentation, and apply the platform to real engineering problems. The platform is designed to make contribution tractable: clean architecture, documented interfaces, and a governance model that makes it safe to invest in.

---

## 6. Design Philosophy

### 6.1 Engineering Discipline is Not Optional

AI makes it cheap to skip steps — to generate code without a specification, to merge without a review, to deploy without a test. Ferret is designed to make following engineering discipline the path of least resistance, not the path of most resistance. The platform enforces discipline by making it easy.

### 6.2 Transparency Over Convenience

When there is a choice between a convenient shortcut and a transparent, auditable path, Ferret chooses transparency. This applies to AI contributions, architectural decisions, and dependency choices. The cost of opacity compounds over time; the cost of transparency is paid once and then amortised.

### 6.3 Structure Enables Freedom

Constraints that are principled and consistently applied create freedom: freedom to change models, freedom to switch tools, freedom to onboard new contributors, freedom to audit past decisions. Ferret's structure — its plugin interfaces, its document formats, its knowledge schema — is designed to create this kind of freedom, not to constrain it.

### 6.4 The Repository is the Record

In Ferret's model, the repository is the canonical record of a project's state: its code, its specifications, its architecture decisions, its AI-assisted artefacts. Nothing important lives only outside the repository. This makes the repository a complete, portable, auditable history of the project.

---

## 7. Guiding Objectives

The following objectives guide all architectural and product decisions in Ferret. They are ordered by priority. When objectives conflict, the higher-ranked objective takes precedence.

| Priority | Objective | Statement |
|---|---|---|
| 1 | **Correctness** | The platform must never produce or enable incorrect conclusions without visible uncertainty. Better to return no result than a confidently wrong one. |
| 2 | **Traceability** | Every AI-contributed artefact must be traceable to the interaction that produced it, the model that produced it, and the human who reviewed or approved it. |
| 3 | **Auditability** | The state of the platform's knowledge — what it knows, when it learned it, what it has done with it — must be inspectable by any authorised user. |
| 4 | **Extensibility** | Core interfaces must be stable. New capabilities must be addable through the plugin system without modifying the core. |
| 5 | **Performance** | The platform must be fast enough to integrate into interactive development workflows without introducing friction. Latency is a user experience concern, not just an engineering one. |
| 6 | **Security** | Every component must follow least-privilege principles. AI systems must not have access to data or capabilities beyond what is required for their task. |
| 7 | **Portability** | Knowledge structures, document formats, and plugin interfaces must be portable across environments. No capability must depend on a single host, model, or operating system. |

---

## Cross References

| Document | Relationship |
|---|---|
| [Mission.md](Mission.md) | Operationalises this vision as a project mission |
| [Principles.md](Principles.md) | Engineering principles derived from this vision |
| [Glossary.md](Glossary.md) | Canonical definitions for terms used in this document |
| [ADR-0001](../adr/0001-use-architecture-decision-records.md) | First architectural decision, recording the ADR process |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-06-27 | Ferret Core Team | Initial accepted version |
