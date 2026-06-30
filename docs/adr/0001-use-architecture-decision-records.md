# ADR-0001 — Use Architecture Decision Records

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-06-27 |
| **Deciders** | Ferret Core Team |
| **Sprint** | Sprint 0 |

---

## Context

Ferret is a multi-component platform that will evolve over many sprints and contributors. Without a lightweight record-keeping mechanism, the reasoning behind key design choices is lost to chat logs, meeting notes, and memory. New team members have no way to understand *why* the system is the way it is.

## Decision

We will use Architecture Decision Records (ADRs) — short text documents that capture a single architectural decision, its context, considered alternatives, and consequences.

- Format: Markdown, stored in `docs/adr/`.
- Naming: `NNNN-kebab-case-title.md`.
- Workflow: proposed via PR → approved via merge → status set to `Accepted`.
- Template: see `docs/adr/0000-template.md`.

We follow the style popularised by Michael Nygard (2011) and adopted by MADR and the Thoughtworks ADR format.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Wiki page per decision | Diverges from code history; harder to tie to PRs |
| Decision log in CHANGELOG | Too interleaved with release notes; no clear per-decision view |
| No formal records | Context is lost; decisions look arbitrary to newcomers |

## Consequences

### Positive
- New contributors can onboard faster.
- Decisions are traceable to the conversation that produced them.
- Outdated decisions can be superseded rather than silently forgotten.

### Negative
- Small overhead per significant decision.
- Requires discipline to maintain the index in `README.md`.
