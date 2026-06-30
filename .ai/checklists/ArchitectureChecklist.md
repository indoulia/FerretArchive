# Architecture Checklist

Use for every ARCH-NNN document before submitting for review. Mark each item: ✓ Pass | ✗ Fail | N/A (with reason).

## Metadata
- [ ] Document ID assigned from README index (not self-assigned)
- [ ] Version, Status, Owner, Author, Review Status, Last Updated all populated
- [ ] Related ADRs listed (or "None" if none)
- [ ] Parent Architecture reference correct

## Document Type
- [ ] Type A or Type B explicitly determined (per ARCH-TEMPLATE-001)
- [ ] All required sections for the document type are present (Appendix B for Type A; §1–§12 for Type B)

## Content Quality
- [ ] Zero placeholder text (TBD, TODO, [fill in], [placeholder], "to be defined")
- [ ] No empty sections — every section has substantive content
- [ ] All design decisions match the Decision Register or are new decisions that will be added
- [ ] Cross-cutting concerns reference ARCH-012 rather than re-defining them
- [ ] Configuration references ARCH-011 rather than defining its own config model

## Diagrams (Type B)
- [ ] C2 (container) diagram present and syntactically valid Mermaid
- [ ] C3 (component) diagram present and syntactically valid Mermaid
- [ ] At least 3 sequence diagrams present
- [ ] Each sequence diagram shows at least one error or failure path
- [ ] No diagram references a module that does not exist in ARCH-001 §7

## Cross-References
- [ ] Every link in the Cross References table resolves to an existing file
- [ ] Every ARCH-NNN referenced in the body exists on disk
- [ ] No reference to a Planned document as if it were Accepted
- [ ] Parent ARCH document updated to reference this document in its Cross References

## Architectural Constraints
- [ ] No constraint or interface contradicts ARCH-001 AC-001 through AC-014
- [ ] No new dependency violates ARCH-001 §8 dependency rules
- [ ] No vendor-specific type in any interface or contract defined in this document

## README Index
- [ ] README index row for this document exists and status matches document status (Draft or Accepted)
