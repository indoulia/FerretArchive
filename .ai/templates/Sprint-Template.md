# Sprint N — [Sprint Goal in One Sentence]

| Field | Value |
|---|---|
| **Sprint** | N |
| **Status** | Planned / In Progress / Complete |
| **Start Date** | YYYY-MM-DD |
| **End Date** | YYYY-MM-DD |
| **Owner** | ProductManager |
| **Architect Sign-Off** | ChiefArchitect (pending / YYYY-MM-DD) |

---

## Sprint Goal

[One sentence. What will be true when this sprint is done that is not true now.]

---

## Pending ADRs Due This Sprint

ADRs that must be Accepted before work items that depend on them can start.

| ADR | Title | Blocks WI | Status |
|---|---|---|---|
| ADR-NNNN | [Title] | WI-XYYY | Pending / Accepted |

---

## Work Items

| ID | Title | Type | Priority | Depends On | Status |
|---|---|---|---|---|---|
| WI-XYYY | [Title] | Doc / Impl / Mixed | High / Med / Low | — | Planned |

---

## Work Item Details

### WI-XYYY — [Title]

**Type:** Documentation / Implementation / Mixed
**Agent:** [ChiefArchitect / PlatformEngineer / TechnicalWriter]
**Files:**
- Create: `path/to/file.md`
- Modify: `path/to/other.cs`

**Inputs:**
- [ARCH-NNN §section]
- [PRD-001 §section]

**Acceptance Criteria:**
- [ ] [Measurable, binary criterion 1]
- [ ] [Measurable, binary criterion 2]

**Exit Criteria:** [What must be true for this WI to be Done]

**Checklist:** [ArchitectureChecklist.md / CodeChecklist.md]

---

## Architectural Constraints

[List any constraints from ARCH-001 AC-001 through AC-014 that are particularly relevant to this sprint's work. Do not re-state all 14 — only those the implementing agent must keep top of mind.]

---

## Definition of Done

A work item is Done when:
- All acceptance criteria are met and verifiable
- Relevant checklist passes with zero open items
- CI is green after all commits
- Reviewer has approved (where required)
- README indexes are updated (for documentation WIs)

---

## Sprint Validation

[Validation steps to run at sprint close — e.g., grep checks, cross-reference verification, fitness function passes.]

---

## Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | YYYY-MM-DD | ProductManager | Initial sprint spec |
