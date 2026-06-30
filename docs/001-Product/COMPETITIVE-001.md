# COMPETITIVE-001 — Competitive Landscape

| Field | Value |
|---|---|
| **Document ID** | COMPETITIVE-001 |
| **Version** | 1.0 |
| **Status** | Living Document |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Last Updated** | 2026-06-28 |

---

## Purpose

This document maps the competitive landscape for Ferret. It informs product decisions by clarifying where Ferret is differentiated, where it competes, and where it deliberately does not compete.

---

## Market Positioning

Ferret occupies the intersection of three existing categories:

| Category | Examples | Where Ferret fits |
|---|---|---|
| AI coding assistants | GitHub Copilot, Cursor, Codeium | Ferret provides context; these consume it |
| Code search / indexing | Sourcegraph, grep.app, OpenGrok | Ferret goes beyond search to understanding |
| Knowledge management | Confluence, Notion, Coda | Ferret captures engineering decisions, not general docs |
| Codebase AI understanding | Greptile, Cody (Sourcegraph) | Direct overlap; Ferret is context-first, not generation-first |

**Ferret's positioning:** "The operating system for engineering context." Not an AI assistant. Not a search engine. The persistent, structured knowledge layer that makes AI assistants significantly more useful.

---

## Direct Competitors

### Greptile

- **What it is:** API service that indexes your codebase and answers questions about it via API.
- **Strengths:** Simple API, fast onboarding, cloud-hosted (no self-hosting needed), natural language queries.
- **Weaknesses:** Cloud-only (no air-gap, no local), codebase-only (no JIRA, Confluence, Slack, logs), no persistent workspace state, no decision history.
- **Ferret differentiator:** Local-first, repository-scoped, connector architecture (any data source), enterprise-ready (air-gap).

---

### Sourcegraph Cody

- **What it is:** AI coding assistant with deep Sourcegraph integration. Context comes from Sourcegraph's code search and graph.
- **Strengths:** Enterprise-grade, battle-tested at scale, multi-repo, team sharing.
- **Weaknesses:** Requires Sourcegraph deployment (complex, expensive), context is codebase-only, no decision/history awareness.
- **Ferret differentiator:** No infrastructure to deploy, connector architecture, knowledge graph captures decisions not just code.

---

### Continue.dev

- **What it is:** Open-source AI coding assistant VS Code/JetBrains extension. Supports MCP context providers.
- **Strengths:** Open-source, highly configurable, supports MCP (context can come from anywhere).
- **Weaknesses:** Context provider ecosystem is fragmented and shallow; no persistent workspace knowledge.
- **Ferret differentiator:** Ferret is a persistent context platform, not a context provider. Continue.dev can *consume* Ferret via MCP.

---

### GitHub Copilot Workspace

- **What it is:** GitHub's AI-native development environment. Understands issues, PRs, and code together.
- **Strengths:** Deep GitHub integration, Microsoft investment, large user base.
- **Weaknesses:** GitHub-only (no JIRA, Confluence, on-prem), cloud-dependent, no local knowledge graph.
- **Ferret differentiator:** Source-agnostic (works with JIRA/ADO/GitHub), local-first, knowledge graph persists across AI sessions.

---

### Pieces for Developers

- **What it is:** AI-powered developer memory tool — snippets, context, workflow capture.
- **Strengths:** Personal developer memory, offline model support.
- **Weaknesses:** Personal tool (not team-scale), codebase awareness is shallow, no connector architecture.
- **Ferret differentiator:** Team-scale, repository-rooted, deep codebase understanding.

---

## Adjacent Tools (Non-Competing)

These tools solve related problems but are not direct competitors. Ferret can integrate with them as connectors.

| Tool | Category | Ferret relationship |
|---|---|---|
| Linear / JIRA | Issue tracking | Connector source |
| Confluence / Notion | Documentation | Connector source |
| Datadog / Grafana | Observability | Connector source (V3) |
| Claude / GPT-4 | LLMs | Consumer of Ferret context via MCP |
| GitHub Copilot | AI assistant | Consumer of Ferret context via MCP |
| Cursor | AI editor | Consumer of Ferret context via MCP |
| Sourcegraph | Code search | Connector source for code graph |

---

## Strategic Moat

Ferret's long-term defensibility is the **knowledge graph** — the accumulated, structured understanding of an engineering organization's history, decisions, and patterns. This is:

1. **Time-compounding** — More valuable the longer it runs. A 5-year-old Ferret installation understands your system better than any new tool.
2. **Organization-specific** — Cannot be replicated by a competitor without your data.
3. **Multi-modal** — Code + decisions + tickets + conversations + logs. No single-source tool can match this.
4. **Privacy-preserving** — Local-first. Enterprise data never leaves the organization.

---

## Where Ferret Does Not Compete

- **LLM providers** — Ferret consumes models, does not build them.
- **General documentation tools** — Confluence, Notion, Google Docs. Ferret indexes them, not replaces them.
- **CI/CD pipelines** — GitHub Actions, Azure Pipelines. Ferret is a context layer, not an execution layer.
- **Code generation** — Copilot, Cursor, Codeium. Ferret provides context for generation, not the generation itself.
