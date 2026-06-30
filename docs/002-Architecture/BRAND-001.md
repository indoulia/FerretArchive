# BRAND-001 — Ferret Brand Identity

| Field | Value |
|---|---|
| **Document ID** | BRAND-001 |
| **Version** | 1.0 |
| **Status** | Accepted |
| **Last Updated** | 2026-06-28 |

## Product Identity

| Attribute | Value |
|---|---|
| **Product name** | Ferret |
| **Technology platform** | ContextOS |
| **Tagline** | Ferret — Dig Deep. Deliver Context. |
| **CLI binary** | `ferret` |
| **Previous name** | AISpace (used through Sprint 5; renamed during Sprint 5) |

## Naming Conventions

- Use **Ferret** as the product name in all user-facing text, documentation, and UI.
- Use **ContextOS** when referring to the technology platform layer.
- The `ferret` CLI binary name is lowercase, matching Unix conventions.
- Namespace prefix: `Ferret.*` (e.g. `Ferret.Core`, `Ferret.Runtime`).
- NuGet package prefix (when published): `Ferret.*`.
- Assembly name prefix: `Ferret.*`.
- Test assembly suffix: `Ferret.*.Tests`.

## Historical Accuracy

Documents written before the rebrand (Sprint 0 through Sprint 5 planning documents) may reference "AISpace". These are preserved with a historical context banner. Do not retroactively remove historical names from commit messages, git tags, or ADRs — these form part of the project's audit trail.

Tags:
- `v0.5.0-sprint5` — last tag under the AISpace name
- `v0.5.0-ferret` — first tag under the Ferret name (same codebase, rebrand commit applied)

## Brand Usage Rules

1. **Do** use "Ferret" in new documentation, code comments, error messages, and CLI output.
2. **Do** use "Ferret" in new ADRs and architecture documents.
3. **Do not** use "AISpace" in new content.
4. **Do not** modify the body of historical ADRs (ADR-0001 through ADR-0011) written under the AISpace name — they carry a post-rebrand notice banner instead.
5. Exception codes `AISP-xxx` are stable identifiers and are **not** renamed. They remain as-is for backwards compatibility.
