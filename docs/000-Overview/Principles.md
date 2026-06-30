# Ferret — Engineering Principles

| Field | Value |
|---|---|
| **Document ID** | PRINCIPLES-001 |
| **Version** | 1.0 |
| **Status** | Accepted |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-27 |

---

## Purpose

This document defines the engineering principles of Ferret. These principles govern every architectural decision, implementation choice, and platform behaviour. They are not aspirational guidelines — they are constraints. A design that violates a principle is not acceptable regardless of its other merits. When principles conflict, the resolution process must be documented and, where the conflict is systemic, resolved by amending this document.

---

## Scope

These principles apply to:

- The core platform: runtime, knowledge layer, plugin host, CLI, MCP integration
- All first-party plugins and extensions
- Public-facing APIs and document formats
- The contribution and review process

Third-party plugins are encouraged to adopt these principles but are not obligated to, provided they interact with the platform only through its published interfaces.

---

## How to Apply These Principles

Each principle is stated as a design constraint that holds unless a documented exception is made. Exceptions must be recorded as Architecture Decision Records (ADRs). An undocumented exception is a violation, not a trade-off.

---

## Principle 1 — AI Agnostic

### Purpose
The platform must not be coupled to any specific AI model, AI provider, or AI API. Coupling to a specific model creates a dependency on decisions — pricing, capability changes, deprecation — made entirely outside the project's control.

### Why It Exists
AI models are improving rapidly. Coupling the platform to today's best model means inheriting its limitations, its costs, and its eventual obsolescence. The platform's value must reside in its structure, its knowledge infrastructure, and its integrations — not in the capabilities of any particular model.

### Expected Behaviour
- All AI model interactions are mediated through a `IModelProvider` interface defined in `Ferret.Core`.
- Model selection is configuration, not code. Changing the model requires no code change.
- The platform ships with no built-in model implementation. At least one model provider plugin must be configured before AI features are available.
- Model-specific capabilities (structured output, vision, tool use) are declared through the provider interface and queried at runtime. Features degrade gracefully when a capability is absent.

### Anti-Patterns
- Hardcoding a model identifier anywhere in the core codebase.
- Writing prompts that assume or require a specific model's behaviour, formatting conventions, or response structure.
- Making model performance characteristics (context window size, latency, throughput) assumptions in core logic.
- Building features that work only with one provider's API format.

### Example
A code review workflow calls `provider.CompleteAsync(prompt, options)` where `provider` is injected at runtime. The workflow does not know whether the provider wraps a cloud API, a locally running inference server, or a test double. Swapping providers requires only a configuration change.

---

## Principle 2 — Specification Driven Development

### Purpose
Software should be specified before it is implemented. A specification records intent explicitly and creates the basis for validation, review, and traceability. Without a specification, it is impossible to determine whether an implementation is correct — only whether it compiles and passes tests that may themselves be incorrectly specified.

### Why It Exists
AI systems lower the cost of generating code to near zero. Without a prior specification, there is no objective reference for reviewing AI-generated code. The reviewer is forced to infer what the code was supposed to do from the code itself — a circular and unreliable process.

### Expected Behaviour
- Every feature delivered by the platform originates from a specification document conforming to the spec template in `docs/templates/spec.md`.
- A specification must reach "Approved" status before implementation begins.
- Acceptance criteria in the specification are expressed as verifiable, testable conditions.
- Test cases are written against specification acceptance criteria, not against implementation details.
- The platform provides tooling to link implementation artefacts (code, tests, commits) to specification identifiers.

### Anti-Patterns
- Beginning implementation before a specification exists.
- Writing a specification that describes what the implementation does rather than what it is supposed to do.
- Specifications with acceptance criteria that cannot be verified without reading the implementation.
- Treating the specification as a record of what was built rather than a constraint on what will be built.

### Example
Sprint 1 requires an agent execution feature. Before any code is written, a specification `docs/001-Product/sprint-1-agent-execution.md` is authored, reviewed, and approved. Its acceptance criteria — "Given a valid specification, the agent completes all steps or fails with a typed error" — drive the test cases that are written before the implementation.

---

## Principle 3 — Plugin First

### Purpose
The core platform must remain minimal, stable, and independently useful. All capabilities beyond the core must be delivered through the plugin system. This separates the pace of innovation in capabilities from the pace of stability in the core.

### Why It Exists
A core that expands to accommodate every use case becomes brittle, over-coupled, and difficult to maintain. A plugin architecture allows capabilities to evolve independently, be versioned separately, and be contributed by the community without affecting the platform's stability guarantees.

### Expected Behaviour
- Every capability that is not strictly required for the runtime to initialise and the plugin host to operate is delivered through a plugin.
- Plugin interfaces are defined in `Ferret.Core` and are stable across minor versions.
- The core platform ships with zero built-in implementations of plugin interfaces. Default behaviour requires at least one plugin to be configured.
- First-party plugins are maintained in separate packages and versioned independently from the core.
- The plugin contract includes: manifest validation, activation lifecycle, capability declaration, permission model, and graceful deactivation.

### Anti-Patterns
- Implementing a capability in the core because it is convenient, intending to "extract it later."
- Leaking concrete plugin implementations into core abstractions.
- Making the runtime non-functional in the absence of a particular plugin.
- Bypassing the plugin interface from within the core to call a plugin implementation directly.

### Example
The platform needs to support both SQLite and PostgreSQL for knowledge storage. Neither is implemented in the core. Instead, `IKnowledgeStore` is defined in `Ferret.Core`. Two separate plugin packages — `Ferret.Plugin.Storage.Sqlite` and `Ferret.Plugin.Storage.Postgres` — implement this interface. Teams configure which plugin to load; the runtime is unaware of either.

---

## Principle 4 — Repository Local Knowledge

### Purpose
The AI's understanding of a project must be derived from and stored within the repository. Knowledge that lives only in external services is unavailable in offline or air-gapped environments, cannot be version-controlled, and may become inconsistent with the repository it is supposed to describe.

### Why It Exists
A knowledge base that cannot be version-controlled alongside the code it describes will eventually drift from the code's current state. A knowledge base stored in an external service may become unavailable or inaccessible without notice. Repository-local knowledge is portable, auditable, and always in sync with the repository state it corresponds to.

### Expected Behaviour
- The knowledge index is stored in a well-defined location within the repository (`.ai/` by default), committed alongside the code.
- The index is incrementally updated by the indexer and its state can be reproduced from the repository's source files at any time.
- No AI interaction requires connectivity to an external knowledge service. All knowledge queries are resolved against the local index.
- The knowledge schema is documented and stable. Tools outside the platform can read the index without a runtime dependency on Ferret.

### Anti-Patterns
- Storing knowledge exclusively in a cloud service that requires authentication and network access.
- Designing the knowledge format so that it cannot be reconstructed from source files without external data.
- Using the knowledge system to cache information that is not derived from the repository.
- Making AI responses depend on knowledge that is not reflected in the repository's version history.

### Example
A developer clones the repository on an air-gapped machine. After running `Ferret index build`, all knowledge queries return correct results. The developer's AI assistant can answer questions about the project's architecture, its ADRs, and its specification status without any network connection.

---

## Principle 5 — Deterministic Behaviour

### Purpose
Given identical inputs and configuration, the platform must produce identical outputs. Non-determinism in the platform layer — as distinct from the inherent non-determinism of AI model inference — makes debugging, testing, and auditing impossible.

### Why It Exists
AI model outputs are probabilistic. If the platform layer also introduces non-determinism — through race conditions, unordered collections, timestamp-dependent logic, or ambient state — then diagnosing unexpected behaviour requires distinguishing between model non-determinism and platform non-determinism. This is intractable. The platform must be deterministic so that any non-determinism in the system's output is clearly attributable to the model.

### Expected Behaviour
- Platform operations produce the same output given the same input, configuration, and knowledge state.
- Collections are ordered by defined criteria, not by hash map iteration order.
- Timestamps are injected as dependencies, not read from the system clock inline.
- Random values are not used in any core logic. Where randomness is required (e.g. for request identifiers), it is isolated behind an interface that can be controlled in tests.
- The platform's CI build is deterministic: same source, same binary, every time.

### Anti-Patterns
- Using `Dictionary<K,V>` iteration order as an implicit ordering in output.
- Reading `DateTime.UtcNow` directly in business logic.
- Using `Guid.NewGuid()` in paths where the value is included in a test assertion.
- Allowing thread scheduling to determine the order of output in concurrent operations.

### Example
The code review workflow processes a list of files. The findings are returned in lexicographic order by file path, then by line number. The same review run on the same commit always produces the same findings in the same order, regardless of the order in which the underlying model returns results.

---

## Principle 6 — Incremental Indexing

### Purpose
The knowledge index must be updated incrementally — processing only changed content — not rebuilt from scratch on every invocation. Full rebuilds are not practical on large codebases and would make the platform unusable in incremental development workflows.

### Why It Exists
A codebase of meaningful size cannot be re-indexed from scratch in interactive development time. If the knowledge index can only be built in full, it can only be meaningfully used in batch processes, not in the inner development loop. Incremental indexing is what makes the knowledge layer viable as a real-time development tool.

### Expected Behaviour
- The indexer tracks which files have changed since the last index run (by content hash, not by modification timestamp).
- Changed files are re-indexed; unchanged files are not re-processed.
- Index updates are atomic: a partial update leaves the index in its prior state, not in a corrupt intermediate state.
- The index can be built from scratch by deleting and rebuilding. The incremental and full-build paths produce identical results for the same repository state.
- Index build time scales with the size of the changeset, not with the size of the repository.

### Anti-Patterns
- Using file modification timestamps as a proxy for content change (these can be altered without content change).
- Making the full-build and incremental-build paths diverge in their treatment of any content type.
- Allowing partial index writes that could be observed by a concurrent reader.
- Designing the index format so that incremental updates require reading and rewriting the entire index.

### Example
A developer changes three files in a repository with 50,000 source files. The next `Ferret index update` run processes three files and completes in under a second. A full `Ferret index build` processes all 50,000 files and takes several minutes. Both produce an index with identical content for the same repository state.

---

## Principle 7 — Traceability

### Purpose
Every AI-contributed artefact — a specification, a review finding, a generated document, a code suggestion — must be traceable to the input that produced it, the model that produced it, the user who invoked it, and the review that approved or rejected it.

### Why It Exists
Traceability is the mechanism by which accountability is maintained in an AI-assisted workflow. Without traceability, it is impossible to audit why a decision was made, what information the AI had access to when it made it, or whether appropriate human oversight was applied. Traceability is not a compliance feature — it is a correctness feature.

### Expected Behaviour
- Every AI interaction that produces a committed artefact records: a unique interaction ID, the model identifier and version, the user identifier, the timestamp, and a reference to the knowledge state at the time of the interaction.
- Artefacts are linked to the specification or requirement they satisfy through explicit identifiers.
- The platform provides a command to query the provenance of any artefact: what produced it, when, and who approved it.
- Traceability records are stored in the repository, not in an external service.

### Anti-Patterns
- Committing AI-generated code without recording the model and interaction that produced it.
- Storing traceability records only in a system that is not version-controlled or that may be unavailable.
- Designing workflows where AI output is mixed into human output in a way that makes attribution ambiguous.
- Omitting traceability when AI-generated content is reviewed and approved — the approval is part of the trace.

### Example
A developer runs `Ferret review generate --spec AISP-142`. The platform records an interaction ID, the model used, and the current knowledge state hash. The resulting review document contains this provenance in its metadata. When the review is approved, the approval is recorded with the reviewer's identity and timestamp. Six months later, any engineer can reconstruct exactly what was known, what was generated, and who approved it.

---

## Principle 8 — Human Review

### Purpose
No AI-generated artefact may be committed to the repository or acted upon without explicit human review and approval. AI output is a candidate, not a decision.

### Why It Exists
AI models produce plausible output; they do not produce correct output. A model that is correct 95% of the time will produce incorrect output in 1 in 20 interactions. At scale, this is a significant source of defects. Human review is the mechanism by which incorrect AI output is caught before it enters the project's permanent record. The alternative — trusting AI output without review — shifts quality responsibility from the engineering team to the AI model, which has no accountability for the consequences of its errors.

### Expected Behaviour
- The platform does not provide a mode in which AI-generated artefacts are automatically committed without human interaction.
- The review step is a first-class platform concept, not an optional or skippable workflow step.
- The platform clearly marks AI-generated content as such at every stage of its lifecycle, before and after review.
- Human approval of AI-generated content is logged with the reviewer's identity and timestamp.
- The platform supports partial approval: a reviewer may accept some findings and reject others, with each decision recorded.

### Anti-Patterns
- Providing a `--no-review` flag that bypasses the human approval step in production workflows.
- Treating CI auto-approval as equivalent to human review.
- Designing the UI or CLI so that the path of least resistance is to approve without reading.
- Removing the AI-generated attribution from content after it has been reviewed, obscuring its origin.

### Example
An engineer requests a specification draft from the platform. The platform generates a draft and presents it in a review interface. The engineer reads each section, edits where necessary, and explicitly approves the document. The platform records the approval and allows the document to be committed. The commit metadata includes the interaction ID of the AI generation and the identity of the approving engineer.

---

## Principle 9 — Documentation First

### Purpose
Significant decisions, interfaces, and behaviours must be documented before they are implemented, not as a retrospective activity after the fact. Documentation is a design act; writing it surfaces ambiguities that would otherwise only appear during implementation.

### Why It Exists
Documentation written after implementation tends to describe what the code does rather than what it is supposed to do. This conflates specification with description and provides no basis for evaluating whether the implementation is correct. Documentation written first forces the author to think through the design before committing to an implementation, and provides a reference against which implementation can be reviewed.

### Expected Behaviour
- Public interfaces and their contracts are documented in the specification or architecture document before the implementation is written.
- ADRs are written for architectural decisions at the time the decision is made, not after the implementation is merged.
- API contracts are written as specification documents before any implementation is started.
- The documentation is the authoritative description of intended behaviour. If there is a conflict between the documentation and the implementation, the documentation is correct and the implementation is wrong until the documentation is explicitly updated.

### Anti-Patterns
- Writing a docstring that describes what a method does by reading its implementation.
- Merging a new API endpoint without a corresponding specification or contract document.
- Treating documentation as a post-release activity.
- Updating the implementation without updating the corresponding documentation.

### Example
A new plugin interface is designed for Sprint 2. The engineer writes `docs/007-SDK/plugin-capability-interface.md` describing the interface's contract, its lifecycle, and its error behaviour. The document is reviewed and approved. The `ICapabilityPlugin` interface in `Ferret.Core` is then written to match the approved document. If an implementation detail forces a change to the interface, the document is updated and re-reviewed before the code change is merged.

---

## Principle 10 — Testability

### Purpose
Every component of the platform must be testable without requiring infrastructure, network access, or AI model availability. If a component cannot be tested in isolation, its behaviour cannot be verified reliably.

### Why It Exists
A codebase that is difficult to test is a codebase that is difficult to change safely. Testability is not a testing concern — it is a design concern. A design that requires a running database, a live AI model, or a network connection to test its core logic has coupled its logic to its infrastructure in a way that makes both harder to change.

### Expected Behaviour
- All domain logic in `Ferret.Core` is testable with no dependencies on infrastructure or AI models.
- Infrastructure dependencies are hidden behind interfaces that have test implementations.
- AI model interactions are mediated through `IModelProvider`, which has a deterministic test implementation that returns configurable responses.
- Unit tests run in under 30 seconds on a developer workstation without any external service.
- Integration tests that require infrastructure are tagged, isolated, and require Docker Compose to run.

### Anti-Patterns
- Instantiating concrete infrastructure types (database connections, HTTP clients, file system paths) inside domain classes.
- Writing tests that require a real AI model to produce meaningful assertions.
- Making the constructor of a class responsible for acquiring its own dependencies.
- Designing test suites where adding a test requires starting a service.

### Example
The `AgentRunner` class depends on `IModelProvider`, `IKnowledgeStore`, and `IPluginHost` — all interfaces with test doubles. A unit test for `AgentRunner` instantiates the class with three deterministic test implementations, exercises the run logic, and makes assertions against the recorded interactions. No infrastructure is required. The test runs in milliseconds.

---

## Principle 11 — Extensibility

### Purpose
The platform must be extensible through defined extension points without requiring modification of the core. New capabilities, integrations, and behaviours are added by extending the platform, not by changing it.

### Why It Exists
Every direct modification of the core widens the surface area that must be understood, tested, and maintained. A platform that can only be extended by modification has no stable foundation — any change to extend it may break existing behaviour. The Open/Closed Principle applies: the core is open for extension through the plugin system and closed to modification for extension purposes.

### Expected Behaviour
- All user-facing capabilities are delivered through extension points defined in `Ferret.Core`.
- Extension points are documented, versioned, and subject to compatibility commitments.
- A new capability can be added by creating a plugin without reading or modifying core source code.
- The platform provides a plugin development guide and an SDK that is tested against the plugin contracts.
- Extension points support composition: multiple plugins can contribute to the same capability without conflicting.

### Anti-Patterns
- Adding a switch statement in core code to handle a new implementation type.
- Requiring a fork of the core to add a capability.
- Making extension points so generic that they provide no useful contract (e.g. an `IExtension` with no methods).
- Creating extension points that are so tightly coupled to one implementation that no other implementation is practical.

### Example
A team needs to integrate with their internal code review system. They write a plugin implementing `IReviewPublisher` and register it via the plugin manifest. The core review workflow calls `IReviewPublisher.PublishAsync(review)` — it has no knowledge of the team's internal system. The plugin does. The entire integration is contained in the plugin.

---

## Principle 12 — Performance

### Purpose
The platform must be fast enough to be used in the interactive development loop without introducing perceptible friction. Performance is a correctness concern: a platform that is too slow to use is a platform that is not used.

### Why It Exists
Developer tools live and die by their latency. A tool that takes ten seconds to respond breaks the development workflow; a tool that responds in under two seconds is invisible. The platform's performance must be measured against the standard of "indistinguishable from not having called the tool" in the common case.

### Expected Behaviour
- CLI startup time is under 500 ms on a modern workstation.
- Incremental index updates for a changeset of ten files complete in under two seconds.
- Knowledge queries return results in under 500 ms for repositories of up to 100,000 source files.
- AI interactions that require model inference report their state continuously; the user is never waiting for a silent operation.
- Performance targets are documented, tested, and enforced through BenchmarkDotNet benchmarks in CI.

### Anti-Patterns
- Loading the entire knowledge index into memory on startup.
- Making synchronous network calls in the hot path of CLI operations.
- Designing the knowledge query interface so that all queries require a full index scan.
- Adding capabilities to the startup path that are not required for the user's immediate operation.

### Example
A developer runs `Ferret review --file src/Core/Agent.cs`. The command starts, queries the index for context about `Agent.cs` and its dependencies, and begins streaming the review output to the terminal within 300 ms of the command being entered. The developer sees results before they have had time to look away from the screen.

---

## Principle 13 — Security

### Purpose
Every component must follow the principle of least privilege. No component accesses data, capabilities, or resources beyond what is required for its defined function. Security is a design constraint, not a post-implementation audit.

### Why It Exists
AI-assisted tools by definition have access to source code, specifications, and architectural documentation — some of the most sensitive data in an engineering organisation. A security failure in such a platform can expose intellectual property, credentials, or architectural vulnerabilities. Security must be designed in from the start; retrofitting it is expensive and unreliable.

### Expected Behaviour
- Plugins declare the permissions they require in their manifest. The platform grants only declared permissions.
- Credentials and secrets are never stored in the knowledge index, the repository, or any platform log.
- Network access by any platform component is blocked unless explicitly permitted by configuration.
- AI model interactions do not include data beyond what is necessary for the specific task.
- Security scanning (static analysis, dependency vulnerability checks) is a mandatory CI check, not an optional step.

### Anti-Patterns
- Plugins that request broad permissions "just in case" rather than the minimum required.
- Logging AI prompts or responses that may contain sensitive repository content at a level that persists to disk.
- Designing the knowledge schema so that it cannot selectively exclude sensitive files.
- Assuming that because a plugin is open-source, it is safe to grant unrestricted access.

### Example
A plugin that publishes review findings to an issue tracker declares permissions `["issue-tracker:write"]`. The platform grants access only to the issue tracker publication interface. The plugin cannot read source files, query the knowledge index, or invoke AI models — those permissions were not declared and are not granted.

---

## Principle 14 — Simplicity

### Purpose
The simplest design that correctly satisfies the requirements is preferred over a more complex one. Complexity that is not required to satisfy current requirements is deferred until it is required.

### Why It Exists
Premature complexity imposes a tax on every future contributor, every future modification, and every future debugging session. A complex design that was introduced to solve a problem that never materialised is purely a liability. The discipline of maintaining simplicity requires active resistance to the temptation to design for hypothetical future requirements.

### Expected Behaviour
- No abstraction is introduced until there are at least two concrete implementations that would benefit from it.
- No configuration option is added until there is a demonstrated use case where the default is wrong.
- No component is split until there is a demonstrated reason to deploy or version it independently.
- The simplest representation of data is used until performance or expressiveness requirements justify a more complex one.
- Code is reviewed for unnecessary complexity as part of the pull request process.

### Anti-Patterns
- Creating an interface for a class that has only one implementation, "for future extensibility."
- Adding configuration options to resolve a hypothetical conflict between use cases that do not yet exist.
- Splitting a module into two packages because they might eventually be deployed independently.
- Writing a generic framework when a simple utility function would suffice.

### Example
The first version of the knowledge query API accepts a string and returns a list of results. It does not accept pagination parameters, sort options, or filter predicates — those will be added when a use case requires them. The initial API is simple, testable, and correct for the current requirements.

---

## Principle 15 — Observability

### Purpose
The internal state and behaviour of every platform component must be visible through structured logging, distributed tracing, and metrics. A platform that cannot be observed cannot be debugged, operated, or improved.

### Why It Exists
AI-assisted workflows involve multiple components, network calls, model interactions, and knowledge queries. When something goes wrong — a slow response, an incorrect result, an unexpected failure — it must be possible to trace the failure to its source. Without structured observability built in from the start, adding it later requires invasive changes to every component.

### Expected Behaviour
- Every component emits structured log events at appropriate levels (Debug, Information, Warning, Error, Critical) using `ILogger<T>`.
- Every AI model interaction is represented as a distributed trace span with the interaction ID, model identifier, token counts, and latency.
- Core operations (index build, knowledge query, agent execution, plugin activation) emit metrics: count, duration, and error rate.
- No `Console.WriteLine` or `Debug.WriteLine` in library code. All observability output goes through `ILogger`.
- Observability is configurable: log level, trace sampling rate, and metric export target are all configurable without code changes.

### Anti-Patterns
- Using `Console.WriteLine` in library code for diagnostic output.
- Emitting structured events without consistent event names and property schemas.
- Making trace spans so coarse-grained that they are not useful for identifying which sub-operation failed.
- Making observability configurable only through recompilation.

### Example
A developer investigating a slow agent execution run opens their trace viewer and finds a single trace for the `agent.run` operation. Within it, they see child spans for `knowledge.query` (12 ms), `model.complete` (1,840 ms), `plugin.invoke` (45 ms), and `result.record` (8 ms). The latency is immediately identified as residing in the model call. No additional instrumentation or debugging is required.

---

## Cross References

| Document | Relationship |
|---|---|
| [Vision.md](Vision.md) | Vision from which these principles are derived |
| [Mission.md](Mission.md) | Mission that these principles serve |
| [Glossary.md](Glossary.md) | Canonical definitions for terms used in this document |
| [docs/002-Architecture/overview.md](../002-Architecture/overview.md) | Architecture that implements these principles |
| [docs/adr/0001-use-architecture-decision-records.md](../adr/0001-use-architecture-decision-records.md) | ADR process used when principles conflict |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-06-27 | Ferret Core Team | Initial accepted version — 15 principles |
