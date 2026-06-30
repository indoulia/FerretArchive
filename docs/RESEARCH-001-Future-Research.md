# RESEARCH-001 — Future Research

| Field | Value |
|---|---|
| **Document ID** | RESEARCH-001 |
| **Version** | 1.0 |
| **Status** | Research Backlog |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-28 |

> **These are research items, not roadmap commitments.** Each item requires a prototype and evaluation before it can be promoted to the roadmap. Research items may be promoted, deferred indefinitely, or abandoned based on findings.

---

## How Research Items Are Managed

1. **Proposed** — Idea captured in this document
2. **Active** — A prototype or spike is underway (referenced in sprint plan)
3. **Evaluated** — Findings written up; decision: promote to roadmap / defer / abandon
4. **Promoted** — Moved to ROADMAP-002 and/or an ADR
5. **Abandoned** — Not worth pursuing; reason recorded here

---

## AI Learning

**Status:** Proposed | **Suggested Version:** V4

**Description:** Ferret observes the team's decisions over time — which PR comments are acted on, which refactors get reverted, which ADRs are superseded — and adapts its recommendations to match the organization's actual engineering standards rather than generic best practices.

**Business Value:** The longer Ferret runs, the more useful it becomes. Institutional knowledge that lives in engineers' heads becomes encoded in the knowledge graph and fed back as context.

**Research Questions:**
- What signals are reliable indicators of "good" vs "bad" decisions in a codebase?
- How do we distinguish domain-specific patterns from anti-patterns?
- How do we prevent "learning" from encoding the team's bad habits?
- What model fine-tuning approach is appropriate for local on-device learning?

**Architectural Impact:** Requires long-term memory store (`Ferret.Memory.LongTerm`), feedback loop from review outcomes to knowledge graph, and a model that can be updated without a full retrain.

**Dependencies:** V2 Knowledge Graph, V3 Enterprise review pipeline

---

## Digital Twin

**Status:** Proposed | **Suggested Version:** V4

**Description:** A continuously-updated, queryable model of the entire software system — not just code, but people, processes, decisions, and history. Every change to the system is reflected in the twin within minutes.

**Business Value:** The Digital Twin answers questions that require correlating multiple data sources: "Who are the three people who understand this service?" or "What is the blast radius of removing this dependency?"

**Research Questions:**
- What data model represents a "software system" comprehensively?
- How do we handle rapidly changing graphs (every commit is a delta)?
- What query language is most expressive for twin queries?
- How do we represent uncertainty (things we don't know about the system)?

**Architectural Impact:** Requires a property graph database (see ARCH-017), a real-time change propagation system (connectors push deltas), and a query interface (`ferret twin query`).

**Dependencies:** V2 Connector Framework, V3 Knowledge Graph

---

## Enterprise Time Machine

**Status:** Reserved (architecture in place at Sprint 7) | **Suggested Version:** V3.5

**Description:** Snapshot and replay the complete enterprise knowledge state at any point in time. Answer questions as of a past date: "What did the team know the day before the outage?"

**Research Questions:**
- What is the minimum snapshot data needed to reconstruct full context at a point in time?
- How do we handle large index data efficiently (incremental snapshots)?
- What is the right UX for time-travel queries in the CLI?
- How do we correlate git history with knowledge history?

**Architectural Impact:** `.ferret/snapshots/` reserved in Sprint 7. Snapshot strategy: copy of workspace state files + index manifests, tagged to git commit hash. Full index snapshotting is optional (disk-intensive).

**Dependencies:** V1 Workspace Engine (Sprint 7), V2 Full Indexing, V2 Knowledge Graph

**See also:** `ARCH-017-Storage-Architecture.md` §8 Snapshot Storage

---

## Context Compression

**Status:** Proposed | **Suggested Version:** V2

**Description:** When a context window exceeds the token limit, automatically compress it: summarize older, less-relevant content; retain verbatim the most recently accessed and highest-relevance content; track what was summarized so it can be retrieved if queried directly.

**Business Value:** Longer working sessions without hitting token limits. Better context assembly for large codebases.

**Research Questions:**
- What compression ratio is achievable without significant relevance loss?
- How do we measure relevance in the context of a specific query?
- Should compression be lossy (summarize) or lossless (chunk and index the overflow)?
- What models are best for selective summarization of technical content?

**Architectural Impact:** New `IContextCompressor` contract in `Ferret.Core`. New analytics event `ContextCompressed`. Compressed context history stored in context.db.

**Dependencies:** V2 Context Engine, V2 Semantic Index

---

## Knowledge Graph (Property Graph)

**Status:** Architecture reserved | **Suggested Version:** V2

**Description:** A queryable property graph over all engineering entities (services, functions, engineers, decisions, tickets, documents) and their relationships. The knowledge graph is the backbone of ContextOS.

**Research Questions:**
- Which embedded .NET graph database is most suitable? (LiteGraph, custom JSON graph, SQLite graph extension)
- What is the right query API? (Gremlin, Cypher subset, custom LINQ-like API)
- How do we handle schema evolution as new entity types are added?
- What is the memory footprint for a 10M-node graph in embedded mode?

**Candidates to evaluate:**
- **Custom JSON-LD graph** — zero dependency, full control, limited query capability
- **LiteGraph** — lightweight embedded graph DB for .NET
- **SQLite graph extension** — query via SQL with graph primitives
- **DGraph embedded** — powerful but complex

**Architectural Impact:** `Ferret.Knowledge` project. `IKnowledgeEngine`, `IEntity`, `IRelationship` contracts. Knowledge stored in `.ferret/knowledge/`.

**Dependencies:** V2 Connector Framework (entities come from connectors)

---

## Agent Collaboration

**Status:** Proposed | **Suggested Version:** V4

**Description:** Multiple Ferret agents (running in different processes or on different machines) collaborate on a shared task: one agent indexes the codebase, another answers questions, a third monitors for architectural drift. Agents communicate via the knowledge graph and event bus.

**Research Questions:**
- What is the right coordination primitive for multi-agent workflows in a local-first system?
- How do we handle conflicts when multiple agents write to the knowledge graph simultaneously?
- What is the security model for inter-agent communication?
- How do we debug multi-agent failures?

**Architectural Impact:** Requires a shared knowledge graph with optimistic concurrency control, a message passing protocol between agents, and an agent registry.

**Dependencies:** V2 Knowledge Graph, V3 Enterprise Knowledge Store

---

## Autonomous Planning

**Status:** Proposed | **Suggested Version:** V4

**Description:** Ferret observes the team's velocity, technical debt accumulation, and architectural drift, and automatically suggests what the next sprint should focus on — prioritized by business impact and engineering health.

**Research Questions:**
- What signals are reliable predictors of technical debt accumulation?
- How do we model "engineering health" in a way that produces actionable recommendations?
- What is the right balance between metric-driven and judgment-driven planning suggestions?
- How do we explain the reasoning behind a planning recommendation?

**Architectural Impact:** Requires analytics events (V2.5), knowledge graph (V2), and a planning model that understands sprint velocity and architectural patterns.

**Dependencies:** V2.5 Analytics, V2 Knowledge Graph, V3 Architecture Intelligence

---

## Future AI Models

**Status:** Continuous research | **Suggested Version:** V2+

**Research Topics:**

**Local Embedding Models (V2)**
- `nomic-embed-text` — strong performance, Apache 2.0, runs locally via ONNX
- `all-MiniLM-L6-v2` — smaller, faster, slightly lower quality
- `bge-large-en-v1.5` — best quality, heavier
- Recommendation: configurable per workspace, default `nomic-embed-text`

**Local LLMs for Context Assembly (V2)**
- Phi-3 Mini — Microsoft, 3.8B, excellent instruction following
- Llama 3.2 3B — Meta, strong multilingual
- Gemma 2 2B — Google, good code understanding
- Research question: which model produces the best context assembly quality per token?

**Reranking Models (V2)**
- `cross-encoder/ms-marco-MiniLM-L-12-v2` — standard reranker
- `bge-reranker-large` — strong, heavier
- Purpose: re-rank keyword + semantic results before context assembly

**Code-Specific Models (V3)**
- CodeBERT / GraphCodeBERT — code understanding
- StarCoder 2 3B — code generation
- Research question: do code-specific models improve context quality for code queries vs general models?

---

## Research Governance

- Each research item has an owner when moved to "Active" status.
- Active research items are tracked in the sprint backlog as spike tasks.
- Spike output: a brief findings doc added to this file under the item.
- Promotion to roadmap requires: working prototype, performance benchmark, and ADR (for architectural impact items).

---

## Related Documents

- `ROADMAP-002-Future-Vision.md` — Promoted research items
- `IDEAS.md` — Speculative feature ideas (not yet research-level)
- `TECH-001-Technology-Evaluation.md` — Technology decisions made
- `FUTURE-001-Future-Architecture.md` — Architecture implications of research
