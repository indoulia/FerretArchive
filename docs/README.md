# Docs

This folder contains all project-level documentation for Ferret.

---

## Structure

```
docs/
├── 000-Overview/    Vision, mission, principles, glossary, project state
├── 001-Product/     PRD, competitive analysis, product roadmap
├── 002-Architecture/System architecture (ARCH-*) and decisions/
├── 003-Workspace/   Workspace subsystem documentation
├── 004-Database/    Data model and storage documentation
├── 005-MCP/         MCP server/client documentation
├── 006-CLI/         CLI command reference
├── 007-SDK/         Plugin SDK documentation
├── 008-Modules/     Module-to-package inventory
├── 009-Testing/     Test strategy documentation
├── 010-Security/    Security documentation
├── 011-Performance/ Performance/SLO documentation
├── 012-Releases/    Release notes and release process
├── 013-Governance/  Governance index and decision log
├── Reviews/         Architecture and governance reviews (AR-*, AGR-*)
├── adr/             Architecture Decision Records
├── benchmarks/      Performance benchmark reports
├── roadmap/         Active roadmap and its ADRs/backlog
└── archive/         Superseded/historical material kept for provenance only —
                      not part of current onboarding reference
```

---

## Finding the Right Document

| I want to… | Go to… |
|---|---|
| Understand a past architectural choice | [docs/adr/](adr/) |
| See the overall system design | [docs/002-Architecture/](002-Architecture/) |
| Understand the data model | [docs/004-Database/](004-Database/) |
| Read the active product roadmap | [docs/roadmap/](roadmap/) |
| Check current project state | [docs/000-Overview/PROJECT-STATE.md](000-Overview/PROJECT-STATE.md) |
| Review a past architecture/governance review | [docs/Reviews/](Reviews/) |

---

## Writing New Docs

All document types have templates in [/templates](../templates/).  
Always use the relevant template to ensure consistent metadata and structure.
