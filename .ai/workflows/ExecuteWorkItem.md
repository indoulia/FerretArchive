# Workflow: ExecuteWorkItem

## Trigger
Work item assigned and all its declared dependencies are in Done status.

## Inputs
- Work item specification (WorkItem-Template.md format) with acceptance criteria
- Relevant ARCH-NNN documents for the module being modified
- Current codebase state (read `.ai/current-context.json`)

## Preconditions
- All WI dependencies are Done
- The ARCH-NNN document for the affected module is in Accepted or Draft status (not Planned)
- For implementation WIs: the relevant ARCH document exists and has been reviewed

## Execution Steps

1. **Understand scope**
   Read the WI spec. Identify: files to create, files to modify, ARCH documents to consult, acceptance criteria, and the relevant checklist.

2. **Load architecture context**
   Read the ARCH-NNN document for the affected module. Note interface contracts, dependency rules, error handling requirements, and observability requirements relevant to this WI.

3. **Plan changes**
   List exact files and changes before writing any code or documentation. For implementation WIs: identify test cases that cover the acceptance criteria.

4. **Execute**
   - **Documentation WIs**: Author the document following ARCH-TEMPLATE-001. Run ArchitectureChecklist.md before submitting.
   - **Implementation WIs**: TDD cycle — write failing test → confirm red → implement → confirm green → refactor. Repeat per acceptance criterion.
   - **Mixed WIs**: Documentation first (architecture must be approved before implementation).

5. **Run relevant checklist**
   Execute CodeChecklist.md (implementation) or ArchitectureChecklist.md (documentation). All items must pass.

6. **Commit**
   Stage only the files changed by this WI. Commit with conventional commit message: `type(scope): description (WI-XYYY)`.

7. **Update context**
   Update `.ai/current-context.json` to reflect the completed WI and any new context needed for subsequent WIs.

8. **Request review** (if required by sprint spec)
   Invoke CodeReview.md or ReviewArchitecture.md as appropriate.

## Validation
- All WI acceptance criteria explicitly verified (not assumed)
- Relevant checklist passes with zero open items
- CI green after commit
- No files outside the WI's declared scope were modified

## Outputs
- Code or documentation changes committed
- Checklist execution record
- Review request (if required)

## Exit Criteria
All acceptance criteria met, relevant checklist green, CI green, Reviewer approved (if required), WI marked Done.
