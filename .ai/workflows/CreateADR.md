# Workflow: CreateADR

## Trigger
A significant architectural decision is required. Identified through: sprint spec (pending ADR column), architecture review finding, ChiefArchitect decision, or a disputed implementation choice.

## Inputs
- Decision context: what problem requires a decision and why it cannot wait
- Constraints from ARCH-001 AC-001 through AC-014 that bound the decision space
- Decision Register — confirm the decision has not already been made
- ADR-Template.md

## Preconditions
- The decision is not already recorded in the Decision Register
- The decision is significant enough to warrant an ADR: it affects public interfaces, introduces a new dependency, changes a platform-wide behaviour, or will be difficult to reverse

## Execution Steps

1. **Check the Decision Register**
   Search `docs/013-Governance/Decision-Register.md` for the decision topic. If it is already Accepted, stop — no new ADR needed. If it is Pending, this workflow produces the ADR for that pending entry.

2. **Assign ADR number**
   Find the highest existing ADR number in `docs/adr/`. Assign the next sequential number. Format: `NNNN-kebab-title.md`.

3. **Author the ADR using ADR-Template.md**
   Required sections:
   - **Title**: one line, imperative ("Use X for Y")
   - **Status**: Proposed
   - **Date**: today's date
   - **Context**: problem statement and the constraints that bound the decision space
   - **Decision**: the decision made, stated as a positive action ("We will use X")
   - **Consequences**: what becomes easier, what becomes harder, what changes as a result
   - **Alternatives Considered**: at least two alternatives with reasons for rejection
   - **References**: ARCH-NNN documents, PRD-001 sections, other ADRs

4. **Circulate for review**
   ChiefArchitect reviews the ADR. If the decision is security-sensitive, SecurityArchitect reviews. If it is performance-critical, PerformanceEngineer reviews.

5. **Update status**
   ChiefArchitect changes status from Proposed to Accepted or Rejected. If Accepted: update the Decision Register.

6. **Update Decision Register**
   If Accepted: move the row from Pending Decisions to Accepted Decisions. Fill in: ADR reference, date, one-sentence description.

7. **Update related ARCH documents**
   If the ADR resolves a design choice referenced in an ARCH-NNN document, update that document's Cross References section and any relevant "Open Questions" section.

## Validation
- ADR has all required sections with non-placeholder content
- Decision Register updated if Accepted
- Related ARCH documents updated if applicable

## Outputs
- `docs/adr/NNNN-title.md` with status Accepted or Rejected
- Updated Decision Register row
- Updated ARCH-NNN cross-references (if applicable)

## Exit Criteria
ADR status is Accepted or Rejected and committed. Decision Register reflects the outcome.
