# Workflow: ReviewArchitecture

## Trigger
An ARCH-NNN document reaches Draft status and the author requests a review.

## Inputs
- ARCH-NNN draft document
- ARCH-TEMPLATE-001 (Type A or Type B checklist)
- ArchitectureChecklist.md
- Parent ARCH document (for consistency check)
- Decision Register (for constraint compliance)

## Preconditions
- Document has no placeholder text (TBD, TODO, [fill in])
- Document metadata table is fully populated
- ArchitectureChecklist.md has been run by the author and all items are recorded

## Execution Steps

1. **Structural validation**
   Verify all required sections are present per document type. Type A: Purpose through Revision History. Type B: §1–§12 plus Cross References and Revision History. Any missing required section is an automatic Blocker.

2. **Content validation**
   - No placeholder text in any section
   - No "TBD", "TODO", "[fill in]", or unfinished sentences
   - Every decision in the document is either consistent with the Decision Register or is a new decision that will be added to the register

3. **Diagram validation**
   - All Mermaid code blocks are syntactically valid
   - Type B documents have a C2 diagram, a C3 diagram, and at least 3 sequence diagrams
   - Every sequence diagram shows at least one error or failure path

4. **Cross-reference validation**
   - Every link in the Cross References section resolves to an existing file
   - Every ARCH document referenced in the body of the document exists
   - No reference to a Planned document as if it were Accepted (use "see ARCH-NNN (Planned)")

5. **Consistency check**
   - No constraint, interface, or behaviour contradicts ARCH-001 AC-001 through AC-014
   - No dependency rule contradicts ARCH-001 §8 (dependency rules)
   - Cross-cutting concerns reference ARCH-012 rather than re-defining them

6. **Produce AR-NNN**
   Write `docs/Reviews/AR-NNN.md` with: document reviewed, reviewer, date, findings (each categorised as Blocker / Suggestion / Question), and overall decision (Approved / Rejected).

7. **Update document status**
   If Approved: update document metadata Status to Accepted; update README index row to Accepted.
   If Rejected: document remains Draft; author addresses Blockers and re-submits.

## Validation
- All ArchitectureChecklist.md items explicitly evaluated
- AR-NNN committed to `docs/Reviews/`
- README index reflects final status

## Outputs
- `docs/Reviews/AR-NNN.md` with findings and decision
- Updated ARCH-NNN.md status field
- Updated README index status

## Exit Criteria
AR-NNN decision is Approved and committed. ARCH-NNN status is Accepted in both the file and the README index.
