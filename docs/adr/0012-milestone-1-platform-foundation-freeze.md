# ADR-0012 — Milestone 1: Platform Foundation Freeze

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-06-28 |
| **Deciders** | Ferret Core Team |
| **Sprint** | Sprint 6 (M1 close) |

---

## Context

After Sprint 6, Ferret has a complete platform foundation: Core contracts, Runtime host, CLI entry point, Hosting infrastructure, Event bus, Health/Diagnostics, Branding, and a full lifecycle model. 245 tests pass. The CLI delivers `ferret doctor`, `ferret status`, and `ferret --version` to real users.

These subsystems have been designed, reviewed, and stabilised across six sprints. They represent the lowest layer of the platform — the layer everything else builds on. At this point, continued ad-hoc changes to these packages risk architectural drift, breaking the assumptions that higher-level subsystems depend on.

The team is now transitioning from **platform building** to **product building**. Sprint 7 onward focuses on user-visible features. That transition only works if the foundation is stable.

## Decision

We declare **Milestone 1 (M1) — Platform Foundation** closed and frozen as of Sprint 6 / tag `v0.6.0-sprint6`.

The following packages are covered by this freeze:

| Package | Responsibility |
|---|---|
| `Ferret.Core` | Base contracts, exceptions, result types, cancellation |
| `Ferret.Runtime` | Runtime host, module lifecycle, DI orchestration |
| `Ferret.Hosting` | `IHostedService` integration, startup/shutdown |
| `Ferret.Cli` | CLI entry point, command dispatch, branding |
| `Ferret.Events` | Event bus contracts and in-process implementation |
| `Ferret.Health` | `IDiagnosticCheck`, `DiagnosticRunner`, health reporting |

**Freeze rules:**

1. No breaking changes to public interfaces, method signatures, or DI registration contracts in the packages above without a superseding ADR.
2. Bug fixes and non-breaking additions are allowed without an ADR.
3. Any proposal to redesign, replace, or structurally refactor a frozen package must first be accepted as a new ADR that supersedes this one (or a relevant section of it).
4. New subsystems (Sprint 7+) may depend on frozen packages but must not be merged back into them in ways that alter their public surface.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Continue evolving foundation packages sprint-by-sprint | Causes architectural drift; higher-level sprints cannot build confidently on a moving foundation |
| Freeze at a later milestone | M1 is the natural seam — foundation is complete and all higher-level work depends on it |
| No formal freeze, rely on code review | Informal gates fail under sprint pressure; an ADR creates explicit accountability |

## Consequences

### Positive
- Sprint 7+ plans can treat the foundation as a stable dependency — no rework risk.
- Architecture review conversations shift from "is this right?" to "does this fit the model?" — faster decisions.
- New contributors have a clear layer boundary: the foundation is not the place for experiments.
- Tag `v0.6.0-sprint6` becomes a meaningful semantic anchor, not just a checkpoint.

### Negative
- Legitimate improvements to frozen packages require an ADR, which adds overhead.
- If a design flaw is discovered in a frozen package, the correction path is more formal.

## Related

- ADR-0001: Use Architecture Decision Records
- Tag: `v0.6.0-sprint6`
- Sprint review: `docs/sprint-reviews/sprint-6-review.md` (if created)
