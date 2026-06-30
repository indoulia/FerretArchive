# Workflow: SpecificationReview

## Trigger
A specification document is submitted for review (Specification Engine state transition: Draft → Under Review).

## Inputs
- Specification document (in Draft state)
- Relevant PRD-001 requirements sections
- ARCH-008 (Review and Specification Architecture — when available)
- Acceptance criteria defined for the associated work item

## Preconditions
- Specification document is in Draft state
- Associated work item is identified and in scope for the current sprint
- Author has confirmed the specification is ready for review (not a work in progress)

## Execution Steps

1. **Completeness check**
   Verify all required sections are present in the specification. A specification with empty or placeholder sections receives an immediate Blocker.

2. **Requirement traceability**
   Map each specification requirement to a PRD-001 section. Any specification requirement that cannot be traced to PRD-001 must be either: (a) traced to an accepted ARCH-NNN constraint, or (b) flagged as a new requirement needing ProductManager approval.

3. **Architectural consistency check**
   Verify the specification does not require behaviour that contradicts AC-001 through AC-014 or any accepted ARCH-NNN document. Flag contradictions as Blockers.

4. **Open questions resolution**
   Identify any open questions or assumptions in the specification. Each one must be resolved before the specification can be approved. An unresolved open question is a Blocker.

5. **Acceptance criteria review**
   Verify the specification's acceptance criteria are measurable and binary. Vague criteria ("looks correct", "seems complete") are Blockers.

6. **Produce findings**
   List all findings as Blocker / Suggestion. Issue Approved or Rejected decision.

7. **Update specification state**
   If Approved: specification state transitions to Approved.
   If Rejected: specification returns to Draft with findings attached.

## Validation
- All required sections evaluated
- All open questions resolved
- All acceptance criteria are measurable
- Zero unresolved Blockers at approval time

## Outputs
- Structured findings list (Blocker / Suggestion)
- Approved or Rejected decision
- Specification state updated

## Exit Criteria
Zero open Blockers, Approved decision issued, specification state is Approved.
