# Workflow: ImplementFeature

## Trigger
A feature work item is assigned with implementation acceptance criteria and the ARCH-NNN document for the target module is Accepted.

## Inputs
- Feature work item spec (WorkItem-Template.md format)
- ARCH-NNN document for the target module
- Feature-Template.md (for feature specification)
- Existing codebase state (`.ai/current-context.json`)

## Preconditions
- ARCH-NNN document for the target module is in Accepted status
- All feature dependencies are Done
- No pending ADR blocks this feature's implementation

## Execution Steps

1. **Author feature specification**
   Using Feature-Template.md, write a one-page feature spec covering: feature ID, module, ARCH reference, interface contracts affected, acceptance criteria, and test plan. This is a working document — not a formal deliverable — but it must exist before writing code.

2. **Identify affected files**
   List: source files to create or modify, test files to create or modify, config files (if any). Do not touch files outside this list without re-scoping the WI.

3. **Write failing tests first (TDD)**
   For each acceptance criterion: write a unit test that exercises it. Run the tests. Confirm all new tests are red. If a test passes before implementation, the test is wrong — fix it.

4. **Implement**
   Write only enough implementation to make the failing tests pass. Do not implement beyond the acceptance criteria.

5. **Refactor**
   After all tests are green: review for clarity, naming consistency with STD-005, and adherence to ARCH-NNN contracts. No behaviour changes — only structural improvements.

6. **Write integration tests**
   If the feature crosses module boundaries: write at least one integration test in `tests/Ferret.Integration.Tests/` that exercises the end-to-end path.

7. **Run CodeChecklist.md**
   All items must pass. Do not proceed to review if any CodeChecklist item fails.

8. **Commit**
   Conventional commit: `feat(<module>): description (WI-XYYY)`. Stage only the files in the declared scope.

9. **Invoke CodeReview.md**
   Submit for review. Address all Blockers. Receive Approve before merging.

## Validation
- All acceptance criteria covered by tests
- CodeChecklist.md passes
- CI green after commit
- Reviewer approved

## Outputs
- Feature implementation in `src/`
- Unit tests in `tests/<Module>.Tests/`
- Integration tests in `tests/Ferret.Integration.Tests/` (if applicable)
- Commit with conventional message

## Exit Criteria
All acceptance criteria met, CodeChecklist green, CI green, Reviewer approved, WI marked Done.
