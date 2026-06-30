# ROADMAP-002 — Future Vision (V2–V4)

| Field | Value |
|---|---|
| **Document ID** | ROADMAP-002 |
| **Version** | 1.0 |
| **Status** | Vision Document (not committed roadmap) |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-29 |

---

## Purpose

This document captures the long-horizon product vision for Ferret beyond V1. These are not committed deliverables — they are the direction we are building toward. They inform architecture decisions today (we don't build things that would prevent V3) and help new contributors understand why we made certain choices.

The V1 roadmap is in `ROADMAP-001.md`. This document covers V2–V4.

---

## V2 — Federated ContextOS

**Theme:** Ferret becomes the federated context operating system. RC1 proves the personal platform — one developer, one workspace, one AI assistant. V2 makes that platform collaborative: teams share Knowledge Spaces, mount each other's indexes, compose context from federated sources, and share AI inference without giving up local-first principles.

**Headline: A team can create a shared Knowledge Space, mount it into every developer's local workspace, and ask any question against the collective knowledge of the entire organisation.**

---

### Knowledge Spaces

A **Knowledge Space** is the core V2 product concept. It is not an index — it is a first-class product object that contains everything needed to make a body of knowledge searchable, contextual, and shareable.

| Component | Purpose |
|---|---|
| **Connectors** | Data sources wired to this space (filesystem, Git, JIRA, Confluence, …) |
| **Indexes** | Keyword, semantic, and graph indexes built from connector content |
| **Metadata** | Provenance, freshness timestamps, coverage statistics, connector health |
| **Prompts** | Context assembly templates scoped to this space |
| **AI Configuration** | Model routing, provider selection, embedding model for this space |
| **Permissions** | Who can read, write, mount, and administer this space |
| **Context Policies** | Token budgets, content filters, deduplication rules |

An index is one component of a Knowledge Space, just as a database is one component of a Git hosting service. The product model is the Knowledge Space; the index is an implementation detail that users never need to think about.

**Personal Knowledge Space (RC1 baseline)**

Every `ferret init` creates a personal Knowledge Space in `.ferret/`. RC1 users already have a Knowledge Space — they just don't share it yet. V2 makes sharing a first-class operation.

**Shared Knowledge Spaces**

A shared Knowledge Space lives on a Ferret Hub (team-hosted or cloud-hosted). Any team member can:

- Mount a shared space into their local workspace: `ferret mount <space-url>`
- Contribute connectors: `ferret space add-connector <space-name> <connector>`
- Search across all mounted spaces simultaneously
- Assemble context from multiple spaces in a single `ferret_context` call

---

### V2 Capabilities

**Connectors (Sprint 8+)**

Every engineering data source becomes a first-class Ferret connector. Connectors can be scoped to a personal Knowledge Space or contributed to a shared one:

| Connector | Data |
|---|---|
| Filesystem | Source code, configs, assets |
| Git | Commits, branches, blame, history |
| JIRA | Tickets, epics, sprints, comments |
| GitHub / Azure DevOps | PRs, reviews, issues, CI runs |
| Confluence | Docs, decisions, architecture pages |
| SharePoint | Enterprise documents |
| Slack / Teams | Conversations linked to commits/tickets |
| Logs | Production and CI log streams |

**Indexes (Sprint 9–10)**

Three complementary search indexes — each an internal component of a Knowledge Space:

| Index | Technology | Purpose |
|---|---|---|
| Keyword | Inverted index | Exact search: function names, error codes, identifiers |
| Semantic | Vector embeddings | Meaning-based: "how does authentication work?" |
| Graph | Property graph | Relationship-based: "what depends on this?" |

Indexes are internal to the Knowledge Space. Users interact with spaces, not with indexes directly.

**Knowledge (Sprint 12+)**

A persistent, queryable knowledge layer — scoped per Knowledge Space:

- **Entities** — people, services, systems, features
- **Relationships** — "Person X owns Service Y", "Feature A depends on Service B"
- **Documents** — architecture decisions, runbooks, post-mortems

**Memory (Sprint 14+)**

Three memory tiers scoped to the local workspace, not to a shared space:

| Tier | Purpose |
|---|---|
| Working | Current session: open files, recent queries |
| Episodic | Session history: what was asked, what was answered |
| Long-term | Persistent facts: patterns, conventions, known issues |

---

### Federation Capabilities

**Mounted Workspaces**

A developer's local workspace can mount external Knowledge Spaces alongside its personal one. Mounting is read-only by default. Writes require explicit contributor access.

```
~/.ferret/
  personal/          ← personal Knowledge Space (RC1 baseline)

.ferret/
  workspace.json     ← includes "mounts": ["team://platform-knowledge", "team://shared-prompts"]
  personal/          ← local indexes, memory, context policies
  mounts/
    platform-knowledge/   ← cached state for mounted space
    shared-prompts/       ← mounted prompt library
```

**Shared AI Inference**

Teams share an AI provider instance rather than each developer configuring their own:

- Shared API key managed by the team, not per-developer
- Shared rate limit and cost tracking across the team
- Shared model catalog: the team selects approved models; individual developers choose from the approved list
- Local override for developers who need a personal provider

**Federated Context Assembly**

The `ferret_context` MCP tool assembles context across all mounted Knowledge Spaces simultaneously. The Context Assembly pipeline (Sprint 13) is already designed as a stage-based pipeline — federation adds a `FederatedSearch` stage that fans out across spaces and merges results before the deduplication stage.

```
Query
  └─ FederatedSearch (V2)
       ├─ PersonalSpace.Search(query)
       ├─ MountedSpace["platform-knowledge"].Search(query)
       └─ MountedSpace["shared-prompts"].Search(query)
  └─ Deduplicate (Sprint 13)
  └─ Expand
  └─ ContentFilter
  └─ TokenBudget
  └─ ContextPackage → AI assistant
```

Context policies on each Knowledge Space control what can be included when the space is queried externally.

---

## V2.5 — Ferret Insights

**Theme:** Business Intelligence and Executive Dashboards. Ferret tells you not just what your codebase contains, but how your team is using it — and proves the ROI to stakeholders.

**Headline: An engineering manager can open a dashboard and see exactly how Ferret is saving developer time, growing the knowledge base, and reducing context-switching costs.**

**Reserved projects:** `Ferret.Analytics`, `Ferret.Dashboard`

### V2.5 Capabilities

**Analytics Platform**
Structured event collection for every Ferret operation: searches, context generations, connector syncs, AI model calls, knowledge additions. Stored locally in SQLite. Events are the foundation for all dashboards and reports.

*See `ARCH-018-Analytics-Architecture.md` for the full event taxonomy.*

**Developer Dashboard**
Per-developer view: context usage, searches performed, AI token cost, most-used connectors. Helps individual developers understand their own Ferret usage patterns.

**Engineering Dashboard**
For tech leads: index freshness, connector health, knowledge coverage, hot files, knowledge debt (undocumented areas).

**Repository Dashboard**
% of codebase indexed, % in the knowledge graph, ADR coverage, technical debt trends.

**AI Dashboard**
Token usage by model, cost by day/week/month, context efficiency ratio. Helps teams track AI spend and optimize.

**ROI Dashboard**
Estimates developer hours saved from faster context retrieval. Year-over-year knowledge base growth. Connector coverage breadth.

**Executive Dashboard**
Non-technical summary: "Connected to N sources, N architecture decisions captured, average context time Xms." One-page snapshot for CTO/VPE.

**Reports**
- `ferret report --weekly` — Markdown + HTML weekly summary
- `ferret report --sprint` — Sprint-level analysis
- `ferret report --executive` — Metrics-only executive report

---

## V3 — Enterprise Intelligence

**Theme:** Ferret applies its ContextOS platform to solve the hardest enterprise engineering intelligence problems. Each V3 product is ContextOS plus a specialized reasoning layer.

**Headline: An enterprise can use Ferret to answer questions that no single engineer, team, or tool could answer alone.**

### V3 Products

**Enterprise Work Intelligence**
Connects JIRA, GitHub, ADO, Confluence, Git, and Slack into a unified work-context model. Answers: "What is the team actually shipping? What is blocking us? What decisions are being made in Slack that never make it to JIRA?"

**Decision Intelligence**
Surfaces every architectural and technical decision ever made: in ADRs, PR comments, Confluence, email, Slack. Answers: "Why does this service do X?" traces through five years of decisions.

**Root Cause Intelligence**
Given a production incident, reconstructs: the exact state of the system at incident time, the last 10 changes, the on-call engineer, the relevant tickets, and the previous similar incidents. 15-minute retrospective becomes 2 minutes.

**Architecture Intelligence**
Maintains a living model of the system architecture: services, dependencies, data flows, security boundaries. Detects drift between the intended and actual architecture. Answers: "Is this PR introducing a forbidden dependency?"

**Security Intelligence**
Scans the knowledge graph for security anti-patterns: exposed secrets, vulnerable dependencies, insecure patterns across the codebase, and compliance violations. Not just static analysis — context-aware: "This pattern is OK in test code but not in production."

**Observability Intelligence**
Connects production telemetry (metrics, logs, traces) to the knowledge graph. Answers: "Which feature is causing the p99 latency spike?" by correlating the deployment graph with the metrics graph.

**Enterprise Knowledge Store**
The persistent, org-scoped knowledge graph. Teams contribute; everyone queries. Ferret Hub (multi-workspace federation). Single source of truth for engineering knowledge.

**Enterprise Knowledge Store**
The persistent, org-scoped knowledge graph. Teams contribute; everyone queries. Ferret Hub (multi-workspace federation). Single source of truth for engineering knowledge.

---

## V3.5 — Enterprise Time Machine

**Theme:** Temporal knowledge and historical replay. The past is a first-class citizen of your engineering intelligence platform.

**Headline: An SRE can reconstruct the complete state of the system — code, decisions, tickets, context — as of the exact moment an incident began.**

### V3.5 Capabilities

**Workspace Snapshots**
Point-in-time capture of the complete ContextOS workspace state: workspace manifest, connector states, index manifests, knowledge graph snapshot. Tagged to the git commit hash at snapshot time.

**Repository Snapshots**
At every significant event (PR merge, release, incident), Ferret automatically records what the codebase looked like — not just the code diff but the full context: who changed what, which tickets were linked, what the architecture looked like.

**Historical Search**
"Search the knowledge graph as of 3 months ago." Answer questions about past states without losing current state.

**Timeline Explorer**
An interactive (CLI and web) view of the workspace's history. Scrub through time; see how the knowledge graph, connector states, and index coverage evolved.

**Incident Replay**
Given an incident timestamp, reconstruct: the deployment state, the last 10 code changes, the on-call engineer, the open tickets, the previous similar incidents, and the relevant ADRs. Turns a 2-hour retrospective into 10 minutes.

**Release Replay**
For any past release, reconstruct exactly what was shipped: features, ADRs applied, technical debt addressed, and the team's context at the time.

**Build Replay**
Replay the context state at the time of any CI/CD run to debug environment-specific failures.

**Enterprise Connectors for V3.5**
All enterprise data sources become time-aware: JIRA tickets as of a date, GitHub PRs merged by a date, Slack conversations from an incident window.

*See `ARCH-017-Storage-Architecture.md` §8 Snapshot Storage for the implementation approach.*
*See `RESEARCH-001-Future-Research.md` — Enterprise Time Machine for open research questions.*

---

## V4 — Autonomous Enterprise

**Theme:** Ferret moves from intelligence to action. The knowledge graph doesn't just answer questions — it drives workflows, detects anomalies, and coordinates automated responses.

**Headline: Ferret autonomously maintains engineering quality, surfaces risks before they become incidents, and learns from every decision the team makes.**

### V4 Capabilities

**Digital Twin**
A continuously-updated, queryable model of the entire software system — not just code, but people, processes, decisions, and history. Every change to the system is reflected in the twin within minutes.

**AI Learning**
The platform learns from the team's decisions: what gets rejected in code review, which PRs get reverted, which ADRs are ignored. It adapts its recommendations to your organization's actual standards, not just generic best practices.

**Autonomous Enterprise**
Ferret detects architectural drift and automatically opens tickets. It detects knowledge debt (undocumented decisions, single-contributor areas) and automatically creates onboarding materials. It monitors for known patterns that precede incidents and alerts before impact.

**Workflow Automation**
Context-aware automation: when a PR touches a high-risk area, Ferret automatically assigns the right reviewers, links the relevant ADRs, and runs the appropriate checklist. Not script-based automation — knowledge-graph-driven automation.

---

## Architecture Implications

These V2/V3/V4 capabilities are the reason for certain V1 architecture decisions:

| V1/V2 Decision | Enables |
|---|---|
| `.ferret/snapshots/` in Sprint 7 | Enterprise Time Machine in V3 |
| `ConnectorType.Custom` in Sprint 7 | Third-party connector ecosystem in V2 |
| `IConnector.SupportsChangeDetection` | Incremental indexing in V2, real-time twin in V4 |
| `Ferret.Core` zero dependencies | Air-gapped enterprise deployment in V3 |
| Three-tier memory model in V2 | AI Learning and context adaptation in V4 |
| Knowledge graph in V2 | Autonomous detection and workflow in V4 |
| Context Assembly pipeline as stages (Sprint 13) | Federated context assembly in V2 — `FederatedSearch` stage fans out across mounted Knowledge Spaces |
| `.ferret/workspace.json` schema (Sprint 7) | `mounts` array in V2 — workspace declares which shared Knowledge Spaces it has mounted |
| `IAiProvider` abstraction (Sprint 12) | Shared AI inference in V2 — provider can be team-scoped, not developer-scoped |
| Context policies in `ContextPackage` (Sprint 13) | Per-Knowledge-Space context policies and permissions in V2 federation |

The workspace engine built in Sprint 7 is not just a folder — it is the embryo of both the V2 Knowledge Space and the V4 Digital Twin.

---

## Related Documents

- `ROADMAP-001.md` — V1 committed roadmap
- `COMPETITIVE-001.md` — Competitive landscape
- `FUTURE-001-Future-Architecture.md` — Architecture implications of V2–V4
- `docs/000-Overview/Vision.md` — Mission and values
- `IDEAS.md` — Speculative feature ideas
