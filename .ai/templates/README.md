# .ai/templates — Runtime AI Templates

This folder contains **runtime prompts and AI instruction templates** used by the Ferret platform and developer tooling at runtime.

These are **not** document templates for human authors.

---

## Distinction

| Folder | Purpose |
|---|---|
| `.ai/templates/` | Prompt templates, system instructions, agent scaffolding — consumed by the AI runtime |
| `docs/templates/` | Document templates for humans — ADRs, specs, PRDs, API docs, etc. |

---

## Contents (planned)

| File | Description |
|---|---|
| `agent-system-prompt.md` | Base system prompt injected into all agents |
| `tool-description.md` | Template for describing a new MCP tool |
| `summarise-session.md` | Prompt template for session summarisation |
| `review-code.md` | Prompt template for automated code review |

---

All runtime templates will be added as the Ferret runtime is implemented in Sprint 1+.
