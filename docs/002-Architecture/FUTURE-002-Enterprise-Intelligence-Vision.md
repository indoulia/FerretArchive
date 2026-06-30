# FUTURE-002: Enterprise Intelligence Vision
**Status:** Exploration — Not Committed
**Date:** 2026-06-28
**Author:** Architecture Review
**Review Period:** Sprint 10–Sprint 14

---

## 1. Executive Summary

Ferret is, today, a Context Operating System. It collects information, structures it, and exposes it to external AI agents through MCP. That is a valid and defensible position. It is also a position that keeps Ferret permanently subordinate to every AI tool that consumes it.

This document explores whether Ferret should take the harder path: embedding its own AI capabilities and becoming an Enterprise Intelligence Platform in its own right, rather than feeding intelligence to systems built by others.

The recommendation is: **yes, but conditionally, and not yet**.

The conditions matter. Embedding AI prematurely is one of the most reliable ways to kill an enterprise platform. Every team that rushed to embed GPT-4 in 2023 has since spent twelve months debugging non-determinism, managing vendor lock-in, and explaining to enterprise security that inference calls leave the network boundary. The platforms that built durable AI infrastructure — Kubernetes, Grafana, GitHub Actions — did so by first establishing a rock-solid foundation layer, then adding intelligence as an optional, composable, replaceable layer above it.

Ferret is not yet at that foundation layer. The Index Platform is being built now. The Search Platform is Sprint 10. The MCP integration is Sprint 11. If Ferret embeds AI before it has a mature, reliable Core, it will be building reasoning capabilities on top of an unstable base — and the AI layer will inherit every instability below it and amplify it.

The specific recommendation is:

1. **Sprints 10–12:** Complete the Context OS. Index, Search, and MCP must be production-quality before any AI embedding begins. No shortcuts.
2. **Sprint 13:** Design the Model Platform contracts in `Ferret.Core.Models`. Interfaces only. No implementations. This reserves the namespace, forces the design, and preserves optionality without adding operational complexity.
3. **Sprint 14:** Implement a single, minimal, opt-in Model Provider — Ollama for local-first. This tests the architecture under real conditions without introducing cloud dependencies.
4. **V2 (Sprint 15+):** If the Model Platform contracts hold and user demand is real, expand to cloud providers and build the Agent Platform.
5. **V3:** Embedded intelligence as a first-class enterprise feature — with all the enterprise requirements (air-gap, RBAC, audit, multi-tenancy) that demands.

The single most important architecture decision in this document is not about AI at all. It is this: **`Ferret.Core` must never depend on `Ferret.Models`**. The AI layer must be an optional additive layer, not a mandatory dependency. Every enterprise customer who cannot use cloud AI — and there are many — must have a fully functional Ferret without it. If that invariant breaks, Ferret loses the air-gapped, regulated-industry, and security-conscious market segments permanently.

The risks of embedding AI are real: non-determinism in tests, vendor lock-in, inference cost management, prompt injection attacks, compliance complications, and the simple operational complexity of running models. None of these are fatal if addressed architecturally. All of them are fatal if ignored.

What follows is the rigorous exploration that this decision deserves.

---

## 2. Current Architecture

### 2.1 What Ferret Is Today

Ferret is a Context Operating System (ContextOS). Its job is to prepare high-quality, structured context and make it available to external AI agents. The current architecture reflects this cleanly.

```
┌─────────────────────────────────────────────────────────────────┐
│                   External AI Consumers                         │
│  Claude Code    Cursor    GitHub Copilot    Custom Agents        │
└────────────────────────────┬────────────────────────────────────┘
                             │  MCP Protocol (Sprint 11)
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Ferret (ContextOS)                           │
│                                                                 │
│  ferret search   ferret index   ferret connector   ferret init  │
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────────────┐    │
│  │  Workspace  │  │  Connectors │  │   Parser Platform    │    │
│  │  Platform   │  │  Platform   │  │   (Sprint 9)         │    │
│  └─────────────┘  └─────────────┘  └──────────────────────┘    │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │               Index Platform (Sprint 9)                  │   │
│  │    SQLite FTS5 / BM25    .ferret/indexes/keyword/        │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │               Search Platform (Sprint 10)                │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │               MCP Server (Sprint 11)                     │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌───────────────────┐  ┌──────────────────────────────────┐    │
│  │  Knowledge (V2)   │  │  Memory (V2/V3)                  │    │
│  └───────────────────┘  └──────────────────────────────────┘    │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │             Ferret.Core (zero external deps)             │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
         Filesystem       GitHub         SharePoint
         Connector       Connector       Connector
```

### 2.2 Architectural Strengths of the Current Model

**Separation of concerns is total.** Ferret never calls an AI model. An AI model calls Ferret. This inversion means Ferret is fully testable without mocking or stubbing AI inference. Every unit test is deterministic. Every integration test runs offline. This is a significant competitive advantage that most AI-embedded tools sacrifice immediately.

**The Core is genuinely zero-dependency.** `Ferret.Core` contains no infrastructure references. This is the architectural invariant that makes the rest possible. It was stated in ARCH-001, it is enforced in Sprint 7, and it remains true in Sprint 10. This is rare and valuable.

**Connector degradability is real.** If SharePoint goes offline, the filesystem connector continues to work. If the Git connector throws, the JIRA connector is unaffected. Connectors are designed to fail independently. This is the kind of operational resilience that enterprise customers actually require.

**The MCP abstraction is correct.** Exposing Ferret capabilities through MCP rather than a proprietary API means Ferret is not locked to any single AI provider. Claude Code today, custom agents tomorrow, Gemini CLI next year — the surface is stable regardless of which AI tool the user prefers.

**Deployment topology is flexible.** The same binary runs as a local CLI, as an MCP server, and (eventually) as an air-gapped enterprise appliance. Nothing in the current architecture prevents offline operation.

### 2.3 Structural Limitations of Option A

The current architecture has honest limitations that cannot be dismissed as theoretical concerns.

**The intelligence ceiling is externally imposed.** Every query Ferret answers is mediated by the context window and reasoning capability of whatever external AI receives it. Ferret can prepare excellent context, but cannot control how that context is reasoned over, what questions are asked of it, or how answers are synthesised. The quality ceiling is always the external AI's ceiling.

**Context preparation is not context understanding.** BM25 retrieves documents that match keywords. Semantic search (if added) retrieves documents that match embeddings. Neither of these is understanding. The difference between "retrieve documents about authentication" and "understand that our authentication module has a latent SQL injection risk given the schema we're using" is the difference between search and reasoning. Option A can deliver the former. Option B is required for the latter.

**Passive indexing misses relational insights.** Ferret can index that file A and file B both contain the word "auth." It cannot, without embedded AI, reason that file A's design pattern is architecturally incompatible with file B's expected interface contract. Cross-document reasoning requires an embedded reasoning layer.

**The MCP integration is a handoff, not a collaboration.** When Ferret exposes context to an external AI through MCP, the conversation ends. Ferret cannot ask follow-up questions, cannot refine its retrieval based on the AI's response, cannot learn from the interaction. It is a one-shot data pipeline. Embedded agents can close this loop.

**Keyword and semantic search have known limitations.** BM25 is excellent for exact term matching. Vector similarity is useful for fuzzy matching. Neither handles complex compositional queries well. "Show me all the places where our team violated the hexagonal architecture principle in the last 30 days" is not a retrieval problem — it is a reasoning problem.

**External AI dependency creates enterprise blockers.** An enterprise customer who runs air-gapped cannot use an external AI to reason over the context Ferret prepares, because the external AI call leaves the network boundary. Ferret-as-ContextOS is structurally unusable for air-gapped intelligence workloads without an embedded local model capability.

---

## 3. Proposed Vision — Option B

### 3.1 The Claim

Ferret evolves from a Context Operating System into an Enterprise Intelligence Platform. Rather than preparing context for external AI to reason over, Ferret embeds AI capabilities directly — model providers, prompt engineering, agent runtimes, and memory — and becomes the primary reasoning layer for enterprise developer intelligence.

This does not mean replacing external AI tools like Claude Code or Cursor. It means that Ferret's own capabilities become AI-native, and that Ferret can provide intelligent answers, proactive insights, and autonomous assistance without requiring an external AI host.

### 3.2 What This Means Concretely

Under Option B, the following become possible:

- `ferret ask "Why does our deployment fail on Tuesdays?"` — Ferret reasons over logs, code changes, calendar, deployment history, and returns an analysed answer, not a search result.
- `ferret architect "Design a caching layer for our API"` — Ferret generates an architecture proposal grounded in the existing codebase, applies existing architectural patterns, and identifies conflicts with current decisions.
- `ferret review src/Api/Auth/` — Ferret performs a security-focused code review, cross-referenced against the project's own ADRs and known vulnerability patterns.
- `ferret watch` — A background agent monitors the workspace, detects semantic drift between code and specifications, and raises alerts without being asked.

### 3.3 The Target Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                   External AI Consumers                         │
│  Claude Code    Cursor    GitHub Copilot    Custom Agents        │
│  (continue to work via MCP — Option B is additive, not          │
│   replacing; external consumers still work)                     │
└────────────────────────────┬────────────────────────────────────┘
                             │  MCP Protocol
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Ferret (Enterprise Intelligence Platform)     │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              Intelligence Layer (Option B)               │   │
│  │                                                          │   │
│  │  ┌──────────────┐  ┌───────────────┐  ┌──────────────┐  │   │
│  │  │ Agent        │  │ Prompt        │  │ Model        │  │   │
│  │  │ Platform     │  │ Platform      │  │ Routing      │  │   │
│  │  └──────────────┘  └───────────────┘  └──────────────┘  │   │
│  │                                                          │   │
│  │  ┌──────────────────────────────────────────────────┐    │   │
│  │  │              Model Platform                      │    │   │
│  │  │  Ollama  OpenAI  Anthropic  Azure  LM Studio     │    │   │
│  │  └──────────────────────────────────────────────────┘    │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              Context Layer (Option A — current)          │   │
│  │  Workspace  Connectors  Parser  Index  Search  MCP       │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │          Memory & Knowledge Layer                        │   │
│  │  WorkingMemory  EpisodicMemory  LongTermMemory           │   │
│  │  EntityStore    RelationshipStore    DocumentStore       │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │             Ferret.Core (zero external deps — FOREVER)   │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

The critical point in this diagram: the Context Layer and the Intelligence Layer are separate strata. The Core has no dependency on either. This is not optional — it is the invariant that makes the entire architecture viable.

---

## 4. Comparative Analysis

### 4.1 Scalability

**Option A (ContextOS):**
Scales well because it does no inference. Indexing is CPU-bound and can be parallelised. Search is I/O-bound against SQLite. MCP serving is lightweight. A Ferret instance serving 100 developers through MCP requires modest resources. The scaling story is simple: more connectors, bigger indexes, more parallel MCP clients. No GPU required. No inference server. No token cost per query.

**Option B (Intelligence Platform):**
Scaling AI is categorically harder. Local models (Ollama, LM Studio) are bounded by GPU VRAM. Cloud models (OpenAI, Anthropic) are bounded by API cost, rate limits, and token quotas. An enterprise deployment where 200 developers each run `ferret ask` queries throughout the day will generate substantial inference costs. The cost model must be designed before the feature is built, not after.

The honest answer: Option B scales if and only if model routing is intelligent. Running every query against GPT-4o when GPT-4o-mini or a local Llama would suffice is fiscal negligence at enterprise scale. Model routing is not a nice-to-have — it is a core scalability mechanism.

**Verdict:** Option A is simpler to scale. Option B is scalable with careful routing design but requires explicit cost management infrastructure.

### 4.2 Extensibility

**Option A:**
Extensibility is through connectors, parsers, and MCP tools. Each of these is a clean extension point with well-defined contracts. Adding a new connector requires no changes to existing code. This is the current model, and it works.

**Option B:**
Option B adds three new extension axes: model providers, agent types, and prompt templates. If designed with the same discipline as connectors (interface-first, isolated projects, no cross-dependencies), this can be equally clean. If designed poorly — embedding model calls into application logic, hardcoding prompt templates, coupling agents to specific providers — it becomes an unmaintainable hairball within eighteen months.

The reference model is LangChain. LangChain attempted to be the universal AI extensibility layer and became so complex that most serious teams end up building their own thin wrapper rather than fighting LangChain abstractions. The lesson is not to avoid extensibility — it is that extensibility requires rigorous contract discipline, which LangChain abandoned in favour of rapid feature shipping.

**Verdict:** Option B can be more extensible, but only with the same discipline that Ferret has applied to connectors. Without that discipline, extensibility becomes complexity.

### 4.3 Enterprise Adoption

**Option A:**
Enterprise adoption is gated on: Does the enterprise already use external AI tools (Claude Code, Copilot, Cursor)? For those that do, Option A is immediately compelling — Ferret makes their existing AI tools more capable by giving them better context. The sales argument is simple: no new AI infrastructure, no new vendor, just better results from the tools you already bought.

**Option B:**
Enterprise adoption for Option B is gated on: Is the enterprise willing to add a new AI platform to their approved vendor list? This is a significantly harder conversation. Enterprise procurement for AI systems involves legal review, security assessment, privacy impact assessment, and executive approval. Each additional cloud model provider in Ferret's dependency list adds a new procurement conversation.

The counterargument: enterprises who cannot use external AI tools (regulated industries, defence contractors, healthcare) are precisely the ones who need embedded AI. For them, Option A is useless for intelligence workloads. Option B with local-first model support is the only viable path.

**Verdict:** Option A wins for enterprises already invested in external AI tools. Option B wins for enterprises that cannot use external AI or want AI infrastructure they control. Both markets are real and large.

### 4.4 Security

**Option A:**
Security perimeter is simple. Ferret reads local files and exposed APIs (connectors). It writes to `.ferret/`. It serves MCP responses over a local socket. The attack surface is well-defined. There are no outbound AI inference calls. There is no prompt injection risk at the Ferret layer (though the external AI that receives Ferret's context can be prompt-injected through malicious content in indexed documents).

**Option B:**
Security surface expands substantially. Prompt injection becomes Ferret's problem, not just the external AI's problem. An attacker who can write a file to an indexed directory can inject malicious prompts that Ferret's embedded AI will execute. This is a real, documented attack vector — it has been demonstrated against GitHub Copilot, against Cursor, and against any RAG-based system that executes code found in retrieved context.

Additionally, if Ferret makes outbound calls to cloud model APIs, enterprise network egress policies apply. Credentials for model APIs must be stored securely (keychain, not config files). API keys must be rotated. Rate limit errors must be handled gracefully. All of this is security surface that does not exist in Option A.

The air-gap mode complicates this further. A genuinely air-gapped Option B means local models only — Ollama, LM Studio, or similar. Running a local model server introduces its own attack surface: a locally-running HTTP server that accepts inference requests.

**Verdict:** Option B requires a substantially more sophisticated security architecture. Prompt injection must be treated as a first-class threat, not an afterthought.

### 4.5 Vendor Lock-In

**Option A:**
Vendor lock-in risk is minimal. Ferret uses SQLite (open source, public domain). The MCP protocol is open. Connectors target specific services (SharePoint, GitHub) but the connector contracts are internal. The only lock-in risk is the MCP protocol itself, but this is a protocol with multi-vendor support and no single vendor controls it.

**Option B:**
Vendor lock-in is the central design problem. If Ferret hardcodes any dependency on a specific model provider — even as a default — it is locked to that provider's pricing, availability, API stability, and terms of service. OpenAI's API has broken backward compatibility multiple times. Anthropic has changed Claude's behaviour through policy updates. Relying on a specific model's output format or reasoning style is technical debt that compounds over time.

The only defense is genuine provider abstraction. Not "we support multiple providers" as a marketing claim, but "every model call in the system goes through an interface that is testable without any real model." This is harder to build than it sounds — particularly for features that depend on specific model capabilities like structured output, function calling, or context length.

**Verdict:** Option B has significant lock-in risk if provider abstraction is not treated as a first-class architectural concern from day one.

### 4.6 Local-First and Offline Support

**Option A:**
Local-first is native. Ferret already operates entirely locally. The filesystem connector, the SQLite index, the BM25 search — none of these require network connectivity. MCP serving is local by default. A developer on an airplane with a downloaded workspace has full Ferret functionality.

**Option B:**
Local-first is possible but requires deliberate architecture. If the model platform defaults to cloud providers, offline mode is broken. The architecture must specify: what happens when no model is available? The answer must not be "Ferret crashes" or "Ferret returns an error for every AI feature." The answer must be "Ferret degrades gracefully to its Context OS capabilities."

This is an argument for making local model support (Ollama) the first implementation, not the afterthought. If the baseline for all AI features is "this works with a local model," then cloud providers are an upgrade, not a requirement. The opposite design — cloud-first with a local fallback bolted on later — produces a system that is permanently fragile offline.

**Verdict:** Option A is naturally local-first. Option B must be explicitly designed for local-first or it will never be genuinely local-first.

### 4.7 Testing Complexity

**Option A:**
Testing is deterministic. Unit tests use fakes. Integration tests use temp directories and in-memory SQLite. No test requires a model. No test output varies based on inference non-determinism. CI runs in minutes, not hours, and never fails due to model timeouts or API rate limits.

**Option B:**
Testing becomes fundamentally harder. Unit tests for logic that calls models must mock the model. Mocks that return canned responses test the routing logic but not the prompt behaviour. Tests that call real models are non-deterministic, slow, and expensive. The test pyramid inverts: what should be a unit test becomes an integration test; what should be an integration test becomes an evaluation.

The industry has not solved this problem. LangChain's test suite is notoriously slow. LlamaIndex has similar issues. Claude Code's own testing likely uses a mix of cached responses, deterministic sampling seeds, and explicit evaluation frameworks. None of these are free, and none produce the same developer confidence as a fully deterministic test suite.

This is not a reason to avoid Option B, but it is a cost that must be acknowledged. If Ferret embeds AI, it needs a proper model evaluation framework — not just unit tests with model mocks, but a structured way to evaluate prompt behaviour, output quality, and regression across model versions.

**Verdict:** Option B makes testing significantly harder. An evaluation framework must be built alongside the model platform, not after it.

### 4.8 Deployment Complexity

**Option A:**
Deployment is a single binary. `dotnet publish` produces a self-contained executable. No GPU, no model server, no inference infrastructure. The deployment story for a developer is: download one binary, run it. The deployment story for an enterprise is: distribute one binary via your existing software distribution channel.

**Option B:**
Deployment complexity scales with model provider selection. Local model deployment (Ollama) requires: Ollama installation, model download (multi-gigabyte), and a running Ollama server. Cloud model deployment requires: API key provisioning, network egress policy, rate limit management. Enterprise deployment adds: GPU infrastructure planning, model access control, inference cost allocation.

A Ferret deployment guide for Option B with Ollama is at least five pages longer than Option A. For enterprises with air-gap requirements, it requires a private model registry, on-premises inference servers, and model governance. This is not hypothetical complexity — it is the operational reality that every enterprise AI platform faces.

**Verdict:** Option B is substantially harder to deploy. The deployment model must be as carefully designed as the software model.

---

## 5. Advantages of Embedded AI

With the risks clearly stated, the genuine advantages of Option B deserve equal rigour.

### 5.1 Closing the Reasoning Loop

The most important advantage of embedded AI is the ability to close the reasoning loop within Ferret itself. In Option A, Ferret prepares context and hands it to an external AI. The external AI reasons over it and returns results to the user. Ferret never sees those results. It cannot learn from them, refine its retrieval based on them, or improve its context preparation based on what the AI found useful.

In Option B, Ferret controls the full loop: retrieve → reason → evaluate → retrieve-again → synthesise. This is the architecture of every high-quality RAG system. The difference between naive RAG (retrieve once, prompt once, return result) and production RAG (multi-hop retrieval, query decomposition, result validation) is enormous in practice. Option A is structurally limited to naive RAG as seen by the consuming AI. Option B enables the full production RAG pattern.

### 5.2 Context-Aware Intelligence

An embedded AI has access to context that an external AI cannot have. Specifically:

- **Workspace history:** What was searched, what was found, what was useful, what was not. An embedded agent can accumulate a session memory that makes each subsequent query better informed.
- **Organisation-specific patterns:** An embedded agent trained or prompted with Ferret's understanding of this specific organisation's architecture patterns can give advice that a generic AI cannot.
- **Cross-session learning:** Long-term memory (V3) allows Ferret to remember that the user asked about the authentication system three times last month, that they consistently find answers in the `src/Api/Auth/` directory, and that the most relevant documents are always those modified in the last 30 days. This is personalisation that is impossible to achieve through an external AI that sees a fresh context window on every invocation.

### 5.3 Enterprise Differentiation

For the segment of enterprise customers who cannot use external AI — regulated industries, defence, healthcare, legal — Option B with local model support is the only viable path to AI-assisted developer intelligence. This is not a niche. Healthcare organisations subject to HIPAA, defence contractors subject to ITAR, financial institutions subject to FCA rules, and government agencies subject to data residency requirements collectively represent a market that cloud-AI-dependent tools systematically cannot serve.

Ferret-as-ContextOS can serve these customers for context preparation, but cannot serve them for AI-assisted reasoning. Ferret-as-Intelligence-Platform, with genuine local-first model support, can serve them completely. This is a meaningful competitive differentiation.

### 5.4 Proactive Intelligence

Option A is reactive. A developer asks a question; Ferret retrieves relevant context; an external AI answers. Option B enables proactive intelligence: Ferret's embedded agents monitor the workspace continuously and surface insights without being asked.

Examples of proactive capabilities that are architecturally impossible in Option A:
- Detecting when new code contradicts an existing ADR and raising an alert
- Identifying when a file has not been modified in six months but is referenced by actively changing code (potential hidden dependency)
- Noticing when a pattern of changes over the past week is converging toward a known architectural anti-pattern

These are not search queries. They are continuous monitoring tasks that require ongoing reasoning over the full workspace state. An external AI cannot perform these tasks because it has no continuous presence in the workspace.

### 5.5 First-Class CLI Commands

Option A's CLI is inherently a retrieval CLI: `ferret search`, `ferret index`, `ferret connector`. Option B's CLI can be an intelligence CLI: `ferret ask`, `ferret architect`, `ferret review`, `ferret fix`. The difference in user-facing value is dramatic.

The comparison to GitHub Copilot and Cursor is instructive. Both tools generate value through embedded AI, not through context preparation alone. A developer using Cursor does not think "Cursor prepared my context well today." They think "Cursor fixed my bug." Ferret's competitive positioning requires the same direct value delivery to earn sustained adoption.

---

## 6. Disadvantages and Risks

### 6.1 Non-Determinism in Production

AI model outputs are stochastic. Temperature settings, sampling algorithms, and model version changes all produce different outputs from identical inputs. This creates an entirely new category of production bug: "the system was working fine, and then it started returning different results, and we cannot reproduce the old behaviour."

This is not theoretical. Every team that has run AI in production has experienced model drift — subtle changes in model behaviour following a provider update that cause downstream breakage without any code change on the consumer's side. The Ferret answer must be: explicit model versioning, pinned model releases for production deployments, and automated evaluation of output quality on model updates.

### 6.2 Prompt Injection

When Ferret indexes a document and then uses that document's content to construct a prompt for an embedded AI, any malicious content in that document becomes a prompt injection vector. An attacker who can write a file to an indexed directory can instruct Ferret's AI to take arbitrary actions — including exfiltrating workspace state, corrupting the knowledge graph, or executing malicious tool calls if Ferret's agents have tool execution capabilities.

This is not a future risk. It has been demonstrated in practice against multiple production AI systems. The defense requires: input sanitisation, prompt construction discipline (user content always in a separate prompt section, never interpolated into instruction content), and agent capability restrictions (agents should not have capabilities they do not need for their specific task).

### 6.3 Inference Cost as Operational Overhead

Every AI call has a cost. For local models, the cost is GPU compute and time. For cloud models, the cost is direct monetary cost. At developer-tool scale, these costs are manageable. At enterprise scale with multiple daily active users and proactive background agents, they are a significant operational line item.

Ferret must provide cost visibility and control: per-user cost tracking, per-operation cost attribution, hard limits on inference spend, and clear documentation of which operations trigger model calls. Without this, enterprise customers will reject Option B on cost management grounds alone, regardless of its technical capabilities.

### 6.4 Compliance and Data Privacy

Sending enterprise code, documentation, and knowledge to a cloud model API is a data privacy event. The code may contain proprietary algorithms. The documentation may contain confidential business information. The knowledge graph may contain personnel information.

Every cloud model provider has terms of service regarding data usage, data retention, and training data. These terms change. Even if a provider's terms are acceptable today, they may not be acceptable after the next terms update. An enterprise customer who discovers that their confidential code was used to train a commercial model has a genuine legal problem.

The architectural requirement: model calls must be configurable as local-only. Cloud model calls must be opt-in, not opt-out. And when cloud model calls are enabled, the data sent to the model must be the minimum necessary — never the full document, always the extracted relevant excerpt.

### 6.5 The Complexity Budget

Every capability has a complexity cost. Ferret has, so far, spent its complexity budget carefully: the Core is simple, the Connector Platform is well-designed, the Index Platform is straightforward. Embedding AI would be the largest single increase in system complexity since the project began.

Complex systems fail in complex ways. A Ferret that crashes during indexing is a straightforward problem to debug. A Ferret whose embedded agent returns a wrong answer is not — because there is no stack trace for a wrong answer. Debugging AI behaviour requires entirely different tools and disciplines than debugging software behaviour.

The team building Ferret must honestly assess whether they have the capacity to maintain both the Context OS and the Intelligence Platform to production quality simultaneously. Splitting focus between two fundamentally different engineering challenges is a reliable way to do both poorly.

---

## 7. Migration Strategy

### 7.1 Principle: Option B is Additive

The migration from Option A to Option B must not break existing users. A developer who uses Ferret for MCP-based context serving today must continue to have a fully functional Ferret after Option B features are added. The Intelligence Layer is additive, not a replacement.

This is non-negotiable. Every enterprise platform that forced users to migrate to a new architecture in order to continue using existing features lost adoption. Docker Swarm's fate when Kubernetes emerged is instructive — the tool that required migration lost; the tool that composed gracefully won.

### 7.2 Phased Migration Path

```
Phase 0 (Now — Sprint 12): Complete Context OS
────────────────────────────────────────────────
No AI embedding. Complete Index, Search, and MCP to production quality.
Deploy MCP Server. Gather real user feedback on context quality.
Establish the performance baseline.

Phase 1 (Sprint 13): Model Platform Contracts
──────────────────────────────────────────────
Define interfaces in Ferret.Core.Models (no implementations).
Design IModelProvider, IChatModel, IEmbeddingModel, IReranker.
Add models.json to .ferret/config/ with schema validation.
Add `ferret model list` and `ferret model test` commands (no inference yet).
No existing functionality changes.

Phase 2 (Sprint 14): First Model Provider (Ollama)
────────────────────────────────────────────────────
Implement Ferret.Models.Ollama against IModelProvider.
Add semantic index backend (Ferret.Index.Semantic) using Ollama embeddings.
Semantic search is opt-in; keyword search remains default.
No agent platform. No prompt platform. No intelligence commands.
Users who do not configure Ollama see zero change.

Phase 3 (V2 Sprint 15+): Prompt Platform and Basic Intelligence
────────────────────────────────────────────────────────────────
Add Ferret.Prompt — template engine, context injection, composition.
Add `ferret ask` as a thin wrapper: retrieve + prompt + display.
Add second model provider (Anthropic or OpenAI).
Add model routing (basic: route to configured provider).
AI features remain opt-in; no feature changes to existing commands.

Phase 4 (V2 Sprint 18+): Agent Platform
────────────────────────────────────────
Add Ferret.Agent — Planner, Reasoner, TaskGraph, ToolExecutor.
Add `ferret architect`, `ferret review` as agent-backed commands.
Agent capabilities are scoped and auditable.
Prompt injection mitigations are implemented and tested.

Phase 5 (V3): Enterprise Intelligence Features
───────────────────────────────────────────────
Proactive monitoring agents.
Organisation memory and long-term learning.
Multi-user agent sessions.
Full enterprise deployment support (air-gap, RBAC, audit logs).
```

### 7.3 The Invariant Across All Phases

In every phase, the following must remain true:

1. `ferret search`, `ferret index`, `ferret connector`, `ferret workspace` work identically whether or not a model is configured.
2. `Ferret.Core` has no reference to any model provider or intelligence interface.
3. The MCP server exposes all context capabilities regardless of intelligence configuration.
4. Local-first operation is possible in every phase.

These are not aspirational goals. They are architectural invariants that must be verified by the test suite at every sprint.

---

## 8. Recommended Architecture

### 8.1 The Layered Model

```
┌─────────────────────────────────────────────────────────────────┐
│                    CLI / API Surface                            │
│  ferret ask  ferret architect  ferret review  ferret watch      │
│  ferret search  ferret index  ferret connector  ferret mcp      │
└─────────────────────────────┬───────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────┐
│                   Intelligence Layer                            │
│  Ferret.Agent   Ferret.Prompt   Ferret.Routing                  │
│  (optional — disabled if no model configured)                   │
└─────────────────────────────┬───────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────┐
│                   Model Platform                                │
│  Ferret.Models.Ollama  Ferret.Models.OpenAI                     │
│  Ferret.Models.Anthropic  Ferret.Models.AzureOpenAI             │
│  Ferret.Models.LmStudio  Ferret.Models.Groq                     │
│  (all behind IModelProvider — provider is swappable)            │
└─────────────────────────────┬───────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────┐
│                   Context Layer (Option A)                      │
│  Ferret.ConnectorPlatform  Ferret.ParserPlatform                │
│  Ferret.IndexPlatform  Ferret.SearchPlatform                    │
│  Ferret.McpServer  Ferret.WorkspacePlatform                     │
└─────────────────────────────┬───────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────┐
│                   Knowledge and Memory Layer                    │
│  Ferret.Knowledge  Ferret.Memory                                │
└─────────────────────────────┬───────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────┐
│                   Ferret.Core                                   │
│  Contracts, Value Objects, Domain Model                         │
│  Zero External Dependencies — Forever                           │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Dependency Rules

The dependency rules extend, not replace, ARCH-001's existing rules:

```
Ferret.Core                          no external dependencies (ARCH-001, enforced)
Ferret.Knowledge                     depends on: Ferret.Core
Ferret.Memory                        depends on: Ferret.Core
Ferret.{ConnectorPlatform,...}       depends on: Ferret.Core
Ferret.Models.*                      depends on: Ferret.Core.Models (interfaces only)
Ferret.Routing                       depends on: Ferret.Core.Models, Ferret.Models.*
Ferret.Prompt                        depends on: Ferret.Core.Models, Ferret.Memory
Ferret.Agent                         depends on: Ferret.Prompt, Ferret.Routing,
                                                  Ferret.SearchPlatform, Ferret.Memory
Ferret.Cli                           depends on: all platform modules via DI
```

**Forbidden:**
- `Ferret.Core` → anything
- `Ferret.ConnectorPlatform` → `Ferret.Models.*`
- `Ferret.IndexPlatform` → `Ferret.Models.*` (except Ferret.Index.Semantic, which explicitly depends on IEmbeddingModel)
- `Ferret.Models.*` → each other (providers are independent)
- `Ferret.Agent` → specific model implementations (only through IModelProvider)

---

## 9. Model Platform Design

### 9.1 Core Interfaces

All model platform contracts live in `Ferret.Core.Models`. No implementation lives there. The interfaces are designed to be testable with simple fakes.

```csharp
// Ferret.Core.Models — IModelProvider.cs
public interface IModelProvider
{
    string ProviderId { get; }
    ModelProviderMetadata Metadata { get; }
    IReadOnlyList<ModelDescriptor> AvailableModels { get; }
    Task<ModelProviderHealth> CheckHealthAsync(CancellationToken ct = default);
    IChatModel GetChatModel(string modelId);
    IEmbeddingModel GetEmbeddingModel(string modelId);
    IReranker? GetReranker(string modelId);
}

// Ferret.Core.Models — IChatModel.cs
public interface IChatModel
{
    string ModelId { get; }
    ModelCapabilities Capabilities { get; }
    IAsyncEnumerable<ChatChunk> ChatAsync(
        ChatRequest request,
        CancellationToken ct = default);
    Task<ChatResponse> CompleteChatAsync(
        ChatRequest request,
        CancellationToken ct = default);
}

// Ferret.Core.Models — IEmbeddingModel.cs
public interface IEmbeddingModel
{
    string ModelId { get; }
    int EmbeddingDimension { get; }
    Task<EmbeddingVector> EmbedAsync(
        string text,
        CancellationToken ct = default);
    Task<IReadOnlyList<EmbeddingVector>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default);
}

// Ferret.Core.Models — IReranker.cs
public interface IReranker
{
    string ModelId { get; }
    Task<IReadOnlyList<RankedResult>> RerankAsync(
        string query,
        IReadOnlyList<SearchResult> candidates,
        int topK,
        CancellationToken ct = default);
}

// Ferret.Core.Models — ModelDescriptor.cs
public sealed record ModelDescriptor(
    string ModelId,
    string ProviderId,
    ModelType Type,         // Chat | Embedding | Reranker | Multimodal
    ModelCapabilities Capabilities,
    ModelPricing? Pricing,  // null for local models
    int? ContextLength,
    bool IsLocal);

// Ferret.Core.Models — ModelCapabilities.cs
public sealed record ModelCapabilities(
    bool SupportsStreaming,
    bool SupportsTools,
    bool SupportsStructuredOutput,
    bool SupportsVision,
    int MaxContextTokens,
    int MaxOutputTokens);
```

### 9.2 Provider Implementations

Each provider lives in its own project with no cross-dependencies:

```
Ferret.Models.Ollama         ← localhost HTTP, local-first, no API key
Ferret.Models.OpenAI         ← OpenAI + compatible API (OpenRouter, Groq, Together)
Ferret.Models.Anthropic      ← Anthropic direct API
Ferret.Models.AzureOpenAI    ← Azure OpenAI (enterprise, data residency)
Ferret.Models.LmStudio       ← LM Studio local server
Ferret.Models.Groq           ← Groq Cloud (high-throughput inference)
Ferret.Models.Mistral        ← Mistral API
```

The Ollama provider must be the reference implementation — the one every other provider's architecture is compared against. It has no authentication requirement, runs locally, and demonstrates that the interface works without any external service dependency.

### 9.3 The IModelProvider Registry

```csharp
// Ferret.Core.Models — IModelRegistry.cs
public interface IModelRegistry
{
    void Register(IModelProvider provider);
    IModelProvider? GetProvider(string providerId);
    IReadOnlyList<IModelProvider> AllProviders { get; }
    IChatModel? ResolveChatModel(string modelId);
    IEmbeddingModel? ResolveEmbeddingModel(string modelId);
    Task<ModelRegistryHealth> CheckHealthAsync(CancellationToken ct = default);
}
```

### 9.4 Configuration Schema

```json
// .ferret/config/models.json
{
  "$schema": "https://ferret.dev/schemas/models/v1",
  "defaultChatModel": "ollama/llama3.2",
  "defaultEmbeddingModel": "ollama/nomic-embed-text",
  "defaultReranker": null,
  "providers": {
    "ollama": {
      "type": "ollama",
      "baseUrl": "http://localhost:11434",
      "models": ["llama3.2", "nomic-embed-text", "mistral"]
    },
    "openai": {
      "type": "openai",
      "apiKeySecret": "keychain:openai-api-key",
      "models": ["gpt-4o", "gpt-4o-mini", "text-embedding-3-large"]
    },
    "anthropic": {
      "type": "anthropic",
      "apiKeySecret": "keychain:anthropic-api-key",
      "models": ["claude-opus-4-5", "claude-sonnet-4-5"]
    }
  }
}
```

Note: `apiKeySecret` references the system keychain, never a plain-text API key in config. This is a hard requirement, not a recommendation.

---

## 10. Agent Platform Design

### 10.1 The Question: Plugins or Workflows?

The fundamental design question for the Agent Platform is: should agents be implemented as plugins (registered, discovered, isolated) or as workflows (composed pipelines of steps)?

The plugin model has precedent in the existing Connector Platform. It provides isolation, independent testability, and the ability to add new agent types without modifying core code. It also provides the ability to sandbox agents that have potentially dangerous capabilities.

The workflow model is what most AI agent frameworks use (LangChain, LlamaIndex, CrewAI). It is more composable and easier to build simple agents quickly. It is also significantly harder to isolate and test.

The recommendation is a hybrid: **agents are plugins, but their internal structure uses a workflow pattern.**

An agent is registered with `IAgentRegistry` (like connectors are registered with `IConnectorRegistry`). It is discovered and bound at startup. It exposes a defined capability set. But internally, it is implemented as a composed workflow of steps: Plan → Retrieve → Reason → Verify → Synthesise.

### 10.2 Core Interfaces

```csharp
// Ferret.Core.Agents — IAgent.cs
public interface IAgent
{
    string AgentId { get; }
    AgentMetadata Metadata { get; }
    AgentCapabilities Capabilities { get; }
    IAsyncEnumerable<AgentEvent> ExecuteAsync(
        AgentRequest request,
        IAgentContext context,
        CancellationToken ct = default);
}

// Ferret.Core.Agents — IAgentContext.cs
public interface IAgentContext
{
    IWorkspace Workspace { get; }
    ISearchPlatform Search { get; }
    IMemoryStore Memory { get; }
    IModelProvider Models { get; }
    IToolExecutor Tools { get; }
    AgentSession Session { get; }
}

// Ferret.Core.Agents — IPlanner.cs
public interface IPlanner
{
    Task<TaskGraph> PlanAsync(
        string goal,
        IAgentContext context,
        CancellationToken ct = default);
}

// Ferret.Core.Agents — ITaskGraph.cs  
public sealed class TaskGraph
{
    public IReadOnlyList<AgentTask> Tasks { get; init; }
    public IReadOnlyList<TaskDependency> Dependencies { get; init; }
    public ExecutionStrategy Strategy { get; init; } // Sequential | Parallel | Adaptive
}

// Ferret.Core.Agents — IToolExecutor.cs
public interface IToolExecutor
{
    IReadOnlyList<ToolDescriptor> AvailableTools { get; }
    Task<ToolResult> ExecuteAsync(
        ToolCall call,
        CancellationToken ct = default);
}
```

### 10.3 Built-In Agent Types

```
Ferret.Agent.Ask         ← Single-turn Q&A over workspace knowledge
Ferret.Agent.Architect   ← Architecture design and analysis
Ferret.Agent.Review      ← Code and architecture review
Ferret.Agent.Fix         ← Issue diagnosis and fix suggestion
Ferret.Agent.Watch       ← Continuous workspace monitoring
```

Each agent is a separate project. Each has its own tests. No agent depends on another.

### 10.4 Agent Safety Model

Agents must operate within a declared capability boundary. An agent that only needs to read workspace documents should not have the capability to write to the workspace or execute shell commands. This is the principle of least privilege applied to agents.

```csharp
// Ferret.Core.Agents — AgentCapabilities.cs
public sealed record AgentCapabilities(
    bool CanReadWorkspace,
    bool CanWriteWorkspace,       // dangerous — requires explicit opt-in
    bool CanExecuteTools,         // dangerous — requires explicit opt-in
    bool CanCallModels,
    bool CanReadMemory,
    bool CanWriteMemory,
    bool CanAccessNetwork,        // dangerous — requires explicit opt-in
    IReadOnlyList<string> AllowedToolIds);
```

---

## 11. Prompt Platform Design

### 11.1 Why a Prompt Platform?

Prompt engineering is a discipline, not a hack. Organisations that treat prompts as throwaway strings discover, after twelve months of production use, that they have hundreds of undocumented, version-untracked, unreviewed prompt strings scattered across their codebase. Changing any one of them changes AI behaviour in unknown ways. Rolling back a bad prompt is impossible because there is no version history. Evaluating prompt quality is impossible because there is no baseline.

The Ferret Prompt Platform treats prompts as first-class engineering artefacts: versioned, composed, tested, and deployable independently of model upgrades.

### 11.2 Prompt Template Design

```
.ferret/prompts/
  ask/
    system.prompt        ← system prompt template
    retrieval.prompt     ← retrieval prompt template
    synthesis.prompt     ← answer synthesis template
  review/
    security.prompt
    architecture.prompt
  architect/
    design.prompt
    analysis.prompt
```

Prompts are text files with structured variable injection. They are not code. They are not embedded in code. They are loaded at runtime and version-tracked in the workspace.

```
// Example: .ferret/prompts/ask/synthesis.prompt
---
version: 1
model_constraint: chat
min_context_length: 4096
---
You are Ferret, an enterprise developer intelligence assistant.
Your workspace contains {{workspace_name}} with {{document_count}} indexed documents.

Answer the following question based solely on the context provided.
If the context does not contain sufficient information, say so explicitly.

## Question
{{user_question}}

## Context
{{retrieved_context}}

## Instructions
- Cite specific documents where relevant (use [Source: filename] notation)
- If the question requires reasoning beyond the provided context, state your assumption
- Do not fabricate information not present in the context
```

### 11.3 Context Injection

Context injection is the process of populating prompt templates with retrieved workspace content. This is not string concatenation — it is a structured process with defined rules:

1. Retrieved documents are ranked by relevance score
2. Documents are truncated to fit within the model's context window
3. User-controlled content (document text) is always placed in a clearly delimited section, separate from instruction content
4. Source attribution is embedded with every document excerpt
5. The total constructed prompt never exceeds 80% of the model's context window (20% reserved for output)

### 11.4 Prompt Versioning and Evaluation

```csharp
// Ferret.Core.Prompts — IPromptRegistry.cs
public interface IPromptRegistry
{
    Task<PromptTemplate> GetAsync(
        string promptPath,
        CancellationToken ct = default);
    Task<PromptTemplate> GetVersionAsync(
        string promptPath,
        int version,
        CancellationToken ct = default);
}

// Ferret.Core.Prompts — IPromptEvaluator.cs
public interface IPromptEvaluator
{
    Task<EvaluationResult> EvaluateAsync(
        PromptTemplate prompt,
        IReadOnlyList<EvaluationCase> cases,
        IChatModel model,
        CancellationToken ct = default);
}
```

Prompt evaluation uses a set of golden test cases: input context, expected output characteristics (not exact text — AI output is non-deterministic), and evaluation criteria. This is the equivalent of unit tests for prompts. It is run when prompts change, when models change, and before production deployments.

---

## 12. Model Routing Design

### 12.1 Why Routing Matters

Running every AI request through the most capable model available is neither necessary nor economical. At enterprise scale, intelligent routing is cost management, not premature optimisation.

The routing principle: use the smallest model capable of producing the required quality for each specific task type.

### 12.2 Routing Tiers

```
Tier 1 — Local, Fast, Free
  Model: Ollama/llama3.2 (7B or 13B)
  Use cases: simple Q&A, document classification, short summarisation
  Latency: 500ms–3s (GPU-dependent)
  Cost: compute only

Tier 2 — Cloud, Capable, Economical
  Model: GPT-4o-mini, Claude Haiku 3.5, Mistral Small
  Use cases: multi-document synthesis, structured output, entity extraction
  Latency: 1s–5s
  Cost: ~$0.001–0.005 per operation

Tier 3 — Cloud, Premium, Expensive
  Model: Claude Opus 4.5, GPT-4o, Gemini Ultra
  Use cases: architecture analysis, complex reasoning, security review
  Latency: 5s–30s
  Cost: ~$0.05–0.50 per operation
```

### 12.3 Routing Interface

```csharp
// Ferret.Core.Models — IModelRouter.cs
public interface IModelRouter
{
    Task<IChatModel> RouteAsync(
        RoutingRequest request,
        CancellationToken ct = default);
}

// Ferret.Core.Models — RoutingRequest.cs
public sealed record RoutingRequest(
    RoutingHint Hint,       // Speed | Balance | Quality | LocalOnly
    TaskType TaskType,      // QA | Summary | Analysis | Generation | Review
    int EstimatedInputTokens,
    int RequiredOutputTokens,
    bool RequiresStructuredOutput,
    bool RequiresTools,
    bool MustBeLocal);      // true for air-gapped environments
```

### 12.4 Routing Strategy Examples

```
ferret ask "what is our auth strategy?"
  → TaskType: QA, Hint: Balance, InputTokens: ~2000
  → Route: Tier 1 (local Llama) or Tier 2 (GPT-4o-mini)
  → Rationale: Simple retrieval + synthesis, no complex reasoning

ferret architect "design a caching layer"
  → TaskType: Generation, Hint: Quality, InputTokens: ~8000
  → Route: Tier 3 (Claude Opus or GPT-4o)
  → Rationale: Architecture design requires sophisticated reasoning

ferret review src/Api/Auth/ --security
  → TaskType: Review, Hint: Quality, RequiresTools: true
  → Route: Tier 3 (Claude Opus)
  → Rationale: Security review requires deep, careful reasoning; errors are costly

ferret watch (background agent)
  → TaskType: Classification/Detection, Hint: Speed
  → Route: Tier 1 (local) always
  → Rationale: Continuous background monitoring must not generate ongoing cloud costs
```

---

## 13. Memory Platform Design

### 13.1 The Three-Tier Memory Model

The memory architecture is already defined in FUTURE-001. This section elaborates the design specifically for the Intelligence Platform use case.

```
Working Memory (Session-Scoped)
─────────────────────────────────
What happened in this session:
- Queries asked and answers given
- Documents retrieved and their relevance scores
- Model calls made and their costs
- User feedback on answer quality
- Current conversation context window

Storage: .ferret/memory/working/{session-id}/
Retention: Cleared on session end (configurable)
Purpose: In-session context continuity

Episodic Memory (Cross-Session)
─────────────────────────────────
What happened in previous sessions:
- Session summaries (distilled from working memory)
- Queries that were answered well/poorly
- Frequently accessed documents
- User-expressed preferences and corrections

Storage: .ferret/memory/episodic/
Retention: 90 days default (configurable)
Purpose: Cross-session personalisation

Long-Term Memory (Persistent Knowledge)
─────────────────────────────────────────
What is persistently true about this workspace:
- Architectural patterns observed over time
- Known problem areas (frequently queried, frequently changing)
- Validated facts extracted from episodic memory
- Organisation-specific terminology and conventions

Storage: .ferret/memory/longterm/
Retention: Until explicitly removed
Purpose: Long-term workspace intelligence
```

### 13.2 Memory as a Service

```csharp
// Ferret.Core.Memory — IMemoryStore.cs (extends FUTURE-001 definition)
public interface IMemoryStore
{
    // Working memory
    Task StoreWorkingAsync(MemoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryEntry>> RecallWorkingAsync(
        MemoryQuery query, CancellationToken ct = default);
    Task ClearWorkingAsync(string sessionId, CancellationToken ct = default);

    // Episodic memory
    Task PromoteToEpisodicAsync(string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryEntry>> RecallEpisodicAsync(
        MemoryQuery query, CancellationToken ct = default);

    // Long-term memory
    Task<IReadOnlyList<MemoryEntry>> RecallLongTermAsync(
        MemoryQuery query, CancellationToken ct = default);
    Task ConsolidateAsync(CancellationToken ct = default); // distil episodic → long-term
}
```

### 13.3 Organisation Memory

For enterprise deployments, memory is not per-user — it is per-organisation. Insights one developer discovers should be accessible to colleagues with appropriate permissions. This requires a shared memory layer above the individual workspace.

```
Local (per-developer):    .ferret/memory/
Shared (per-team):        .ferret-shared/memory/  (synced via git or shared filesystem)
Enterprise (per-org):     Ferret Hub / Remote Store (V3)
```

The local memory architecture must be designed from the start to be promotable to shared and enterprise layers without architectural change. The interface (`IMemoryStore`) is the same at all levels. The backing store is different.

---

## 14. Enterprise Intelligence Evolution

### 14.1 Decision Intelligence

Once Ferret has indexed an organisation's decisions (ADRs, meeting notes, architecture documents) and built a knowledge graph of their relationships, embedded AI enables a new capability: decision intelligence.

Decision intelligence is the ability to:
- Retrieve all decisions relevant to a proposed change
- Identify conflicts between a proposed decision and existing decisions
- Trace the history of a decision and understand why it was made
- Predict the impact of reversing a decision

This is qualitatively different from search. It requires reasoning over a graph of decisions, not just retrieving documents that mention decision-related keywords.

### 14.2 Architecture Intelligence

Architecture intelligence enables Ferret to understand not just the current architecture, but the trajectory of the architecture over time:
- Is the codebase moving toward or away from its documented target architecture?
- Where are the highest-risk areas of architectural drift?
- Which modules are most likely to require refactoring in the next quarter based on current change velocity?

### 14.3 Security Intelligence

Security intelligence applies embedded AI reasoning to security concerns:
- Detect patterns that historically precede security incidents
- Cross-reference code changes against known vulnerability patterns (not just CVE matching — pattern reasoning)
- Identify when dependency updates introduce transitive vulnerability exposure

### 14.4 Developer Intelligence

Developer intelligence is proactive assistance for individual developers:
- "You are about to modify `AuthService.cs`. This file has been changed 23 times in the last month by 4 different developers. Three of those changes introduced bugs that were reverted. These are the patterns that caused problems."
- "The test coverage for the code you just modified is 47%. The 5 most likely failure modes are..."

---

## 15. CLI Evolution

### 15.1 Current CLI (Option A)

```
ferret workspace init       ← create workspace
ferret workspace status     ← show workspace state
ferret connector list       ← list registered connectors
ferret connector info       ← connector details
ferret index                ← index workspace
ferret search <query>       ← keyword search
ferret mcp serve            ← start MCP server
```

### 15.2 Intelligence CLI (Option B)

```
ferret ask <question>       ← Q&A over workspace knowledge
ferret architect <spec>     ← architecture design and analysis
ferret review <path>        ← code and architecture review
ferret fix <issue>          ← issue diagnosis and fix suggestion
ferret explain <path>       ← explain code or documentation
ferret compare <a> <b>      ← compare two documents or components
ferret watch                ← start continuous workspace monitoring
ferret model list           ← list available models
ferret model test           ← test model connectivity
ferret model route          ← show routing decisions for a query (debug)
ferret prompt list          ← list registered prompt templates
ferret prompt eval          ← evaluate prompt quality against test cases
ferret memory show          ← show current memory contents
ferret memory clear         ← clear working memory
ferret agent list           ← list registered agents
ferret agent run <id>       ← run a specific agent
```

### 15.3 The MCP Surface Evolution

Option B does not eliminate the MCP surface — it expands it. New MCP tools are exposed:

```
# Existing MCP tools (Option A)
ferret:search              ← keyword search
ferret:get_document        ← retrieve specific document
ferret:list_connectors     ← list connector status

# New MCP tools (Option B)
ferret:ask                 ← Q&A with embedded reasoning
ferret:summarise           ← summarise a document or topic
ferret:review              ← code review via embedded AI
ferret:explain             ← explain a piece of code
ferret:find_similar        ← semantic similarity search
ferret:get_memory          ← retrieve session memory
ferret:architect           ← architecture analysis
```

The external AI consumer can use any of these. A Claude Code user gets richer Ferret tools. A custom agent can use the intelligence tools without any external AI of their own.

---

## 16. Deployment Modes

### 16.1 Local Developer (Developer Workstation)

```
Model: Ollama (local)
Storage: .ferret/ in project directory
Connectors: Filesystem (primary), Git (secondary)
Memory: Working + Episodic (local disk)
AI Features: All, bounded by local GPU
Network: None required
Cost: Compute only (local GPU electricity)
Setup: Install Ferret + Install Ollama + Pull model (~5GB)
```

This is the baseline deployment. Every intelligence feature must work in this mode. If a feature requires a cloud model, it must degrade gracefully to no-AI behaviour when only Ollama is configured and the required model is too large.

### 16.2 Small Team (5–20 Developers)

```
Model: OpenAI or Anthropic (cloud, shared API key)
Storage: .ferret/ committed to shared git repository
Connectors: Filesystem, Git, GitHub, JIRA
Memory: Working (local) + Episodic (shared via git) + Long-term (shared via git)
AI Features: All, including cloud-tier reasoning
Network: HTTPS to model provider APIs
Cost: Per-token billing (shared, attributed per-developer)
Setup: Ferret config with shared API key (stored in team keychain)
```

For small teams, the shared git repository for `.ferret/` config and memory is a natural collaboration mechanism. It requires no additional infrastructure.

### 16.3 Enterprise (100+ Developers)

```
Model: Azure OpenAI (data residency) + Ollama fleet (background tasks)
Storage: Ferret Hub (centralised, multi-tenant)
Connectors: All connectors, enterprise-grade credentials in Vault
Memory: All tiers, with RBAC-controlled sharing
AI Features: All, with enterprise controls
Network: Internal VPN only (model endpoints are on-prem or Azure with VNet)
Cost: Per-user licensing + inference cost allocation by team
Security: RBAC, audit log, prompt injection filtering, output monitoring
Setup: Enterprise deployment package (Helm chart or MSI + Group Policy)
```

Enterprise deployment requires Ferret Hub — a centralised service that manages workspace metadata, shared memory, telemetry, and billing. Ferret Hub is out of scope for V2 but the local architecture must be designed to accommodate it.

### 16.4 Air-Gapped (Classified or Regulated)

```
Model: Ollama (on-premises GPU servers) only
Storage: Local .ferret/ only (no external sync)
Connectors: Filesystem, Git, internal services only
Memory: Local, never synced
AI Features: All features available with local models
Network: Zero outbound (enforced at OS level)
Cost: On-premises GPU infrastructure
Setup: Private Ollama deployment + Ferret with network.policy: isolated
```

Air-gapped mode is not a fallback — it is a first-class deployment target. The air-gapped enterprise is a real customer with a real use case. Ferret's local-first architecture makes it viable. Any design decision that breaks air-gapped operation is unacceptable.

### 16.5 CI/CD Integration

```
Model: Ollama (ephemeral, started per pipeline) or cloud (if allowed)
Storage: .ferret/ from repository checkout
Connectors: Git (primary), test report outputs
Memory: None (stateless per run)
AI Features: Review and validation only (no proactive agents)
Network: Pipeline network policies apply
Cost: Per-run inference (billable per CI job)
Use case: Automated architecture review gates, test impact analysis
```

CI/CD integration is a V2 feature. The stateless nature of CI runs means no persistent memory and no background agents. Only on-demand intelligence features apply.

### 16.6 Cloud (Ferret Hub, V3)

```
Model: Provider routing managed by Ferret Hub
Storage: Ferret Hub (multi-tenant, geo-replicated)
Connectors: All connectors, OAuth-managed credentials
Memory: All tiers, cloud-backed
AI Features: Full enterprise intelligence suite
Network: Cloud-native, Ferret Hub as central service
Cost: SaaS subscription + inference
Use case: Teams who want zero-infrastructure Ferret
```

Cloud deployment is not V1 or V2. It requires Ferret Hub, which is a separate product. This document notes it as an eventual deployment mode without committing to its design.

---

## 17. Should AI Be Optional?

### 17.1 The Answer Is Yes. Always.

This is the most important architectural question in this document, and the answer must be unambiguous: **AI must be optional in every version of Ferret, in every deployment mode, forever.**

This is not about supporting edge cases. It is about the fundamental nature of the product.

A developer tool that requires AI to function is not a developer tool — it is an AI service with a developer interface. Ferret's value proposition is that it organises, indexes, and makes available enterprise knowledge. That value is real without AI. The AI makes it better. The AI is not what makes it valuable.

### 17.2 The Invariant

```
Ferret.Core                → zero AI dependency (enforced)
Ferret.ConnectorPlatform   → zero AI dependency (enforced)
Ferret.IndexPlatform       → zero AI dependency (enforced, except Ferret.Index.Semantic)
Ferret.SearchPlatform      → zero AI dependency (enforced)
Ferret.McpServer           → zero AI dependency (enforced)
Ferret.Knowledge           → zero AI dependency (enforced)
Ferret.Memory              → zero AI dependency (enforced)

Ferret.Index.Semantic      → depends on IEmbeddingModel (explicit, opt-in)
Ferret.Agent.*             → depends on IModelProvider (explicit, opt-in)
Ferret.Prompt              → depends on IModelProvider (explicit, opt-in)
Ferret.Routing             → depends on IModelProvider (explicit, opt-in)
```

The `(enforced)` notation means this is verified by ArchUnit tests in the build pipeline, not just stated in documentation.

### 17.3 Degradation Contract

When AI features are requested but no model is configured:

- `ferret ask` → returns: "AI features require a configured model. Run `ferret model list` to see available providers, or configure Ollama for local-first AI."
- `ferret search` → works normally (no AI required)
- `ferret index` → works normally (keyword index only; semantic index skipped with warning)
- `ferret mcp serve` → works normally; AI-backed MCP tools return empty results with capability explanation

This degradation is explicit, not silent. A user who has not configured AI should never see a silent failure — they should see a clear, actionable message explaining what is needed.

### 17.4 Test Suite Requirement

The test suite must always include a "no-AI" integration test suite that runs entirely without any model configured. If any test in this suite requires a model, it is a regression. This suite runs in CI without any model infrastructure. It validates that the Context OS capabilities are never broken by Intelligence Platform changes.

---

## 18. Future Vision

### 18.1 From Context OS to Enterprise Intelligence Platform

The evolution is not a revolution. It is a series of disciplined additions to a stable foundation.

```
V1 (Current): Context Operating System
  "Ferret knows everything about your codebase and makes it available to your AI tools."

V2: Enterprise Intelligence Platform (Basic)
  "Ferret answers your questions about your codebase with grounded, cited answers."

V3: Enterprise Intelligence Platform (Full)
  "Ferret monitors your workspace, detects problems before you find them, and guides
   your architectural decisions with knowledge of your entire organisation's history."

V4: Operating System for Enterprise Intelligence
  "Ferret is the platform on which your organisation's AI-assisted engineering
   workflows are built. It is not a tool you use — it is the foundation that all
   your developer tools run on."
```

### 18.2 The V4 Vision

V4 is a 5–10 year horizon. It is worth describing to understand whether the current architecture foreclosures it.

In V4, Ferret is not a CLI tool or an MCP server. It is a platform service — an always-running intelligence layer that every developer tool in the organisation integrates with. It knows more about the organisation's codebase than any individual developer does. It has memory spanning years. It has seen every architectural decision, every bug pattern, every deployment failure.

Developers interact with it through the tools they already use — their IDE, their PR review tool, their planning software. Ferret is invisible as infrastructure but omnipresent as capability. When a developer starts typing in their IDE, Ferret has already retrieved the relevant context and pre-positioned the relevant knowledge. When an architect proposes a design, Ferret has already validated it against ten years of accumulated organisational knowledge.

This is not fiction — it is the natural endpoint of the trajectory Ferret is already on. The question is whether today's architecture decisions foreclose V4. The answer: they do not, as long as the Core remains zero-dependency, the Intelligence Layer remains optional, and the Memory Platform is designed for long-term persistence from the beginning.

---

## 19. Suggested New Platform Modules

### 19.1 Namespace Structure

```
Ferret.Core.Models            ← model contracts (IModelProvider, IChatModel, etc.)
Ferret.Core.Agents            ← agent contracts (IAgent, IPlanner, ITaskGraph, etc.)
Ferret.Core.Prompts           ← prompt contracts (IPromptTemplate, IPromptRegistry, etc.)

Ferret.Models.Ollama          ← Ollama provider (local-first)
Ferret.Models.OpenAI          ← OpenAI + OpenAI-compatible (Groq, Together, OpenRouter)
Ferret.Models.Anthropic       ← Anthropic direct
Ferret.Models.AzureOpenAI     ← Azure OpenAI (enterprise data residency)
Ferret.Models.LmStudio        ← LM Studio local server
Ferret.Models.Mistral         ← Mistral API
Ferret.Models.vLlm            ← vLLM (self-hosted, enterprise GPU)

Ferret.ModelPlatform          ← registry, routing, health monitoring, cost tracking

Ferret.Prompt                 ← template engine, context injection, composition, evaluation

Ferret.Index.Semantic         ← vector index (depends on IEmbeddingModel)
Ferret.Index.Graph            ← property graph index (no AI dependency)

Ferret.Agent.Ask              ← Q&A agent
Ferret.Agent.Architect        ← architecture analysis agent
Ferret.Agent.Review           ← code and architecture review agent
Ferret.Agent.Fix              ← issue diagnosis and fix agent
Ferret.Agent.Watch            ← continuous monitoring agent

Ferret.AgentPlatform          ← agent registry, session manager, tool executor, safety

Ferret.Evaluation             ← prompt and agent evaluation framework
```

### 19.2 Dependency Graph Extension

```
Ferret.Core.Models            no external dependencies (part of Core boundary)
Ferret.Core.Agents            depends on: Ferret.Core, Ferret.Core.Models
Ferret.Core.Prompts           depends on: Ferret.Core, Ferret.Core.Models

Ferret.Models.*               depends on: Ferret.Core.Models (interface only)
Ferret.ModelPlatform          depends on: Ferret.Core.Models, Ferret.Models.*

Ferret.Prompt                 depends on: Ferret.Core.Prompts, Ferret.ModelPlatform,
                                           Ferret.Memory

Ferret.Index.Semantic         depends on: Ferret.Core.Indexing, Ferret.Core.Models

Ferret.Agent.*                depends on: Ferret.Core.Agents, Ferret.Prompt,
                                           Ferret.ModelPlatform, Ferret.SearchPlatform

Ferret.AgentPlatform          depends on: Ferret.Core.Agents, Ferret.Agent.*

Ferret.Evaluation             depends on: Ferret.Core.Models, Ferret.Core.Agents,
                                           Ferret.Prompt, Ferret.ModelPlatform

Ferret.Cli                    depends on: Ferret.AgentPlatform, Ferret.ModelPlatform,
                                           Ferret.Prompt (via DI, all optional)
```

---

## 20. Suggested ADRs

The following ADRs should be drafted during the review period. They represent decisions that will foreclose or enable architectural options if made implicitly.

**ADR-0016 — Model Platform Abstraction Strategy**
Decides whether model provider abstraction is at the interface level (recommended), the HTTP client level (insufficient), or the SDK level (lock-in risk). Establishes the `IModelProvider` interface as the canonical boundary and prohibits direct SDK imports outside `Ferret.Models.*` projects.

**ADR-0017 — AI Feature Opt-In and Degradation Contract**
Formally records the invariant that AI features are always optional and defines the degradation behaviour for all AI-dependent commands when no model is configured. Establishes the "no-AI" test suite as a CI requirement.

**ADR-0018 — Local-First Model Requirement**
Establishes that the first model provider implemented is Ollama (local), not a cloud provider. Formalises the requirement that all AI features must be functional with a local model before cloud providers are added.

**ADR-0019 — Prompt Engineering as First-Class Artefact**
Decides that prompt templates are stored as versioned files in `.ferret/prompts/`, not embedded in code. Establishes the prompt evaluation framework as a required component of any prompt change.

**ADR-0020 — Agent Capability Sandboxing**
Establishes the principle of least privilege for agents: each agent declares its required capabilities, and the agent runtime enforces those boundaries. Agents without explicit write-workspace capability cannot modify workspace state.

**ADR-0021 — Prompt Injection Threat Model**
Documents the prompt injection attack vector for RAG-based systems and establishes the mitigation requirements: input sanitisation, instruction/content separation in prompts, and agent capability restrictions. Makes prompt injection mitigation a required engineering task for any agent that processes indexed content.

**ADR-0022 — Model Routing Tiers**
Defines the three routing tiers (local/economical/premium) and the criteria for routing decisions. Establishes that background agents must use local-only routing to prevent unbounded cloud inference costs.

**ADR-0023 — Memory Persistence and Retention Policy**
Decides the retention policies for working, episodic, and long-term memory. Establishes the consolidation process (episodic → long-term distillation) and the schema for memory entries. Addresses GDPR/privacy implications of persisting conversation history.

**ADR-0024 — Semantic Index as Opt-In Feature**
Establishes that the semantic index (vector embeddings) is opt-in, requires explicit model configuration, and must not prevent keyword index from functioning. The keyword index is always the default. The semantic index is an enhancement.

**ADR-0025 — Enterprise Deployment and Air-Gap Support**
Formalises the air-gap deployment as a first-class target. Establishes that any feature that requires outbound network connectivity must be explicitly gated and must not be called in air-gap mode. Network isolation must be configurable at the workspace level, not just through OS-level network policies.

---

## 21. Open Questions

These questions are not resolved by this document. They require further investigation, prototype work, or stakeholder input before decisions can be made.

**Q1: What is the right granularity for prompt templates?**
Should there be one prompt per command (`ask.prompt`, `review.prompt`) or one prompt per reasoning step (`retrieval.prompt`, `synthesis.prompt`, `validation.prompt`)? Finer granularity allows more targeted optimisation but increases coordination complexity.

**Q2: How should model cost be attributed in enterprise multi-user deployments?**
Options: per-user billing (simplest), per-team billing (better for cost centre allocation), per-project billing (most granular), or Ferret Hub managed (V3). The V1/V2 architecture should not foreclose any of these.

**Q3: Should the Agent Platform support long-running background processes, or only on-demand agents?**
`ferret watch` implies a continuously running background agent. This requires a process management model that does not currently exist. Should this use a separate process (agent daemon), or is it a foreground process with a different lifecycle?

**Q4: How should conflicting information across connectors be handled by embedded AI?**
If the JIRA connector and the Confluence connector provide contradictory information about the same topic, which does the AI prefer? Should Ferret expose this conflict, or should it pick one source? This is a significant information architecture question.

**Q5: What is the privacy boundary for organisation memory?**
Long-term memory may contain information about individual developers' coding patterns, mistake histories, and skill gaps. Who owns this data? Can developers delete their own memory entries? Can managers access team memory? This requires a privacy model before V3.

**Q6: Should Ferret implement its own vector database, or use an embedded library?**
Options for the semantic index: SQLite with the `vector` extension, DuckDB with VSS, Qdrant embedded, or a purpose-built simple implementation. The choice affects: performance, deployment complexity, upgrade path, and dependency surface.

**Q7: How does the Evaluation Framework integrate with CI/CD?**
Should `ferret prompt eval` be a CI gate (failing CI if prompt quality regresses)? This requires deterministic evaluation — which is fundamentally at odds with non-deterministic model outputs. The framework needs a solution (golden baselines, fuzzy matching, or sampling-based statistical evaluation).

**Q8: Is Ferret Hub a separate product or part of Ferret?**
The enterprise memory sharing and centralised management implied by V3 require infrastructure that is not a local CLI. Should this be a separate commercial product (Ferret Hub) or part of the open-source Ferret with a managed cloud option? This decision affects the business model, not just the architecture.

---

## 22. Items Deferred to Future Versions

**Deferred to V2:**
- Cloud model providers (OpenAI, Anthropic, Azure OpenAI) — local-first first
- Multi-user agent sessions
- `ferret watch` continuous monitoring (background agent daemon)
- Semantic index (vector embeddings) — keyword-first, semantic second
- Agent-to-agent communication (multi-agent pipelines)
- Evaluation framework (beyond basic prompt testing)

**Deferred to V3:**
- Ferret Hub (centralised enterprise management)
- Organisation-level memory and knowledge sharing
- RBAC for knowledge graph and memory access
- Audit logging for AI operations
- Multi-tenant enterprise deployment
- Cost allocation and billing infrastructure
- Proactive intelligence features (drift detection, risk prediction)
- Privacy management for organisation memory (GDPR, deletion, export)

**Deferred indefinitely (requires further validation):**
- AI-generated ADRs (human review must remain mandatory)
- Autonomous code modification (agents that write code without human review)
- Cross-organisation knowledge sharing (multi-company Ferret Hub)
- Model fine-tuning on organisation-specific data (significant privacy and cost implications)

**Items that should NOT be deferred:**
- Model platform contract design (`Ferret.Core.Models` interfaces) — Sprint 13
- Air-gap mode specification (ADR-0025) — before any cloud provider work
- Prompt injection threat model (ADR-0021) — before any agent that processes indexed content
- "No-AI" test suite — Sprint 13 (must exist before AI features are added)

---

## 23. Final Recommendation

### 23.1 The Verdict

**Ferret should embed AI. The architecture supports it. The market demands it. But not yet, and not without conditions.**

The current architecture is well-designed for this evolution. The zero-dependency Core, the interface-first design, the connector isolation model — these are exactly the properties that make an intelligence layer composable rather than coupled. Ferret has not foreclosed Option B by building Option A. This is a genuine competitive advantage over tools that embedded AI from the start and are now discovering the limitations of that choice.

### 23.2 The Conditions

The recommendation comes with four non-negotiable conditions:

**Condition 1: Complete the Context OS first.**
The Index Platform, Search Platform, and MCP Server must be production-quality before any intelligence work begins. Sprint 13 is the earliest reasonable point to start Model Platform design. Sprint 14 is the earliest for any implementation. Any earlier risks building intelligence on a shaky foundation.

**Condition 2: `Ferret.Core` never gains an AI dependency.**
This is an architectural invariant that must be enforced by automated tests, not just stated in documentation. If a PR introduces an AI dependency in Core, it must be rejected automatically, not just flagged in code review.

**Condition 3: Local-first model support before cloud.**
Ollama must be the first and reference implementation. Every AI feature must work with Ollama before any cloud provider is added. This guarantees air-gap support, avoids vendor dependency, and keeps the development feedback loop fast (no API costs during development).

**Condition 4: AI features must be opt-in, and degradation must be explicit.**
A developer who has not configured a model must get clear, actionable guidance — not a cryptic error. The system must degrade gracefully, not catastrophically. The "no-AI" integration test suite must run in CI and must never break.

### 23.3 What Will Happen If These Conditions Are Ignored

If Ferret embeds AI before completing the Context OS: the intelligence layer inherits the instability of an immature retrieval layer, and every AI answer will be wrong in ways that are hard to diagnose (because the cause is retrieval quality, not reasoning quality).

If `Ferret.Core` gains an AI dependency: air-gap deployments are broken permanently, and the regulated-industry market is lost.

If cloud-first model support is added before local: Ferret will never work in air-gapped environments, development will require API costs, and the feedback loop for feature development will be slow and expensive.

If AI features are not opt-in: enterprise customers who need approval for AI vendor onboarding will be unable to deploy Ferret at all until they complete procurement processes that can take 6–18 months. This eliminates the initial land-and-expand strategy.

### 23.4 The Opportunity

If these conditions are met, Ferret can become something that does not currently exist: an enterprise intelligence platform that is genuinely local-first, air-gap capable, provider-agnostic, and built on a Context OS that makes AI answers reliably grounded in real organisational knowledge.

GitHub Copilot is smart but workspace-blind. Claude Code is capable but ephemeral — it has no memory of yesterday's conversation. Cursor knows your open files but not your organisation's five years of architectural decisions. Ferret, done right, knows everything. It has been indexing your workspace since day one. It remembers what was decided and why. It can tell you, with citations, whether the thing you are about to build has been tried before and what happened.

That is a product that enterprise development teams will pay for, and cannot easily replace. That is the option B worth building toward.

---

*This document is an architectural exploration. It does not commit the Ferret project to any specific implementation. All interface definitions, namespace proposals, and ADR titles are indicative of the recommended direction, not finalised contracts. Decisions require formal ADRs. Implementation requires sprint planning.*
