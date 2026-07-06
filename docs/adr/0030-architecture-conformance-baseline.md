# ADR-0030 — Milestone: Architecture Conformance Baseline

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-06 |
| **Deciders** | Ferret Core Team |
| **Sprint** | Post-Dogfooding Sprint 2 / Architecture Conformance Review, Rounds 1–4 |
| **Supersedes** | — |

---

## Context

A four-round Architecture Conformance Review ran across 2026-07-06, triggered by a set of dogfooding bug fixes and the discovery that the working branch (`dogfooding`) had drifted 51 commits behind `main`, missing the entire `Ferret.Workspace.Graph`/`Ferret.Knowledge.Federation` subsystem. Each round closed a distinct category of finding rather than re-litigating the whole review from scratch:

- **Round 1–2**: identified the branch divergence (Critical) and reviewed the newly-merged federation subsystem's own conformance once reconciled.
- **Round 3**: closed every documentation/governance drift that didn't require a code change — module inventory, ADR index, backlog completion status, a Decision Log entry reconciling `ferret status`'s interim implementation against the Sprint 6 decision it superseded.
- **Round 4**: resolved the two remaining AC-012 (Minimal Core) findings — `GitHeadResolver` (an active git-I/O capability hardcoded into `Ferret.Core`) moved to `Ferret.Indexing`; `SearchHit.SourceWorkspaceId` (a Core contract naming a Workspace-domain concept) renamed to the domain-neutral `SourceId`.

At the close of Round 4, the review had verified — not assumed — the following, against the actual repository state at commit `cff94b4`:

| Property | Verified as |
|---|---|
| **AC-001** (Vendor Neutrality) | Satisfied — no vendor SDK reference in `Ferret.Core`/`Ferret.Runtime` (checked directly; `ProviderId`/`ConnectorType` are taxonomy labels, not SDK imports) |
| **AC-004** (Plugin First) | Satisfied — no domain-specific capability remains hardcoded in Core after the `GitHeadResolver` move |
| **AC-008** (Deterministic Behaviour) | Satisfied — no `Random`/`DateTime.Now` in Core, Search, Indexing, Workspace.Graph, or Federation domain logic |
| **AC-012** (Minimal Core) | Satisfied — zero undocumented exceptions remain in `Ferret.Core`; both known additions resolved (moved or renamed) rather than left silent |
| **Dependency graph** | Verified acyclic, inward-only, by full `.csproj`-reference-graph audit of all 41 projects — `Ferret.Core` has zero outgoing project references; no project depends back "up" into `Cli`/`Mcp`/`Runtime` from a lower layer |
| **Documentation** | Synchronized with implementation — module inventory, ADR index, and backlog completion status all corrected to match actual `src/` state |
| **Governance** | Current — the one implementation decision that superseded a prior Accepted decision (`ferret status`) is now recorded, not silent |
| **Build & architecture fitness tests** | `Ferret.Architecture.Tests` (the ARCH-001 §8.4 fitness-function suite): 31/31 passing. Full solution: 30/30 test projects, 0 failures |

This is the same seam ADR-0012 and ADR-0021 each recognized in their own domains: continuing to hunt for new findings past this point would not be discovering more architecture — it would be re-verifying properties this review has already established with direct evidence.

## Decision

We declare an **Architecture Conformance Baseline** for Ferret, as of commit `cff94b4` on `dogfooding`, covering the properties verified above.

**Preservation rules, effective immediately for every future Epic:**

1. **AC-001, AC-004, AC-008, and AC-012 must continue to hold.** A change that would violate one of these requires either a fix before merge, or a superseding ADR that explicitly documents the exception and its justification — the same discipline this baseline itself was established under (see ADR-0030's own Round 4 resolutions for the pattern: move the capability out, or rename to remove the leaked vocabulary, or document why neither applies).
2. **The dependency graph's acyclic, inward-only property must not regress.** A new project reference that closes a cycle, or that reaches outward from `Ferret.Core` into any other module, is a blocking finding on sight — not a style note to be revisited later.
3. **Future Architecture Conformance Reviews treat this record as the baseline, not a re-derivation point.** A review from here forward reports *new* deviations from the properties in the Context table above; it does not need to re-verify AC-001/004/008/012 compliance from first principles each time, unless a specific change gives it reason to suspect regression in one of them.
4. **This baseline does not freeze any subsystem's public surface** (unlike ADR-0012's package freeze) **— it freezes the architectural invariants**: dependency direction, Core purity, and doc/governance synchronization. Search, Federation, Workspace Graph, CLI, and every other module remain free to evolve; what may not regress is Core's purity and the graph's shape.
5. **No new automated fitness function was added to enforce AC-012 specifically.** The existing `Ferret.Architecture.Tests` suite (31 tests) enforces the dependency-graph and layering rules already; AC-012 (semantic/vocabulary purity, as distinct from structural dependency purity) was verified manually this round and is not yet machine-checked. Adding such a check is compatible with this baseline but is not required by it.

## Alternatives Considered

| Option | Why rejected |
|---|---|
| No formal baseline; treat each future review as starting from zero | Loses the explicit seam ADR-0012 and ADR-0021 already established as this repository's pattern for exactly this kind of milestone; every future review would re-spend effort re-verifying properties already directly checked here |
| Continue searching for further architectural findings before declaring a baseline | Diminishing-returns risk, same reasoning ADR-0021 applied to its own mechanism-layer review: two full audit passes (Round 2's full dependency-graph sweep, Round 4's targeted AC-001/004/008/012 check) found exactly two real findings, both now resolved — further searching without new evidence (a real implementation change, a new subsystem) is unlikely to be productive |
| Build automated CI enforcement for AC-012 before declaring the baseline | Real, valuable follow-up work, but not required to establish that the current state satisfies AC-012 — the manual verification in this round is direct evidence, not a placeholder for automation that doesn't exist yet |

## Consequences

### Positive
- Future Architecture Conformance Reviews can open with "confirm the baseline still holds, then look for what's new" instead of a full first-principles audit — a faster, evidence-grounded starting point, mirroring ADR-0021 §Consequences exactly.
- Any autonomous or human-driven Epic work from here forward has a concrete, checkable contract (the Context table + preservation rules above) rather than an implicit expectation.
- The dependency-graph audit methodology (full `.csproj` reference extraction, checked for cycles and outward-from-Core edges) is now a repeatable, low-cost check any future review can re-run in minutes.

### Negative
- Any future violation of AC-001/004/008/012 must now be either fixed or ADR-documented before merge — added process overhead versus silent drift, accepted deliberately (this is the exact overhead ADR-0012 §Negative already accepted for its own freeze).
- AC-012 (semantic Core purity) is not machine-enforced; a future addition could reintroduce a naming/vocabulary leak into Core without a CI gate catching it. This baseline records that gap rather than closing it.

### Neutral / Risks
- This baseline is scoped to the four constraints actually reviewed (AC-001, AC-004, AC-008, AC-012) and the dependency graph — it does not certify AC-002/003/005–007/009–011/013/014, which were out of scope for this review cycle and remain unverified one way or the other.

## Related

- ADR-0001: Use Architecture Decision Records
- ADR-0012: Milestone 1 — Platform Foundation Freeze (precedent for this decision's format and "freeze rules" structure)
- ADR-0021: Milestone: Ferret V2 Architecture Baseline v1 Complete (precedent for declaring a review-driven baseline and its transition rules)
- ARCH-001 §8 (Dependency Rules), §9 (Architectural Constraints AC-001–AC-014), §8.4 (Fitness Functions)
- `docs/013-Governance/DECISION-LOG.md` — "Governance — Architecture Baseline Established (2026-07-06)"
- Commits `545e5c4` (governance/documentation alignment), `171e7ee` (`GitHeadResolver` move), `cff94b4` (`SourceWorkspaceId` rename)
