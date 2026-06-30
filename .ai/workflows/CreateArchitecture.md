# Workflow: CreateArchitecture

## Trigger
Sprint spec requires a new ARCH-NNN document, or a pending ADR outcome mandates a new architecture document.

## Inputs
- Parent ARCH document (typically ARCH-001 for new modules, or ARCH-012 for cross-cutting sub-topics)
- PRD-001 requirements that this architecture addresses
- ARCH-TEMPLATE-001 (document standard — Type A or Type B selection)
- Any accepted ADRs relevant to this architecture domain
- Decision Register (for architectural constraints already in force)

## Preconditions
- Document ID is assigned in the relevant README.md index (no self-assignment)
- Document type (A = system/platform-wide, B = single engine/component) is determined by ChiefArchitect
- No existing accepted ARCH document covers the same scope

## Execution Steps

1. **Assign document ID and reserve it**
   Add a `Planned` row to the relevant README.md index (`docs/002-Architecture/README.md` for architecture docs) with the document ID before writing anything else. This prevents ID conflicts.

2. **Determine document type**
   - Type A: platform-wide concern (no C2/C3 diagrams required). Follow Appendix B of ARCH-TEMPLATE-001.
   - Type B: single engine or component. Requires C2 diagram (§2), C3 diagram (§3), ≥3 sequence diagrams (§4).

3. **Populate metadata table**
   Fill all metadata fields: Document ID, Version (1.0), Status (Draft), Owner, Author, Review Status (Pending), Last Updated, Related ADRs, Related Spec, Parent Architecture.

4. **Author all required sections**
   For Type A: Purpose, Scope, all concern sections, Design Rationale, Cross References, Revision History.
   For Type B: §1–§12 per ARCH-TEMPLATE-001 §3, Cross References, Revision History.
   No section may be left empty. No placeholder text.

5. **Draw diagrams**
   All diagrams are Mermaid inline. Validate Mermaid syntax before proceeding. Every sequence diagram must have a title and show at least one error/failure path.

6. **Update cross-references**
   Add a row to the Cross References section of this document for every ARCH document that this document references or is referenced by. Update the parent ARCH document's Cross References section to include this document.

7. **Run ArchitectureChecklist.md**
   All items must pass before submitting for review.

8. **Update README index**
   Change the row status from Planned to Draft.

9. **Submit for review**
   Invoke ReviewArchitecture.md.

## Validation
- ArchitectureChecklist.md passes
- Mermaid diagrams syntactically valid
- Zero placeholder text
- All cross-references resolve to existing files
- README index updated

## Outputs
- `docs/002-Architecture/ARCH-NNN.md` in Draft status
- Updated README index (status: Draft)
- Updated parent ARCH document cross-references

## Exit Criteria
ReviewArchitecture.md completes with Approved outcome. Document status updated to Accepted in both the file and the README index.
