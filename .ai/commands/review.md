# Command: review

## When Invoked
The user types `/review` or asks for a review of something (code, architecture document, specification, ADR).

## Behaviour

1. **Determine what is being reviewed**
   Identify the subject from context:
   - If a PR number or diff is referenced: code review → invoke CodeReview.md
   - If an ARCH-NNN document is referenced: architecture review → invoke ReviewArchitecture.md
   - If a specification document is referenced: specification review → invoke SpecificationReview.md
   - If an ADR is referenced: ADR review as part of CreateADR.md step 4
   - If ambiguous: ask the user to clarify before proceeding

2. **Load review inputs**
   Read the subject document or diff. Load the relevant checklist. Load the work item spec if this is a code review.

3. **Execute the appropriate review workflow**
   - Code: CodeReview.md
   - Architecture: ReviewArchitecture.md
   - Specification: SpecificationReview.md

4. **Report findings**
   Present all findings clearly categorised as Blocker / Suggestion / Question. State the decision: Approve or Request Changes. Do not issue "Approve with minor comments" when any Blocker exists.

5. **For architecture reviews: produce AR-NNN**
   Write `docs/Reviews/AR-NNN.md` with the review outcome. Assign AR number sequentially from the highest existing AR number in `docs/Reviews/`.

## What NOT to Do
- Do not approve a subject with any open Blocker
- Do not conflate Suggestions with Blockers in the decision
- Do not skip ReviewChecklist.md step ("Before Starting" section) — CI must be green before reviewing code
- Do not review own work
