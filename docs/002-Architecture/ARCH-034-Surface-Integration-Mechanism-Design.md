# ARCH-034 — Ferret V2 Surface Integration Mechanism Design

| Field | Value |
|---|---|
| **Document ID** | ARCH-034 |
| **Version** | 1.0 |
| **Status** | Draft |
| **Owner** | Ferret Project |
| **Author** | Ferret Core Team |
| **Review Status** | Pending — requires a Standard Architecture Review (`AR-`) or SDK-level review per V2-ROADMAP-001 §5's RM-09 governance note |
| **Date** | 2026-07-03 |
| **Last Updated** | 2026-07-03 |
| **Related ADRs** | None yet — this document defines no CLI command, MCP tool, flag, or resource; it makes no API decision of any kind |
| **Related Spec** | None yet |
| **Parent Architecture** | ARCH-023 (V2 Architectural Boundary); ARCH-024 (Artifact Inventory) §7 — the real CLI/MCP surface this document integrates with; ARCH-032 (Persistence Mechanism Design); ARCH-033 (Dependency Resolution Mechanism Design) — the mechanism this document surfaces |
| **Governed By** | ARCH-031 (Mechanism Architecture Principles) |
| **Roadmap Item** | [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) RM-09 |

---

## Purpose

V2-ROADMAP-001 §5 describes RM-09 as specifying "any CLI/MCP-facing surface for V2 capabilities, per existing `docs/006-CLI/`, `docs/007-SDK/` conventions." A Repository-First check of those two locations, and of `docs/005-MCP/`, found that all three are Sprint-0/1 placeholder scaffolding — generic index stubs with no real command, tool, or endpoint content, and in `docs/007-SDK/README.md`'s case, a description of a REST API (`https://api.Ferret.dev/v1`, JWT/API-key auth) that ARCH-024's direct source-code investigation found no evidence of anywhere in `src/`. None of the three is a verified architectural source. This document therefore grounds itself instead in ARCH-024 §7, the Repository-First-verified inventory of the real CLI and MCP surface (`Ferret.Cli`, `Ferret.Mcp`), and treats `docs/006-CLI/`, `docs/005-MCP/`, and `docs/007-SDK/` only as the eventual location for eventual, unrelated documentation of whatever concrete surface changes a future ADR or implementation makes — never as a source of architectural fact this document may build on.

This document answers one question: **what must remain true of the existing CLI/MCP surface if and when an owning engine chooses to let its output benefit from ARCH-033's resolution outcome, instead of always recomputing?** It does not define a new command, a new MCP tool, a new flag, a new resource, a request or response shape, or an error code. Every one of those is an API decision, explicitly and permanently out of scope for this document — not merely deferred to an ADR, but excluded from this document's authority to decide at all (§6).

Every statement in this document answers **how the conceptual kernel is realized**. None answers what the conceptual kernel should be.

---

## Scope

Covers:
- The real, existing CLI/MCP surface this document integrates with, and its current ownership (§1)
- What must remain invariant about that surface's behavior if resolution is ever consulted behind it (§2)
- Inputs this mechanism consumes (§3) and outputs it is bound by (§4)
- What surface integration does not do, and is not authorized to decide (§5)
- Guarantees a surface integration must satisfy (§6, §7)
- The boundary with RM-07 (Persistence) and RM-08 (Resolution) (§8)
- The implementation freedom this document deliberately leaves open (§9)

Does not cover, and has no authority to decide:
- Any new CLI command, subcommand, or flag
- Any new MCP tool, resource, or prompt
- Any change to an existing command's or tool's name, arguments, or output shape
- Any request or response schema
- Any error code, exit code, or status taxonomy
- Storage, serialization, key, or hashing decisions (ARCH-032, ARCH-033, and their ADRs)
- Any redefinition of an artifact (ARCH-024), a validity concept (ARCH-025), a persistence requirement (ARCH-026), a resolution outcome (ARCH-027), request equivalence (ARCH-028), a propagation rule (ARCH-029), or dependency participation semantics (ARCH-030)
- Any reopening of a Closed Architectural Decision (AGR-001 §6)

---

## Repository-First Method

Every fact about the existing CLI/MCP surface used below is taken as-is from ARCH-024 §7, the Repository-First-verified inventory of `Ferret.Cli` and `Ferret.Mcp`. This document introduces no new surface artifact, and it does not treat `docs/006-CLI/README.md`, `docs/005-MCP/README.md`, or `docs/007-SDK/README.md` as authoritative, for the reason stated in Purpose — those three files describe a generic or aspirational surface that ARCH-024's direct investigation did not confirm exists. Every requirement, guarantee, and principle governing what surface integration must preserve is taken as-is from ARCH-023, ARCH-027, ARCH-032, and ARCH-033. This document introduces no new component, no new artifact class, and no new ownership boundary.

---

## 1. The Existing Surface This Document Integrates With

Per ARCH-024 §7, the real CLI/MCP surface consists of artifacts already owned by `Ferret.Cli` and `Ferret.Mcp`: `CommandResult`, `DiagnosticCheckResult`, `ValidationFailure`/`ValidationResult`, view-model DTOs, `McpToolDescriptor`, `McpToolResult`, and `McpResourceContent`. ARCH-024 §7 already states that `McpToolResult` and `McpResourceContent` "inherit reuse-eligibility of the underlying artifact" they wrap (e.g. `ContextPackage`, `SearchResult`) — this document is the first to state what that inheritance requires once a resolution mechanism (ARCH-033) actually exists to make reuse possible.

Four real MCP tools exist today (`search`, `read_document`, `workspace_status`, `ferret_context`), each proxying a deterministic V1 capability; none invokes a model (ARCH-024 §7). This document assumes no change to that list, their names, their arguments, or their output shapes — introducing, removing, or altering any of them is an API decision outside this document's authority (Scope, above).

---

## 2. What Must Remain Invariant

**Indistinguishable output.** The Core V2 Principle (ARCH-023 §5) is that reuse replaces recomputation without changing what is produced. A surface artifact populated from a resolution-confirmed reuse (`Satisfied`, ARCH-027 §3) and the same surface artifact populated from a fresh computation must be behaviorally indistinguishable with respect to every field that already exists on that surface artifact today — the same CLI output, the same MCP tool result shape, the same existing fields, with the same meaning. Nothing about this document, or about resolution existing, licenses a surface to change the meaning or presence of an existing field, or to introduce a different error condition, depending on whether reuse occurred. This guarantee governs the artifact's existing shape; it does not by itself forbid a later, strictly additive, optional field (§9) — such a field does not make the *existing* output distinguishable, since a consumer unaware of it observes no change to anything it already relied on.

**Unchanged ownership.** The owning engine (Knowledge Engine for `ContextPackage`/`SearchResult`, per ARCH-023's V1 Component Mapping) remains the sole owner of the command or tool that surfaces its output. This document assigns no CLI command or MCP tool to V2, to ARCH-032, or to ARCH-033 — consistent with ARCH-023 §4's non-goal that V2 never becomes a second orchestrator or a new owner of any V1 responsibility.

**Unchanged fallback.** Where ARCH-033 reports Not-satisfied or Indeterminate, the owning engine's pre-existing behavior — compute the artifact itself, exactly as it does today with no resolution mechanism present — is what the surface presents. Nothing about a failed or negative resolution outcome is itself surfaced as an error; it is surfaced as whatever result recomputation would have produced anyway, per ARCH-023 §6 ("V1 does not require V2 to function").

---

## 3. Inputs

This mechanism consumes:
- The engine's already-existing decision of what to do with a resolution outcome from ARCH-033 (reuse the candidate, or recompute) — a decision this document does not make and does not influence.
- The artifact the owning engine ultimately produces or reuses (e.g., a `ContextPackage`), regardless of which path produced it.
- Nothing from ARCH-032 or ARCH-033 directly. This mechanism has no independent channel to persistence or resolution — it receives only what the owning engine, having already consulted those mechanisms itself, hands to its own existing surface-production code.

---

## 4. Outputs

This mechanism is bound by, and produces nothing beyond, the same surface artifact type the owning engine already produces today (`CommandResult`, `McpToolResult`, `McpResourceContent`, or another artifact from ARCH-024 §7's inventory) — populated with the same content it would contain with no V2 mechanism present, per §2's indistinguishability requirement. This document defines no new output type and no new field on an existing one.

---

## 5. What Surface Integration Does Not Do

- **It does not create, remove, or rename a command, tool, resource, or flag.** That is an API decision (Scope, above).
- **It does not define a request or response shape.** ARCH-024 §7's existing artifacts already have shapes; this document does not add to or alter them.
- **It does not invoke resolution or persistence directly.** Per §3, it receives only what the owning engine already decided and produced — it is not a second caller of ARCH-032 or ARCH-033.
- **It does not expose resolution's internal outcome (Satisfied / Not-satisfied / Indeterminate) as user-facing surface state**, unless and until a future ADR or implementation, operating within §7's guarantees, decides to add optional provenance information — a decision this document neither makes nor forecloses (§9).
- **It does not introduce a new error code, exit code, or failure mode.** An Indeterminate or Not-satisfied resolution outcome degrades to the pre-V2 recomputation path (§2); it is never surfaced as a distinct V2-specific failure.

---

## 6. Surface Integration Guarantees

| Guarantee | Statement | Basis |
|---|---|---|
| Indistinguishable output | A reused artifact and a freshly computed one produce identical values for every existing surface field; any future optional field is strictly additive and never changes an existing field's presence or meaning | ARCH-023 §5 (Core V2 Principle) |
| Unchanged ownership | The owning V1 engine remains the sole owner of its command/tool surface | ARCH-023 §4 non-goals; ARCH-023 Data Ownership |
| Unchanged fallback | A Not-satisfied or Indeterminate outcome degrades silently, from the surface's perspective, to the pre-V2 recomputation path | ARCH-023 §6; ARCH-027 §6 |
| No new API surface | This document authorizes no new command, tool, resource, flag, schema, or error code | ARCH-023 §4 non-goals (CLI integrations named as out of scope for ARCH-023 itself; realized, not reopened, here) |
| No side effects | Surface integration never itself invokes `IModelProvider`, mutates persisted state, or performs resolution | ARCH-023 §9; ARCH-032 §7.5; ARCH-033 §8.3 |
| No second source of truth | Surface integration maintains no independent record of what was reused or recomputed beyond what the owning engine already tracks | ARCH-023 §4; ARCH-027 §5 |

---

## 7. Mechanism-Level Invariants — Preserving Conceptual Guarantees Through Surface Integration

**7.1 Existing ownership (ARCH-023 Data Ownership; ARCH-031 §8).** Realized by §2 and §6: every command and tool remains owned by the same component ARCH-023's V1 Component Mapping already names. This document introduces no ninth owning component and no new "V2 surface."

**7.2 No hidden side effects (ARCH-023 §9; ARCH-031 §8).** Realized by §5: surface integration is purely a presentation step over an artifact the owning engine already finished producing — it performs no resolution, no persistence, and no model invocation of its own.

**7.3 No silent recomputation, inverted (ARCH-032 §4; ARCH-033 §8.4).** Where ARCH-032 and ARCH-033 forbid silently substituting recomputation for a stored verdict, this document forbids the opposite failure mode: silently changing an *existing* field's presence or meaning depending on whether reuse occurred. Realized by §2's indistinguishable-output guarantee — reuse must be invisible with respect to everything a surface already exposes, never invisible in the sense of being undetectable if something is actually wrong. A later, strictly additive, optional field (§9) does not violate this invariant precisely because it changes nothing about what already exists.

**7.4 Fail-closed, applied to the surface layer (ARCH-025 §8; ARCH-026 §7; ARCH-027 §5).** Realized by §2's unchanged-fallback guarantee: a resolution mechanism that cannot confirm validity never causes the surface to present a result that resolution could not itself stand behind — the surface simply receives the same recomputed result it would have received had ARCH-032/ARCH-033 never been consulted.

**7.5 No new source of truth (ARCH-023 §4; ARCH-027 §5).** Realized by §6: surface integration keeps no record of what was reused, beyond whatever the owning engine already tracks in its own domain (and, per ARCH-032, only if that engine chooses to make such a record durable).

---

## 8. Boundary With RM-07 (Persistence) and RM-08 (Resolution)

- This mechanism has no direct relationship with ARCH-032 or ARCH-033. It is downstream of the owning engine, which is itself the sole caller of both (ARCH-027 §2; ARCH-032 §1). Surface integration never bypasses the owning engine to consult persistence or resolution on its own.
- ARCH-033 §10 already states this from the resolution side: resolution "has no visibility into, and makes no assumption about, how or whether its outcome ever reaches a CLI or MCP surface." This document confirms the reverse is equally true: surface integration has no visibility into, and makes no assumption about, how resolution reached its outcome.
- Where a future ADR or implementation adds optional provenance information to a surface artifact (§5), that information may describe only what the owning engine itself already knows (per §6's no-second-source-of-truth guarantee) — it may not be sourced independently from ARCH-032 or ARCH-033.

---

## 9. Implementation Freedom Remaining

After this document, at least the following remain entirely open, to be settled by an ADR or by implementation, and are explicitly **not** decided, foreclosed, or mandated here:

- Whether any existing command or tool is ever modified to add optional, owning-engine-sourced provenance information (e.g., "this result was reused") — and if so, in what form, provided the addition is strictly additive and never changes the presence or meaning of an existing field (§2, §6)
- Whether a new command, tool, resource, or flag is ever introduced to expose V2 capabilities directly (e.g., a diagnostic command reporting resolution statistics) — this document neither authorizes nor forbids such a future addition; it states only that, whenever proposed, it is an API decision requiring the same scrutiny (and, per ARCH-031 §6, likely a dedicated ADR) any other API change in this repository already requires
- The internal code path by which an owning engine's existing surface-production logic comes to consume a resolution-confirmed reuse instead of a fresh computation
- Whether `docs/006-CLI/`, `docs/005-MCP/`, or `docs/007-SDK/` are ever updated to reflect real, current surface conventions — a documentation-maintenance question outside this document's scope

---

## Relationship to the Conceptual Kernel

This document adds nothing to the frozen kernel and amends none of ARCH-023 through ARCH-030. It realizes the "V2 Surface Design (CLI/MCP)" item V2-ROADMAP-001 §5 schedules as RM-09, grounded in ARCH-024 §7's verified surface inventory rather than in the unverified placeholder documentation V2-ROADMAP-001 §5 pointed toward. Where this document states a rule not verbatim in the kernel — the indistinguishable-output requirement (§2, §6) chief among them — it is shown to be a direct corollary of the Core V2 Principle (ARCH-023 §5), not an independent addition to it.

---

## Interaction With RM-07 and RM-08

RM-09 (this document) requires both RM-07 (ARCH-032) and RM-08 (ARCH-033) to be complete before it proceeds, per V2-ROADMAP-001 §5's entry criteria for RM-09. This document assumes ARCH-032's and ARCH-033's guarantees hold; it decides nothing about how persistence or resolution are realized, and — per §8 — has no direct calling relationship with either.

---

## Interaction With Future ADRs

Per ARCH-031 §6's test, any concrete surface change (a new flag, a new provenance field, a new command) is an API decision, not a decision this document makes. Such a change would itself typically warrant an ADR under the existing criteria already stated in `docs/adr/README.md` ("affects more than one component or team boundary... involves a technology or pattern that will be hard to reverse..."), following the process ADR-0001 establishes. Any such future ADR must state which of this document's guarantees (§6) and invariants (§7) it upholds — in particular, indistinguishable output (§2) and unchanged ownership (§2) — before it may be accepted.

---

## Conformance With ARCH-031

| ARCH-031 §7 requirement | Satisfied by |
|---|---|
| Guarantee-by-guarantee trace | §7, tracing five kernel-derived invariants individually |
| Responsibility trace | §1–§5; §8 |
| Ownership trace | §2 ("Unchanged ownership"); no new owning component introduced anywhere in this document |
| Explicit non-goals | Scope ("Does not cover, and has no authority to decide"), §5 |
| Statement of ADRs produced | Interaction With Future ADRs — none produced by this document itself; concrete surface changes are named as the trigger for a future ADR |
| Confirmation no Closed Architectural Decision is contradicted | See Impact on Existing Architecture, below |

---

## Impact on Existing Architecture

**Existing components reused.** This document reuses ARCH-024 §7's real CLI/MCP artifact inventory, ARCH-023's Core V2 Principle and Data Ownership principle, and ARCH-032's and ARCH-033's outcome/guarantee vocabulary — all without modification.

**Existing components extended.** None. This document assigns no new interface, storage responsibility, or behavior to `Ferret.Cli` or `Ferret.Mcp` — it states only what must remain true of their existing behavior.

**Existing components intentionally unchanged.** All of them, including the four real MCP tools and every CLI artifact ARCH-024 §7 catalogued. This document changes none of their names, arguments, or output shapes.

**New concepts introduced.** None at the conceptual tier. One mechanism-tier corollary — the indistinguishable-output requirement (§2, §7.3) — is introduced, derived directly from ARCH-023 §5's Core V2 Principle rather than added independently.

**Closed Architectural Decisions.** All nine (AGR-001 §6) checked individually against this document's text; none is contradicted, narrowed, or reinterpreted.

**Correction to a prior assumption.** V2-ROADMAP-001 §5 describes RM-09 as proceeding "per existing `docs/006-CLI/`, `docs/007-SDK/` conventions." This document's Repository-First check found those conventions do not exist in verified form — both locations (and `docs/005-MCP/`) contain only Sprint-0/1 placeholder scaffolding, and `docs/007-SDK/README.md` describes a REST API ARCH-024's code-level investigation found no evidence of. This document does not amend V2-ROADMAP-001 (a planning document, not part of the frozen kernel, and outside this document's scope to edit) but records the discrepancy here so no future document relies on those three files as an architectural source without first re-verifying them against `src/`.

---

## Cross References

| Document | Relationship |
|---|---|
| [ARCH-023](ARCH-023-V2-Architectural-Boundary.md) | Core V2 Principle (§2, §6, §7.3), Data Ownership, and non-goals this document realizes without reopening |
| [ARCH-024 §7](ARCH-024-Artifact-Inventory.md) | Repository-First source of the real CLI/MCP surface this document integrates with (§1) |
| [ARCH-027](ARCH-027-Dependency-Resolution-Architecture.md) | Source of the reuse-vs-recompute decision this document treats as entirely the owning engine's (§2, §3) |
| [ARCH-031](ARCH-031-Mechanism-Architecture-Principles.md) | Governing document — the evidentiary standard and invariant checklist this document is written to satisfy |
| [ARCH-032](ARCH-032-Persistence-Mechanism-Design.md) | Sibling mechanism document; this document has no direct calling relationship with it (§8) |
| [ARCH-033](ARCH-033-Dependency-Resolution-Mechanism-Design.md) | Sibling mechanism document whose outcome this document's owning-engine caller consumes indirectly (§8, §10 of ARCH-033) |
| [AGR-001](../Reviews/AGR-001.md) | Source of the nine Closed Architectural Decisions confirmed unaffected (Impact, above) |
| [V2-ROADMAP-001](V2-ROADMAP-001-Architecture-Program.md) | RM-09 — this document is that roadmap item; corrected here regarding the unverified `docs/006-CLI/`/`docs/007-SDK/` convention reference (Impact, above) |
| `docs/006-CLI/README.md`, `docs/005-MCP/README.md`, `docs/007-SDK/README.md` | Checked and found to be unverified placeholder scaffolding, not an architectural source (Purpose, Repository-First Method) |
| `docs/adr/README.md`, [ADR-0001](../adr/0001-use-architecture-decision-records.md) | Existing ADR process and criteria a future concrete surface change would be assessed against |

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-03 | Ferret Core Team | Initial Surface Integration Mechanism Design — realizes RM-09 against ARCH-024 §7's verified CLI/MCP inventory rather than the unverified `docs/006-CLI/`/`docs/007-SDK/` conventions V2-ROADMAP-001 §5 pointed toward. Defines no API. Pending Standard Architecture Review. |
