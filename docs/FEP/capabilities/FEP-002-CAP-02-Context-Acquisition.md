# FEP-002-CAP-02 — Context Acquisition

| Field | Value |
|---|---|
| **Document ID** | FEP-002-CAP-02 |
| **Program** | Ferret Engineering Program (FEP) |
| **Parent** | [FEP-002 — Capability Catalog](../FEP-002-Capability-Catalog.md) |
| **Authoritative Source** | FEP-001 §2.2 — Capability Model |
| **Status** | Draft — Prompt 2 output |
| **Last Updated** | 2026-07-08 |

---

## 1. Purpose

Nothing downstream can organize, maintain, assemble, or deliver context that Ferret never observed in the first place. Context Acquisition exists to make sure engineering-relevant content actually enters Ferret's awareness, faithfully and within the scope a workspace has declared.

## 2. Responsibilities

- Discover what content exists within a workspace's declared scope, across whichever source categories that scope includes.
- Read and observe the content of discovered sources — code, documents, history, decisions, conversations, and any other declared source.
- Recognize when new sources appear within scope, and when previously known sources disappear or become unreachable.
- Preserve acquired material faithfully enough that Organization can work from it without loss of meaning.
- Attach acquisition-time facts — what was acquired, from where, and when — for Provenance & Attribution to record.
- Report acquisition coverage and gaps to Observability & Health.

## 3. Non-Responsibilities

- Must never interpret, structure, or extract meaning from what it reads — that belongs to Context Organization.
- Must never decide, on its own initiative, when to re-acquire due to detected change — that decision belongs to Context Maintenance; Acquisition executes it.
- Must never filter or rank content by relevance to a particular request — that belongs to Context Assembly.
- Must never write to, modify, or take action on the sources it reads.
- Must never decide what counts as in-scope — that belongs to Workspace Definition.

## 4. Inputs

- A resolved scope declaration from Workspace Definition, describing which sources to observe.
- Connectivity or reachability state for declared sources, described conceptually.
- Change signals from Context Maintenance indicating that re-acquisition is due.

## 5. Outputs

- Raw acquired material, faithfully preserved, ready for Context Organization.
- Acquisition facts — source identity, acquisition time, success, failure, or partial status — for Provenance & Attribution.
- Coverage and gap reports for Observability & Health.

## 6. Context Objects

- **Source** — a conceptual reference to something Ferret can acquire from: a repository, a document store, a conversation archive.
- **Acquisition Unit** — a discrete piece of raw material acquired from a source at a point in time.
- **Acquisition Event** — the conceptual record that a given Acquisition Unit was read, when, and with what outcome.

## 7. Relationships

Consumes scope from Workspace Definition. Feeds raw material to Context Organization. Executes on demand from Context Maintenance's change detection. Supplies origin facts to Provenance & Attribution. Reports to Observability & Health.

## 8. Constraints

- **Business.** Acquisition must never exceed the scope Workspace Definition declares — reading out-of-scope content is a policy violation, not a bonus.
- **Product.** Acquisition must be resilient to partial failure; one unreachable source must not block acquisition of others in the same workspace.
- **Context integrity.** What is acquired must be preserved faithfully; Acquisition must not summarize, truncate, or otherwise lossily transform content on the way in, since loss at this stage is unrecoverable downstream.

## 9. Success Criteria

- Everything within declared scope that is reachable has, in fact, been acquired.
- Gaps — unreachable, inaccessible, or out-of-scope material — are known and reported, never silently absent.
- Raw material handed to Organization faithfully represents the source at the time of acquisition.

## 10. Failure Modes

- **Silent gaps** — a source is unreachable and Acquisition produces nothing with no visible signal of incomplete coverage, violating Product Principle P5.
- **Scope creep** — Acquisition reads beyond declared scope, for example by following a link into an out-of-scope system.
- **Lossy acquisition** — content is acquired in a degraded or partial form Organization cannot recover meaning from.
- **Acquisition storms** — overly sensitive or excessive re-acquisition overwhelms a source, when a change signal was too coarse or acquisition doesn't respect a source's own constraints.

## 11. Future Evolution

Expansion of recognized source categories as consumer needs grow (FEP-001 Open Question 6). Increasingly precise coverage reporting, distinguishing "not yet acquired" from "acquired but incomplete" from "declared out of scope." An explicit product stance on acceptable coverage for source categories that are inherently partial or sampled, such as very high-volume conversation archives.
