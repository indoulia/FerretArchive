# ADR-0025 — Uncommitted Work During an Active Governance Gate

| Field | Value |
|---|---|
| **Status** | Proposed |
| **Date** | 2026-07-04 |
| **Deciders** | Ferret Core Team |
| **Sprint** | N/A — governance reconciliation, not a sprint deliverable |

---

## Context

`docs/DOGFOOD-001.md` was authorized 2026-06-30 (committed on `main`), declaring itself "the authoritative guide for the dogfooding period" and stating explicitly: *"no new implementation milestone is planned or started... Out of scope: new features, architectural changes, and new platform layers."*

Between 2026-07-03 and 2026-07-04, the Ferret V2 architecture program (ARCH-023 through ARCH-037, ADR-0021 through ADR-0024, AGR-001 through AGR-004, `V2-ROADMAP-001`, and the `Ferret.Persistence`/`Ferret.VerticalSlice` implementation) was developed while DOGFOOD-001 remained the most recent committed governance decision. No recorded governance reconciliation between these initiatives was found.

A governance review (2026-07-04) found that this program — every document and every line of code — exists only as untracked working-tree state. It has never been committed on any branch, local or remote (`git log --all` returns no matches for any V2 file). `main` and `docs/v2-architecture-boundary` have identical commit histories.

Because nothing has been committed, this is not a conflict in recorded history requiring correction — it is the absence of a rule for handling exactly this situation, discovered while a concrete instance of it was already underway.

## Decision

During an active governance gate, exploratory architecture, implementation, and review work may exist in the working tree, but no part of that work — documentation or production code — enters the committed repository until an explicit governance decision authorizes it.

The governance decision itself is committed as soon as it is made, even when the work it governs is not — a record of the policy or its application is exactly what was missing here, and its absence is what made the conflict discoverable only in retrospect rather than avoidable in advance.

Any future initiative found to overlap an active governance gate should be checked against this ADR, and reconciled through an explicit governance record, before any of its work is committed — not reconciled after the fact.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| Commit V2 documentation now; hold only the code back | The doc/code split is artificial — architecture documents are themselves "architectural changes," which DOGFOOD-001's scope excludes regardless of whether code accompanies them |
| Treat V2 as already implicitly authorized (no new ADR) | Nothing has been committed, so there is nothing to retroactively authorize — an implicit-authorization reading isn't supported by any record |
| Close DOGFOOD-001 now to clear the way for V2 | DOGFOOD-001 has no logged evidence of having been executed (no populated daily log); closing it without that evidence would defeat its own stated purpose |

## Consequences

### Positive
- Repository history stays consistent with any active governance gate's recorded scope — no retroactive fix is ever needed.
- Establishes a reusable principle for any future initiative that overlaps an active governance gate, not just the one that prompted this ADR.

### Negative
- Work that must wait for authorization remains outside version control for the duration of the gate — at risk from machine loss, workspace reset, or accidental `git clean`/`checkout`, with no remote backup, until the gate closes or an authorization decision is made. This ADR does not mitigate that risk; it only makes the trade-off explicit every time the policy applies.

### Applied Now — the Ferret V2 Working Tree

This is the situation that prompted this ADR, recorded here as its first application rather than as part of the policy itself:

- The Ferret V2 architecture program (ARCH-023 through ARCH-037, ADR-0021 through ADR-0024, AGR-001 through AGR-004, and the `Ferret.Persistence`/`Ferret.VerticalSlice` implementation — three sprints of reviewed, tested work) remains uncommitted pending a future decision to either (a) authorize its commit once DOGFOOD-001 concludes or is otherwise formally closed, or (b) discard it.
- Concretely, per the Negative consequence above: this specific body of work sits outside version control for the remainder of the dogfooding period (at least 2026-07-28, possibly to 2026-08-26). If that risk is judged unacceptable for this case, the mitigation (e.g., pushing to a private, unmerged branch rather than leaving it as plain working-tree files) is a separate decision this ADR does not make.
