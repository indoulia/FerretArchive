# IDEAS — Ferret Project Ideas Backlog

Speculative ideas that are not on the roadmap but may be worth exploring. These are raw inputs — not commitments. Any idea promoted to the roadmap becomes a ROADMAP entry; any idea shaping an architectural decision becomes an ADR.

**Last updated:** 2026-06-28

---

## Context Intelligence

- **Ambient context** — Ferret observes which files a developer has open, which tests they run, and builds a real-time "working context" automatically. No explicit `ferret index` needed.
- **Context diff** — Before a PR review, show what context has changed since the last review session. "Here's what's new since you last looked at this."
- **Context replay** — Replay the context state at any point in git history. Answer "what did the team know on the day this commit was made?"
- **Context compression** — Automatic pruning of stale context (old branches, deleted files, superseded decisions) to keep the workspace lean.

---

## Enterprise Intelligence

- **Decision archaeology** — Given a line of code, trace back through every relevant decision: ticket, PR, comment, ADR, Confluence doc that contributed to it existing.
- **Blast radius analysis** — Before merging a PR, show which teams, products, and downstream consumers are affected.
- **Knowledge debt** — Surface areas of the codebase with no ADRs, no docs, no tests, and single-contributor knowledge (bus factor 1).
- **Onboarding accelerator** — When a new engineer joins, auto-generate a context pack: the 10 most important decisions, the 5 highest-risk areas, the 3 experts to talk to.

---

## Connector Ideas

- **Slack connector** — Index public channel conversations, link discussion threads to the commits or PRs they reference.
- **Email connector** — RFC / design decision emails indexed and linked to code.
- **Meeting notes connector** — Auto-index meeting notes from calendar systems; link action items to tickets.
- **Browser history connector** — With explicit consent, link the documentation a developer read to the commits they made afterward.
- **Log connector** — Index production logs, surface "this error occurred 47 times in the last 30 days" during code review.

---

## Developer Experience

- **Ferret MCP server** — Expose the workspace knowledge graph as an MCP server so any AI assistant (Claude, GitHub Copilot, Cursor) can consume Ferret context.
- **VS Code extension** — Inline context: hover over a function to see its decision history, test coverage, and last modifier.
- **JetBrains plugin** — Same as VS Code but for Rider/IntelliJ.
- **`ferret ask`** — Natural language query against the knowledge graph. "Why does this service exist?" returns the relevant ADR, ticket, and PR.

---

## Platform / ContextOS

- **Multi-workspace federation** — A single Ferret instance querying across multiple repositories (monorepo-unfriendly teams).
- **Knowledge graph export** — Export the workspace graph as RDF/JSON-LD for consumption by other tools.
- **Schema versioning** — workspace.json and state.json schema migration framework. Auto-upgrade on open.
- **Workspace templates** — `ferret workspace init --template web-api` seeds standard connectors, conventions, and initial knowledge for common project types.
- **Ferret Hub** — Shared, org-scoped knowledge layer. Teams push to Hub; individuals pull. Enterprise product.

---

## Enterprise Time Machine

- **Snapshot diffs** — What changed in the knowledge graph between two snapshots (sprints, releases, incidents)?
- **Incident archaeology** — Given an incident timestamp, reconstruct exactly what the system knew, who was on-call, what was deployed, and what the last 10 changes were.
- **Release retrospective** — Automatically generate a release report: features delivered, ADRs applied, technical debt addressed, tests added.

---

## Speculative / Long-Term

- **Ferret as a build step** — `ferret validate` as a CI check: does this PR violate any ADR? Does it introduce knowledge debt? Does it reduce context coverage?
- **Knowledge graph ML** — Train a model on your org's knowledge graph to predict: which areas are high-risk, which engineers should review which PRs, which refactors are overdue.
- **Digital twin** — A living, queryable model of your entire software system: not just code, but people, processes, decisions, and history.
