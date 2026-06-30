# Specification — Sprint 0: Project Foundation

| Field | Value |
|---|---|
| **Status** | Approved |
| **Sprint** | Sprint 0 |
| **Author** | Ferret Core Team |
| **Date** | 2026-06-27 |
| **Last Updated** | 2026-06-27 |

---

## Problem Statement

Ferret is a greenfield project. Before any production code can be written, the repository must be structured so that the team can work efficiently, consistently, and with quality gates from day one.

## Goal

Establish the complete repository scaffold: folder hierarchy, document templates, CI/CD workflows, coding standards, and community files — so all subsequent sprints have a stable, professional foundation.

## Scope

### In Scope
- Repository folder structure
- `.editorconfig`, `.gitignore`, `Directory.Build.props`
- Bootstrap PowerShell scripts
- Skeleton .NET solution
- GitHub Actions: CI, release, security
- GitHub community files: CONTRIBUTING, CODE_OF_CONDUCT, SECURITY, CODEOWNERS
- Document templates in `docs/templates/`
- README files for every top-level folder
- `.ai/` workspace folder with workspace.json, ai-config.json, metrics.json, session.md, current-context.json
- Numbered `docs/` category structure (000–013, ADR, Reviews, templates)
- First ADR recording the use of ADRs
- Architecture Review AR-001
- This specification document

### Out of Scope
- Any production source code
- Database or infrastructure provisioning
- External service integrations

## Acceptance Criteria

- [ ] All folders listed in the repository layout exist and contain at least one file.
- [ ] `dotnet build src/Ferret.sln` succeeds on a clean checkout.
- [ ] `scripts/bootstrap.ps1` runs without error on Windows (PowerShell 7+).
- [ ] CI workflow file is syntactically valid YAML.
- [ ] All templates use consistent metadata headers.
- [ ] `docs/` is organised into numbered categories 000–013 plus ADR, Reviews, templates.
- [ ] `.ai/` folder contains all required files and subfolders.
- [ ] No orphaned files or broken links.

## Open Questions

_None at this time._
