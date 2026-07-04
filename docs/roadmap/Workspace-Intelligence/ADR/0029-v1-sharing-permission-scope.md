# ADR-0029 — v1 Sharing and Permission Model Scope

| Field | Value |
|---|---|
| **Status** | Proposed — requires Founder decision |
| **Date** | 2026-07-05 |
| **Deciders** | Founder |
| **Milestone** | Workspace Intelligence Platform, Phase 5 |
| **Supersedes** | — |

---

## Context

The original brief calls for a five-role sharing model (Owner, Admin, Developer, Viewer, AI Agent), full invitation flow, conflict handling, and audit history. FUTURE-002 §22 already defers "RBAC for knowledge graph and memory access," "multi-tenant enterprise deployment," and "audit logging for AI operations" to V3, and leaves the organisation-memory privacy model as an open question (FUTURE-002 Q5). Building the full model now would either contradict that existing deferral or force resolving Q5 under time pressure it doesn't need to be resolved under.

## Decision

We will ship a **reduced four-role model for v1**: Owner, Admin, Developer, Viewer, enforced at reference-resolution time (`../03-Cross-Workspace-References.md` §4) and on the `workspace share` command (`../12-API.md` §2). The AI Agent role, cross-organisation sharing, invitation flows beyond direct user-ID grants, and audit history are deferred — see `../Future/Deferred-Scope.md`.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Ship the full five-role model with invitation flow and audit history now | Requires resolving FUTURE-002 Q5 (organisation memory privacy) and Q8 (Ferret Hub business model) prematurely; substantially expands Phase 5 scope against the Founder's own "fastest path to coding" directive |
| Ship no sharing model at all in v1, defer entirely | Shared workspaces are one of the milestone's stated top priorities (Objective 3); shipping zero access control on a workspace that can now be referenced by others is also a real security gap once references exist |

## Consequences

### Positive
- Unblocks shared-workspace use within a team immediately, without waiting on unresolved V3 questions
- Stays consistent with FUTURE-002's existing deferral boundary rather than quietly re-opening it

### Negative
- Teams wanting an AI-Agent-scoped role or cross-org sharing in the near term don't get it in this milestone

### Neutral / Risks
- If FUTURE-002 Q5/Q8 resolve differently than expected, the four-role model may need extension (not rework) — role enforcement is a single check point (`../03-Cross-Workspace-References.md` §4), so adding a fifth role later is additive
